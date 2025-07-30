using UnityEngine;
using System.Collections;

public class EnemyBehaviour : MonoBehaviour, IEnemy
{
    private float currentHealth;
    private float currentShieldHealth;
    private float moveSpeed;
    private EnemyConfig config;
    private System.Action onReturnToPool;
    private bool isInitialized;
    private bool hasExploded = false;

    private SpriteRenderer cachedRenderer;
    private Rigidbody2D cachedRigidbody;
    private Transform playerTransform;

    private float attackTimer;
    private float burstTimer;
    private int burstShotsRemaining;
    private bool isInBurstMode;
    private float movementTimer;
    private Vector2 movementOffset;
    private bool isRetreating;
    private float healthRegenTimer;

    private static int PlayerLayer = -1;
    private static bool layersInitialized;

    private void Awake()
    {
        if (!layersInitialized)
        {
            PlayerLayer = LayerMask.NameToLayer("Player");
            if (PlayerLayer == -1) PlayerLayer = LayerMask.NameToLayer("Default");
            layersInitialized = true;
        }

        EnsureRequiredComponents();
    }

    private void EnsureRequiredComponents()
    {
        cachedRigidbody = GetComponent<Rigidbody2D>();
        if (cachedRigidbody == null)
        {
            cachedRigidbody = gameObject.AddComponent<Rigidbody2D>();
            cachedRigidbody.gravityScale = 0f;
            cachedRigidbody.linearDamping = 2f;
        }

        if (GetComponent<Collider2D>() == null)
        {
            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;
        }

        cachedRenderer = GetComponent<SpriteRenderer>();
        if (cachedRenderer == null)
        {
            cachedRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    public void Initialize(EnemyConfig config, System.Action onReturnToPool = null)
    {
        if (config == null)
        {
            Debug.LogError($"Enemy {gameObject.name}: EnemyConfig is null during initialization");
            return;
        }

        this.config = config;
        this.onReturnToPool = onReturnToPool;

        currentHealth = config.maxHealth;
        currentShieldHealth = config.hasShield ? config.shieldHealth : 0f;
        moveSpeed = config.moveSpeed;

        isInitialized = true;
        hasExploded = false;
        isRetreating = false;
        attackTimer = config.attackInterval * 0.5f;
        burstShotsRemaining = 0;
        isInBurstMode = false;
        movementTimer = 0f;
        healthRegenTimer = 0f;

        SetupVisuals();
        CachePlayerReference();

        transform.localScale = Vector3.one * config.scale;

        try { gameObject.tag = "Enemy"; } catch { }

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) gameObject.layer = enemyLayer;

        gameObject.SetActive(true);
        Debug.Log($"Enemy {config.enemyName} initialized with health={currentHealth}");
    }

    public void InitializeForPool(EnemyConfig config)
    {
        this.config = config;
        currentHealth = config.maxHealth;
        currentShieldHealth = config.hasShield ? config.shieldHealth : 0f;
        moveSpeed = config.moveSpeed;
        isInitialized = true;
        hasExploded = false;
        SetupVisuals();
    }

    private void SetupVisuals()
    {
        if (cachedRenderer == null || config == null) return;

        CreateEnemySprite();
        cachedRenderer.color = config.enemyColor;
        cachedRenderer.sortingOrder = 5;
    }

    private void CreateEnemySprite()
    {
        int size = Mathf.RoundToInt(32 * config.scale);
        var texture = new Texture2D(size, size);
        var colors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size * 0.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance < radius)
                {
                    float alpha = 1f - (distance / radius) * 0.3f;
                    colors[y * size + x] = new Color(config.enemyColor.r, config.enemyColor.g, config.enemyColor.b, alpha);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        cachedRenderer.sprite = sprite;
    }

    private void CachePlayerReference()
    {
        if (playerTransform == null)
        {
            if (ServiceLocator.TryGet<GameObject>(out var playerObject))
            {
                playerTransform = playerObject.transform;
            }
        }
    }

    private void Update()
    {
        if (!gameObject.activeSelf || !isInitialized || hasExploded || playerTransform == null)
            return;

        CachePlayerReference();
        if (playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        UpdateHealthRegeneration();
        UpdateMovement(distanceToPlayer);
        UpdateAttack(distanceToPlayer);
        UpdateTimers();
    }

    private void UpdateHealthRegeneration()
    {
        if (!config.regeneratesHealth || currentHealth >= config.maxHealth) return;

        healthRegenTimer += Time.deltaTime;
        if (healthRegenTimer >= 1f)
        {
            currentHealth = Mathf.Min(config.maxHealth, currentHealth + config.healthRegenRate);
            healthRegenTimer = 0f;
        }
    }

    private void UpdateMovement(float distanceToPlayer)
    {
        if (config.ShouldFlee(currentHealth) && !isRetreating)
        {
            isRetreating = true;
        }

        Vector2 targetPosition = CalculateTargetPosition(distanceToPlayer);
        MoveTowardsTarget(targetPosition);
    }

    private Vector2 CalculateTargetPosition(float distanceToPlayer)
    {
        Vector2 playerPos = playerTransform.position;
        Vector2 currentPos = transform.position;

        if (isRetreating)
        {
            Vector2 fleeDirection = (currentPos - playerPos).normalized;
            return currentPos + fleeDirection * moveSpeed * Time.deltaTime * 2f;
        }

        switch (config.movementType)
        {
            case EnemyMovementType.DirectChase:
                return playerPos;

            case EnemyMovementType.Stop:
                if (distanceToPlayer > config.optimalDistance)
                    return Vector2.MoveTowards(currentPos, playerPos, 0.1f);
                return currentPos;

            case EnemyMovementType.CircularOrbit:
                return CalculateOrbitPosition(playerPos, currentPos);

            case EnemyMovementType.ZigZag:
                return CalculateZigZagPosition(playerPos, currentPos);

            case EnemyMovementType.Wave:
                return CalculateWavePosition(playerPos, currentPos);

            case EnemyMovementType.Spiral:
                return CalculateSpiralPosition(playerPos, currentPos);

            default:
                return playerPos;
        }
    }

    private Vector2 CalculateOrbitPosition(Vector2 playerPos, Vector2 currentPos)
    {
        movementTimer += Time.deltaTime * config.movementFrequency;

        float angle = movementTimer;
        float orbitRadius = config.optimalDistance;

        Vector2 orbitCenter = playerPos;
        Vector2 orbitPosition = orbitCenter + new Vector2(
            Mathf.Cos(angle) * orbitRadius,
            Mathf.Sin(angle) * orbitRadius
        );

        return orbitPosition;
    }

    private Vector2 CalculateZigZagPosition(Vector2 playerPos, Vector2 currentPos)
    {
        movementTimer += Time.deltaTime * config.movementFrequency;

        Vector2 directionToPlayer = (playerPos - currentPos).normalized;
        Vector2 perpendicular = new Vector2(-directionToPlayer.y, directionToPlayer.x);

        float zigzagOffset = Mathf.Sin(movementTimer) * config.movementAmplitude;

        return currentPos + directionToPlayer * moveSpeed * Time.deltaTime +
               perpendicular * zigzagOffset * Time.deltaTime;
    }

    private Vector2 CalculateWavePosition(Vector2 playerPos, Vector2 currentPos)
    {
        movementTimer += Time.deltaTime * config.movementFrequency;

        Vector2 directionToPlayer = (playerPos - currentPos).normalized;
        Vector2 perpendicular = new Vector2(-directionToPlayer.y, directionToPlayer.x);

        float waveOffset = Mathf.Sin(movementTimer) * config.movementAmplitude;

        return Vector2.MoveTowards(currentPos, playerPos, moveSpeed * Time.deltaTime) +
               perpendicular * waveOffset * Time.deltaTime;
    }

    private Vector2 CalculateSpiralPosition(Vector2 playerPos, Vector2 currentPos)
    {
        movementTimer += Time.deltaTime * config.movementFrequency;

        float distance = Vector2.Distance(currentPos, playerPos);
        float angle = movementTimer;

        float targetRadius = Mathf.Max(config.optimalDistance, distance - moveSpeed * Time.deltaTime);

        return playerPos + new Vector2(
            Mathf.Cos(angle) * targetRadius,
            Mathf.Sin(angle) * targetRadius
        );
    }

    private void MoveTowardsTarget(Vector2 targetPosition)
    {
        if (cachedRigidbody == null) return;

        Vector2 currentPos = transform.position;
        Vector2 direction = (targetPosition - currentPos).normalized;

        float actualSpeed = isRetreating ? moveSpeed * 1.5f : moveSpeed;
        cachedRigidbody.linearVelocity = direction * actualSpeed;

        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }

    private void UpdateAttack(float distanceToPlayer)
    {
        if (config.attackType == EnemyAttackType.None ||
            !config.IsInAttackRange(transform.position, playerTransform.position))
            return;

        if (!config.canAttackWhileMoving && cachedRigidbody.linearVelocity.magnitude > 0.1f)
            return;

        attackTimer += Time.deltaTime;

        if (config.attackType == EnemyAttackType.BurstFire)
        {
            UpdateBurstAttack();
        }
        else if (attackTimer >= config.attackInterval)
        {
            PerformAttack();
            attackTimer = 0f;
        }
    }

    private void UpdateBurstAttack()
    {
        if (!isInBurstMode)
        {
            if (attackTimer >= config.burstCooldown)
            {
                isInBurstMode = true;
                burstShotsRemaining = config.burstCount;
                burstTimer = 0f;
                attackTimer = 0f;
            }
        }
        else
        {
            burstTimer += Time.deltaTime;
            if (burstTimer >= config.burstInterval && burstShotsRemaining > 0)
            {
                PerformAttack();
                burstShotsRemaining--;
                burstTimer = 0f;

                if (burstShotsRemaining <= 0)
                {
                    isInBurstMode = false;
                    attackTimer = 0f;
                }
            }
        }
    }

    private void PerformAttack()
    {
        ShowAttackWarning();

        switch (config.attackType)
        {
            case EnemyAttackType.SingleShot:
                FireSingleProjectile();
                break;
            case EnemyAttackType.BurstFire:
                FireSingleProjectile();
                break;
            case EnemyAttackType.Spray:
                FireSprayProjectiles();
                break;
            case EnemyAttackType.Homing:
                FireHomingProjectile();
                break;
            case EnemyAttackType.Explosive:
                FireExplosiveProjectile();
                break;
        }
    }

    private void ShowAttackWarning()
    {
        if (config.attackWarningDuration > 0f)
        {
            StartCoroutine(AttackWarningEffect());
        }
    }

    private IEnumerator AttackWarningEffect()
    {
        Color originalColor = cachedRenderer.color;
        cachedRenderer.color = config.attackWarningColor;

        yield return new WaitForSeconds(config.attackWarningDuration);

        cachedRenderer.color = originalColor;
    }

    private void FireSingleProjectile()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        CreateProjectile(direction, config.projectileSpeed, false);
    }

    private void FireSprayProjectiles()
    {
        Vector2 baseDirection = (playerTransform.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

        for (int i = 0; i < config.projectileCount; i++)
        {
            float angleOffset = (i - (config.projectileCount - 1) * 0.5f) * config.spreadAngle;
            float finalAngle = (baseAngle + angleOffset) * Mathf.Deg2Rad;

            Vector2 direction = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));
            CreateProjectile(direction, config.projectileSpeed, false);
        }
    }

