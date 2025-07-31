using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyBehaviour : MonoBehaviour, IEnemy
{
    [Header("Enemy Configuration")]
    [SerializeField] private EnemyConfig enemyConfig;

    private float currentHealth;
    private float currentShieldHealth;
    private float moveSpeed;
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

        CacheComponents();
        SetupDamageReceiver();
    }

    private void CacheComponents()
    {
        cachedRigidbody = GetComponent<Rigidbody2D>();
        cachedRenderer = GetComponent<SpriteRenderer>();

        if (cachedRigidbody != null)
        {
            cachedRigidbody.gravityScale = 0f;
            cachedRigidbody.linearDamping = 2f;
        }

        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void SetupDamageReceiver()
    {
        if (GetComponent<EnemyDamageReceiver>() == null)
        {
            gameObject.AddComponent<EnemyDamageReceiver>();
        }
    }

    public void InitializeFromPrefab(System.Action onReturnToPool = null)
    {
        if (enemyConfig == null)
        {
            Debug.LogError($"Enemy {gameObject.name}: No EnemyConfig assigned!");
            return;
        }

        Initialize(enemyConfig, onReturnToPool);
    }

    public void Initialize(EnemyConfig config, System.Action onReturnToPool = null)
    {
        if (config == null)
        {
            Debug.LogError($"Enemy {gameObject.name}: EnemyConfig is null during initialization");
            return;
        }

        this.enemyConfig = config;
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
        Debug.Log($"Enemy {config.enemyName} (ID: {config.EnemyId}) initialized with health={currentHealth}");
    }

    public void InitializeForPool(EnemyConfig config = null)
    {
        if (config != null)
        {
            this.enemyConfig = config;
        }

        if (enemyConfig == null)
        {
            Debug.LogError($"Enemy {gameObject.name}: No EnemyConfig available for pool initialization");
            return;
        }

        currentHealth = enemyConfig.maxHealth;
        currentShieldHealth = enemyConfig.hasShield ? enemyConfig.shieldHealth : 0f;
        moveSpeed = enemyConfig.moveSpeed;
        isInitialized = true;
        hasExploded = false;
        SetupVisuals();
    }

    public void ResetForReuse(System.Action onReturnToPool = null)
    {
        if (enemyConfig == null)
        {
            Debug.LogError($"Enemy {gameObject.name}: No EnemyConfig available for reuse");
            return;
        }

        this.onReturnToPool = onReturnToPool;

        currentHealth = enemyConfig.maxHealth;
        currentShieldHealth = enemyConfig.hasShield ? enemyConfig.shieldHealth : 0f;
        moveSpeed = enemyConfig.moveSpeed;
        hasExploded = false;
        isRetreating = false;
        attackTimer = 0f;
        burstShotsRemaining = 0;
        isInBurstMode = false;
        movementTimer = 0f;
        healthRegenTimer = 0f;

        if (cachedRenderer != null)
        {
            cachedRenderer.color = enemyConfig.enemyColor;
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector2.zero;  
            cachedRigidbody.angularVelocity = 0f;
        }

        CachePlayerReference();
        Debug.Log($"Enemy {enemyConfig.enemyName} reset for reuse");
    }

    private void SetupVisuals()
    {
        if (cachedRenderer == null || enemyConfig == null) return;

        if (cachedRenderer.sprite == null)
        {
            CreateEnemySprite();
        }

        cachedRenderer.color = enemyConfig.enemyColor;
        cachedRenderer.sortingOrder = 5;
    }

    private void CreateEnemySprite()
    {
        int size = Mathf.RoundToInt(32 * enemyConfig.scale);
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
                    colors[y * size + x] = new Color(enemyConfig.enemyColor.r, enemyConfig.enemyColor.g, enemyConfig.enemyColor.b, alpha);
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
        if (!gameObject.activeSelf || !isInitialized || hasExploded || playerTransform == null || enemyConfig == null)
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
        if (!enemyConfig.regeneratesHealth || currentHealth >= enemyConfig.maxHealth) return;

        healthRegenTimer += Time.deltaTime;
        if (healthRegenTimer >= 1f)
        {
            currentHealth = Mathf.Min(enemyConfig.maxHealth, currentHealth + enemyConfig.healthRegenRate);
            healthRegenTimer = 0f;
        }
    }

    private void UpdateMovement(float distanceToPlayer)
    {
        if (enemyConfig.ShouldFlee(currentHealth) && !isRetreating)
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

        switch (enemyConfig.movementType)
        {
            case EnemyMovementType.DirectChase:
                return playerPos;

            case EnemyMovementType.Stop:
                if (distanceToPlayer > enemyConfig.optimalDistance)
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
        movementTimer += Time.deltaTime * enemyConfig.movementFrequency;
        float angle = movementTimer;
        float orbitRadius = enemyConfig.optimalDistance;
        Vector2 orbitCenter = playerPos;
        Vector2 orbitPosition = orbitCenter + new Vector2(
            Mathf.Cos(angle) * orbitRadius,
            Mathf.Sin(angle) * orbitRadius
        );
        return orbitPosition;
    }

    private Vector2 CalculateZigZagPosition(Vector2 playerPos, Vector2 currentPos)
    {
        movementTimer += Time.deltaTime * enemyConfig.movementFrequency;
        Vector2 directionToPlayer = (playerPos - currentPos).normalized;
        Vector2 perpendicular = new Vector2(-directionToPlayer.y, directionToPlayer.x);
        float zigzagOffset = Mathf.Sin(movementTimer) * enemyConfig.movementAmplitude;
        return currentPos + directionToPlayer * moveSpeed * Time.deltaTime +
               perpendicular * zigzagOffset * Time.deltaTime;
    }

    private Vector2 CalculateWavePosition(Vector2 playerPos, Vector2 currentPos)
    {
        movementTimer += Time.deltaTime * enemyConfig.movementFrequency;
        Vector2 directionToPlayer = (playerPos - currentPos).normalized;
        Vector2 perpendicular = new Vector2(-directionToPlayer.y, directionToPlayer.x);
        float waveOffset = Mathf.Sin(movementTimer) * enemyConfig.movementAmplitude;
        return Vector2.MoveTowards(currentPos, playerPos, moveSpeed * Time.deltaTime) +
               perpendicular * waveOffset * Time.deltaTime;
    }

    private Vector2 CalculateSpiralPosition(Vector2 playerPos, Vector2 currentPos)
    {
        movementTimer += Time.deltaTime * enemyConfig.movementFrequency;
        float distance = Vector2.Distance(currentPos, playerPos);
        float angle = movementTimer;
        float targetRadius = Mathf.Max(enemyConfig.optimalDistance, distance - moveSpeed * Time.deltaTime);
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
        if (enemyConfig.attackType == EnemyAttackType.None ||
            !enemyConfig.IsInAttackRange(transform.position, playerTransform.position))
            return;

        if (!enemyConfig.canAttackWhileMoving && cachedRigidbody.linearVelocity.magnitude > 0.1f)
            return;

        attackTimer += Time.deltaTime;

        if (enemyConfig.attackType == EnemyAttackType.BurstFire)
        {
            UpdateBurstAttack();
        }
        else if (attackTimer >= enemyConfig.attackInterval)
        {
            PerformAttack();
            attackTimer = 0f;
        }
    }

    private void UpdateBurstAttack()
    {
        if (!isInBurstMode)
        {
            if (attackTimer >= enemyConfig.burstCooldown)
            {
                isInBurstMode = true;
                burstShotsRemaining = enemyConfig.burstCount;
                burstTimer = 0f;
                attackTimer = 0f;
            }
        }
        else
        {
            burstTimer += Time.deltaTime;
            if (burstTimer >= enemyConfig.burstInterval && burstShotsRemaining > 0)
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

        switch (enemyConfig.attackType)
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
        if (enemyConfig.attackWarningDuration > 0f)
        {
            StartCoroutine(AttackWarningEffect());
        }
    }

    private IEnumerator AttackWarningEffect()
    {
        Color originalColor = cachedRenderer.color;
        cachedRenderer.color = enemyConfig.attackWarningColor;
        yield return new WaitForSeconds(enemyConfig.attackWarningDuration);
        cachedRenderer.color = originalColor;
    }

    private void FireSingleProjectile()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        CreateProjectile(direction, enemyConfig.projectileSpeed, false);
    }

    private void FireSprayProjectiles()
    {
        Vector2 baseDirection = (playerTransform.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;

        for (int i = 0; i < enemyConfig.projectileCount; i++)
        {
            float angleOffset = (i - (enemyConfig.projectileCount - 1) * 0.5f) * enemyConfig.spreadAngle;
            float finalAngle = (baseAngle + angleOffset) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));
            CreateProjectile(direction, enemyConfig.projectileSpeed, false);
        }
    }

    private void FireHomingProjectile()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        CreateProjectile(direction, enemyConfig.projectileSpeed * 0.8f, true);
    }

    private void FireExplosiveProjectile()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        CreateProjectile(direction, enemyConfig.projectileSpeed * 0.6f, false, true);
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
        projectile.Initialize(enemyConfig.attackDamage, speed, enemyConfig.projectileLifetime, isHoming, isExplosive);

        try { projectileGO.tag = "Enemy"; } catch { }

        int bulletLayer = LayerMask.NameToLayer("EnemyBullet");
        if (bulletLayer != -1) projectileGO.layer = bulletLayer;

        Debug.Log($"Enemy {enemyConfig.enemyName} created projectile at {projectileGO.transform.position}");
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
    }

    private void UpdateTimers()
    {
        movementTimer += Time.deltaTime;
    }

    public void TakeDamage(float damageAmount)
    {
        if (!isInitialized || hasExploded || enemyConfig.immuneToPlayerBullets)
            return;

        float actualDamage = enemyConfig.CalculateDamageReduction(damageAmount);

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

        Debug.Log($"Enemy {enemyConfig.enemyName}: Took {actualDamage} damage, health={currentHealth}, shield={currentShieldHealth}");

        if (gameObject.activeInHierarchy)
            StartCoroutine(DamageFlash());

        if (enemyConfig.stunDuration > 0f)
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
        yield return new WaitForSeconds(enemyConfig.stunDuration);
        moveSpeed = originalSpeed;
    }

    private void Die()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (enemyConfig.canSplit && enemyConfig.splitCount > 0)
        {
            CreateSplitEnemies();
        }

        CreateExperienceParticles();

        if (enemyConfig.hasDeathAnimation)
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
        for (int i = 0; i < enemyConfig.splitCount; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = transform.position + (Vector3)randomDirection * 0.5f;

            var splitEnemy = new GameObject($"Split_{enemyConfig.enemyName}");
            splitEnemy.transform.position = spawnPos;

            var splitBehaviour = splitEnemy.AddComponent<EnemyBehaviour>();
            var splitConfig = Instantiate(enemyConfig);
            splitConfig.maxHealth *= enemyConfig.splitHealthRatio;
            splitConfig.scale *= 0.7f;
            splitConfig.canSplit = false;

            splitBehaviour.Initialize(splitConfig);
        }
    }

    private void CreateExperienceParticles()
    {
        int expToDrop = enemyConfig.experienceDrop;
        int particleCount = Mathf.Min(3, expToDrop / 5 + 1);
        int expPerParticle = expToDrop / particleCount;
        int remainingExp = expToDrop % particleCount;

        for (int i = 0; i < particleCount; i++)
        {
            Vector3 particlePosition = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
            int thisParticleExp = expPerParticle + (i == 0 ? remainingExp : 0);
            ExperienceParticle.CreateExperienceParticle(particlePosition, thisParticleExp);
        }

        Debug.Log($"Enemy {enemyConfig.enemyName}: Created {particleCount} experience particles with total exp={expToDrop}");
    }

    private IEnumerator DeathAnimation()
    {
        float timer = 0f;
        Vector3 originalScale = transform.localScale;
        Color originalColor = cachedRenderer.color;

        while (timer < enemyConfig.deathAnimationDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / enemyConfig.deathAnimationDuration;

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
        Debug.Log($"Enemy {enemyConfig.enemyName}: Exploded, dealing {enemyConfig.explosionDamage} damage to player");
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
            cachedRenderer.color = i % 2 == 0 ? Color.white : enemyConfig.enemyColor;
            yield return new WaitForSeconds(0.05f);
        }
    }

    public void ResetState()
    {
        if (enemyConfig == null) return;

        currentHealth = enemyConfig.maxHealth;
        currentShieldHealth = enemyConfig.hasShield ? enemyConfig.shieldHealth : 0f;
        moveSpeed = enemyConfig.moveSpeed;
        hasExploded = false;
        isRetreating = false;
        attackTimer = 0f;
        burstShotsRemaining = 0;
        isInBurstMode = false;
        movementTimer = 0f;
        healthRegenTimer = 0f;

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * enemyConfig.scale;

        if (cachedRenderer != null)
        {
            cachedRenderer.color = enemyConfig.enemyColor;
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector2.zero;  
            cachedRigidbody.angularVelocity = 0f;
        }

        gameObject.SetActive(false);
        onReturnToPool = null;

        Debug.Log($"Enemy {enemyConfig.enemyName}: State reset complete");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized || hasExploded)
            return;

        Debug.Log($"Enemy {enemyConfig.enemyName}: OnTriggerEnter2D with {other.gameObject.name} (tag: {other.tag})");

        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null && damageable.GetTeam() == DamageTeam.Player && damageable.IsAlive())
        {
            Debug.Log($"Enemy {enemyConfig.enemyName}: Player collision! Exploding and dealing {enemyConfig.explosionDamage} damage");

            var contactSource = new EnemyContactDamageSource(this);
            damageable.TakeDamage(enemyConfig.explosionDamage, contactSource);
            Explode();
            return;
        }

        var bullet = other.GetComponent<Bullet>();
        if (bullet != null && bullet.GetOwner() == BulletOwner.Player)
        {
            Debug.Log($"Enemy {enemyConfig.enemyName}: Hit by player bullet, damage={bullet.GetDamage()}");
            TakeDamage(bullet.GetDamage());
            return;
        }

        try
        {
            if (other.CompareTag("PlayerBullet"))
            {
                Debug.Log($"Enemy {enemyConfig.enemyName}: Hit by tagged player bullet, damage=10");
                TakeDamage(10f);
                Destroy(other.gameObject);
            }
        }
        catch
        {
            // Игнорируем ошибки с тегами
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isInitialized || hasExploded)
            return;

        var damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null && damageable.GetTeam() == DamageTeam.Player && damageable.IsAlive())
        {
            Debug.Log($"Enemy {enemyConfig.enemyName}: Physical collision with player! Exploding and dealing {enemyConfig.explosionDamage} damage");

            var contactSource = new EnemyContactDamageSource(this);
            damageable.TakeDamage(enemyConfig.explosionDamage, contactSource);
            Explode();
        }
    }

    private void OnDisable()
    {
        hasExploded = false;
        Debug.Log($"Enemy {enemyConfig?.enemyName ?? gameObject.name}: Disabled");
    }

    private void OnDestroy()
    {
        if (Application.isPlaying && cachedRenderer != null && cachedRenderer.sprite != null)
        {
            if (cachedRenderer.sprite.texture != null)
            {
                Object.DestroyImmediate(cachedRenderer.sprite.texture, true);
            }
            Object.DestroyImmediate(cachedRenderer.sprite, true);
        }
        onReturnToPool = null;
    }

    // Interface methods
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => enemyConfig?.maxHealth ?? 100f;
    public float GetCurrentShieldHealth() => currentShieldHealth;
    public float GetMoveSpeed() => moveSpeed;
    public float GetCollisionDamage() => enemyConfig?.collisionDamage ?? 10f;
    public float GetExplosionDamage() => enemyConfig?.explosionDamage ?? 20f;
    public bool IsAlive() => currentHealth > 0 && !hasExploded;
    public bool IsInitialized() => isInitialized;
    public bool IsRetreating() => isRetreating;
    public EnemyConfig GetConfig() => enemyConfig;

    // Public methods for external access
    public void SetEnemyConfig(EnemyConfig config)
    {
        enemyConfig = config;
    }

    public string GetEnemyInfo()
    {
        if (enemyConfig == null) return "No config assigned";

        return $"=== Enemy {enemyConfig.enemyName} Info ===\n" +
               $"ID: {enemyConfig.enemyId}\n" +
               $"Tier: {enemyConfig.tier}\n" +
               $"Health: {currentHealth:F1}/{enemyConfig.maxHealth}\n" +
               $"Shield: {currentShieldHealth:F1}\n" +
               $"Move Speed: {moveSpeed}\n" +
               $"Attack Type: {enemyConfig.attackType}\n" +
               $"Movement Type: {enemyConfig.movementType}\n" +
               $"Difficulty: {enemyConfig.difficultyValue}\n" +
               $"Is Retreating: {isRetreating}\n" +
               $"Initialized: {isInitialized}\n" +
               $"Has Exploded: {hasExploded}\n" +
               $"Alive: {IsAlive()}";
    }

    [ContextMenu("Show Enemy Info")]
    private void DebugShowInfo()
    {
        Debug.Log(GetEnemyInfo());
    }

    [ContextMenu("Take Damage (10)")]
    private void DebugTakeDamage() => TakeDamage(10f);

    [ContextMenu("Kill Enemy")]
    private void DebugKillEnemy()
    {
        currentHealth = 0;
        Die();
    }

    [ContextMenu("Trigger Explosion")]
    private void DebugTriggerExplosion() => Explode();
}