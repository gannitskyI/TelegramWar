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
    private EnemyDamageReceiver damageReceiver;

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
        damageReceiver = GetComponent<EnemyDamageReceiver>();
        if (damageReceiver == null)
        {
            damageReceiver = gameObject.AddComponent<EnemyDamageReceiver>();
        }
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

        EnemyRegistry.Instance.RegisterEnemy(damageReceiver);
        gameObject.SetActive(true);
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
        EnemyRegistry.Instance.RegisterEnemy(damageReceiver);
    }

    private void SetupVisuals()
    {
        if (cachedRenderer == null || enemyConfig == null) return;

        string spriteKey = GetSpriteKeyForTier(enemyConfig.tier);
        cachedRenderer.sprite = SpriteCache.GetSprite(spriteKey);
        cachedRenderer.color = enemyConfig.enemyColor;
        cachedRenderer.sortingOrder = 5;
    }

    private string GetSpriteKeyForTier(EnemyTier tier)
    {
        return tier switch
        {
            EnemyTier.Tier1 => "enemy_basic",
            EnemyTier.Tier2 => "enemy_basic",
            EnemyTier.Tier3 => "enemy_elite",
            EnemyTier.Tier4 => "enemy_boss",
            EnemyTier.Tier5 => "enemy_boss",
            _ => "enemy_basic"
        };
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

    private void UpdateMovement(float distanceToPlayer)
    {
        if (enemyConfig.ShouldFlee(currentHealth) && !isRetreating)
        {
            isRetreating = true;
        }

        Vector2 targetPosition = CalculateTargetPosition(distanceToPlayer);
        MoveTowardsTarget(targetPosition);
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

    private Vector2 CalculateTargetPosition(float distanceToPlayer)
    {
        Vector2 playerPos = playerTransform.position;
        Vector2 currentPos = transform.position;

        if (isRetreating)
        {
            Vector2 fleeDirection = (currentPos - playerPos).normalized;
            return currentPos + fleeDirection * moveSpeed * Time.deltaTime * 2f;
        }

        return enemyConfig.movementType switch
        {
            EnemyMovementType.DirectChase => playerPos,
            EnemyMovementType.Stop => distanceToPlayer > enemyConfig.optimalDistance
                ? Vector2.MoveTowards(currentPos, playerPos, 0.1f)
                : currentPos,
            _ => playerPos
        };
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

    private void UpdateAttack(float distanceToPlayer)
    {
        if (enemyConfig.attackType == EnemyAttackType.None ||
            !enemyConfig.IsInAttackRange(transform.position, playerTransform.position))
            return;

        if (!enemyConfig.canAttackWhileMoving && cachedRigidbody.linearVelocity.magnitude > 0.1f)
            return;

        attackTimer += Time.deltaTime;

        if (attackTimer >= enemyConfig.attackInterval)
        {
            PerformAttack();
            attackTimer = 0f;
        }
    }

    private void PerformAttack()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        CreateProjectile(direction, enemyConfig.projectileSpeed, false);
    }

    private void CreateProjectile(Vector2 direction, float speed, bool isHoming)
    {
        var projectileGO = new GameObject("EnemyProjectile");
        projectileGO.transform.position = transform.position;

        var renderer = projectileGO.AddComponent<SpriteRenderer>();
        string spriteKey = isHoming ? "projectile_homing" : "projectile_basic";
        renderer.sprite = SpriteCache.GetSprite(spriteKey);
        renderer.sortingOrder = 10;

        var rb = projectileGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = direction * speed;

        var collider = projectileGO.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.15f;

        var projectile = projectileGO.AddComponent<EnemyProjectile>();
        projectile.Initialize(enemyConfig.attackDamage, speed, enemyConfig.projectileLifetime, isHoming, false);

        try { projectileGO.tag = "Enemy"; } catch { }

        int bulletLayer = LayerMask.NameToLayer("EnemyBullet");
        if (bulletLayer != -1) projectileGO.layer = bulletLayer;
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

        if (gameObject.activeInHierarchy)
            StartCoroutine(DamageFlash());

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

    private void Die()
    {
        if (hasExploded) return;
        hasExploded = true;

        CreateExperienceParticles();
        onReturnToPool?.Invoke();
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
    }

    public void ResetState()
    {
        if (enemyConfig == null) return;

        EnemyRegistry.Instance.UnregisterEnemy(damageReceiver);

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
    }

    private void OnDestroy()
    {
        if (damageReceiver != null)
        {
            EnemyRegistry.Instance.UnregisterEnemy(damageReceiver);
        }
        onReturnToPool = null;
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => enemyConfig?.maxHealth ?? 100f;
    public bool IsAlive() => currentHealth > 0 && !hasExploded;
    public bool IsInitialized() => isInitialized;
    public EnemyConfig GetConfig() => enemyConfig;
    public float GetExplosionDamage() => enemyConfig?.explosionDamage ?? 20f;
}