    private void FireHomingProjectile()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        CreateProjectile(direction, config.projectileSpeed * 0.8f, true);
    }

    private void FireExplosiveProjectile()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        CreateProjectile(direction, config.projectileSpeed * 0.6f, false, true);
    }

    private void CreateProjectile(Vector2 direction, float speed, bool isHoming, bool isExplosive = false)
    {
        var projectileGO = new GameObject("EnemyProjectile");
        projectileGO.transform.position = transform.position;

        var renderer = projectileGO.AddComponent<SpriteRenderer>();
        CreateProjectileSprite(renderer, isHoming, isExplosive);

        var rb = projectileGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = direction * speed;

        var collider = projectileGO.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.15f;

        var projectile = projectileGO.AddComponent<EnemyProjectile>();
        projectile.Initialize(config.attackDamage, speed, config.projectileLifetime, isHoming, isExplosive);

        try { projectileGO.tag = "EnemyBullet"; } catch { }

        int bulletLayer = LayerMask.NameToLayer("EnemyBullet");
        if (bulletLayer != -1) projectileGO.layer = bulletLayer;

        Debug.Log($"Enemy {config.enemyName} created projectile at {projectileGO.transform.position}");
    }

    private void CreateProjectileSprite(SpriteRenderer renderer, bool isHoming, bool isExplosive)
    {
        int size = isExplosive ? 20 : (isHoming ? 16 : 12);
        var texture = new Texture2D(size, size);
        var colors = new Color[size * size];

        Color projectileColor = isExplosive ? Color.red : (isHoming ? Color.magenta : Color.yellow);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size * 0.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance < radius)
                {
                    float alpha = 1f - (distance / radius) * 0.2f;
                    colors[y * size + x] = new Color(projectileColor.r, projectileColor.g, projectileColor.b, alpha);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        renderer.sprite = sprite;
        renderer.sortingOrder = 10;
        renderer.color = projectileColor;

        Debug.Log($"Created projectile sprite: size={size}, color={projectileColor}, sortingOrder=10");
    }

    private void UpdateTimers()
    {
        movementTimer += Time.deltaTime;
    }

    public void TakeDamage(float damageAmount)
    {
        if (!isInitialized || hasExploded || config.immuneToPlayerBullets)
            return;

        float actualDamage = config.CalculateDamageReduction(damageAmount);

        if (currentShieldHealth > 0f)
        {
            float shieldDamage = Mathf.Min(currentShieldHealth, actualDamage);
            currentShieldHealth -= shieldDamage;
            actualDamage -= shieldDamage;
        }

        if (actualDamage > 0f)
        {
            currentHealth -= actualDamage;
        }

        Debug.Log($"Enemy {config.enemyName}: Took {actualDamage} damage, health={currentHealth}, shield={currentShieldHealth}");

        if (gameObject.activeInHierarchy)
            StartCoroutine(DamageFlash());

        if (config.stunDuration > 0f)
        {
            StartCoroutine(StunEffect());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlash()
    {
        if (cachedRenderer == null) yield break;

        Color originalColor = cachedRenderer.color;
        cachedRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        cachedRenderer.color = originalColor;
    }

    private IEnumerator StunEffect()
    {
        float originalSpeed = moveSpeed;
        moveSpeed = 0f;

        yield return new WaitForSeconds(config.stunDuration);

        moveSpeed = originalSpeed;
    }

    private void Die()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (config.canSplit && config.splitCount > 0)
        {
            CreateSplitEnemies();
        }

        CreateExperienceParticles();

        if (config.hasDeathAnimation)
        {
            StartCoroutine(DeathAnimation());
        }
        else
        {
            onReturnToPool?.Invoke();
        }
    }

    private void CreateSplitEnemies()
    {
        for (int i = 0; i < config.splitCount; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = transform.position + (Vector3)randomDirection * 0.5f;

            var splitEnemy = new GameObject($"Split_{config.enemyName}");
            splitEnemy.transform.position = spawnPos;

            var splitBehaviour = splitEnemy.AddComponent<EnemyBehaviour>();
            var splitConfig = Instantiate(config);
            splitConfig.maxHealth *= config.splitHealthRatio;
            splitConfig.scale *= 0.7f;
            splitConfig.canSplit = false;

            splitBehaviour.Initialize(splitConfig);
        }
    }

    private void CreateExperienceParticles()
    {
        int expToDrop = config.experienceDrop;
        int particleCount = Mathf.Min(3, expToDrop / 5 + 1);
        int expPerParticle = expToDrop / particleCount;
        int remainingExp = expToDrop % particleCount;

        for (int i = 0; i < particleCount; i++)
        {
            Vector3 particlePosition = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
            int thisParticleExp = expPerParticle + (i == 0 ? remainingExp : 0);
            ExperienceParticle.CreateExperienceParticle(particlePosition, thisParticleExp);
        }

        Debug.Log($"Enemy {config.enemyName}: Created {particleCount} experience particles with total exp={expToDrop}");
    }

    private IEnumerator DeathAnimation()
    {
        float timer = 0f;
        Vector3 originalScale = transform.localScale;
        Color originalColor = cachedRenderer.color;

        while (timer < config.deathAnimationDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / config.deathAnimationDuration;

            transform.localScale = originalScale * (1f + progress);

            Color newColor = originalColor;
            newColor.a = 1f - progress;
            cachedRenderer.color = newColor;

            yield return null;
        }

        onReturnToPool?.Invoke();
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        CreateExperienceParticles();
        PlayExplosionEffect();
        onReturnToPool?.Invoke();
        Debug.Log($"Enemy {config.enemyName}: Exploded, dealing {config.explosionDamage} damage to player");
    }

    private void PlayExplosionEffect()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ExplosionVisualEffect());
        }
    }

    private IEnumerator ExplosionVisualEffect()
    {
        Vector3 originalScale = transform.localScale;

        for (int i = 0; i < 5; i++)
        {
            transform.localScale = originalScale * (1f + i * 0.2f);
            cachedRenderer.color = i % 2 == 0 ? Color.white : config.enemyColor;
            yield return new WaitForSeconds(0.05f);
        }
    }

    public void ResetState()
    {
        if (config == null) return;

        currentHealth = config.maxHealth;
        currentShieldHealth = config.hasShield ? config.shieldHealth : 0f;
        moveSpeed = config.moveSpeed;
        hasExploded = false;
        isRetreating = false;
        attackTimer = 0f;
        burstShotsRemaining = 0;
        isInBurstMode = false;
        movementTimer = 0f;
        healthRegenTimer = 0f;

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * config.scale;

        if (cachedRenderer != null)
        {
            cachedRenderer.color = config.enemyColor;
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector2.zero;
            cachedRigidbody.angularVelocity = 0f;
        }

        gameObject.SetActive(false);
        onReturnToPool = null;

        Debug.Log($"Enemy {config.enemyName}: State reset complete");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized || hasExploded)
            return;

        Debug.Log($"Enemy {config.enemyName}: OnTriggerEnter2D with {other.gameObject.name} (tag: {other.tag})");

        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null && damageable.GetTeam() == DamageTeam.Player && damageable.IsAlive())
        {
            Debug.Log($"Enemy {config.enemyName}: Player collision! Exploding and dealing {config.explosionDamage} damage");

            var contactSource = new EnemyContactDamageSource(this);
            damageable.TakeDamage(config.explosionDamage, contactSource);
            Explode();
            return;
        }

        var bullet = other.GetComponent<Bullet>();
        if (bullet != null && bullet.GetOwner() == BulletOwner.Player)
        {
            Debug.Log($"Enemy {config.enemyName}: Hit by player bullet, damage={bullet.GetDamage()}");
            TakeDamage(bullet.GetDamage());
            return;
        }

        if (other.CompareTag("PlayerBullet"))
        {
            Debug.Log($"Enemy {config.enemyName}: Hit by tagged player bullet, damage=10");
            TakeDamage(10f);
            Destroy(other.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isInitialized || hasExploded)
            return;

        var damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null && damageable.GetTeam() == DamageTeam.Player && damageable.IsAlive())
        {
            Debug.Log($"Enemy {config.enemyName}: Physical collision with player! Exploding and dealing {config.explosionDamage} damage");

            var contactSource = new EnemyContactDamageSource(this);
            damageable.TakeDamage(config.explosionDamage, contactSource);
            Explode();
        }
    }

    private void OnDisable()
    {
        hasExploded = false;
        Debug.Log($"Enemy {config.enemyName}: Disabled");
    }

    private void OnDestroy()
    {
        if (cachedRenderer != null && cachedRenderer.sprite != null)
        {
            Destroy(cachedRenderer.sprite.texture);
            Destroy(cachedRenderer.sprite);
        }
        Debug.Log($"Enemy {config.enemyName}: Destroyed");
        onReturnToPool = null;
    }

    // Интерфейсные методы IEnemy
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => config?.maxHealth ?? 100f;
    public float GetCurrentShieldHealth() => currentShieldHealth;
    public float GetMoveSpeed() => moveSpeed;
    public float GetCollisionDamage() => config?.collisionDamage ?? 10f;
    public float GetExplosionDamage() => config?.explosionDamage ?? 20f;
    public bool IsAlive() => currentHealth > 0 && !hasExploded;
    public bool IsInitialized() => isInitialized;
    public bool IsRetreating() => isRetreating;
    public EnemyConfig GetConfig() => config;

    [ContextMenu("Take Damage (10)")]
    private void DebugTakeDamage() => TakeDamage(10f);

    [ContextMenu("Kill Enemy")]
    private void DebugKillEnemy() { currentHealth = 0; Die(); }

    [ContextMenu("Trigger Explosion")]
    private void DebugTriggerExplosion() => Explode();

    [ContextMenu("Show Enemy Info")]
    private void DebugShowInfo()
    {
        Debug.Log($"=== Enemy {config?.enemyName ?? gameObject.name} Info ===");
        Debug.Log($"Health: {currentHealth}/{GetMaxHealth()}");
        Debug.Log($"Shield: {currentShieldHealth}");
        Debug.Log($"Move Speed: {moveSpeed}");
        Debug.Log($"Attack Type: {config?.attackType}");
        Debug.Log($"Movement Type: {config?.movementType}");
        Debug.Log($"Is Retreating: {isRetreating}");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Has Exploded: {hasExploded}");
        Debug.Log($"Alive: {IsAlive()}");
        Debug.Log($"Experience Drop: {config?.experienceDrop ?? 0}");
        Debug.Log($"==================");
    }
}

 