using UnityEngine;

public class EnemyBehaviour : MonoBehaviour, IEnemy
{
    private float currentHealth;
    private float moveSpeed;
    private float damage;
    private EnemyConfig config;
    private System.Action onReturnToPool;
    private bool isInitialized;

    public void Initialize(EnemyConfig config, System.Action onReturnToPool = null)
    {
        if (config == null)
        {
            Debug.LogError("EnemyConfig is null!");
            return;
        }

        this.config = config;
        this.onReturnToPool = onReturnToPool;
        currentHealth = config.maxHealth;
        moveSpeed = config.moveSpeed;
        damage = config.damage;
        isInitialized = true;

        Debug.Log($"[ENEMY DEBUG] {gameObject.name} initialized with health: {currentHealth}");
        gameObject.SetActive(true);
    }

    public void InitializeForPool(EnemyConfig config)
    {
        if (config == null)
        {
            Debug.LogError("EnemyConfig is null in pool initialization!");
            return;
        }

        this.config = config;
        currentHealth = config.maxHealth;
        moveSpeed = config.moveSpeed;
        damage = config.damage;
        isInitialized = true;
    }

    public void ResetState()
    {
        if (config != null)
        {
            currentHealth = config.maxHealth;
        }
        else
        {
            currentHealth = 100f;
            Debug.LogWarning("Config is null when resetting enemy state");
        }

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        gameObject.SetActive(false);
        onReturnToPool = null;

        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void Update()
    {
        if (!gameObject.activeSelf || !isInitialized) return;

        if (ServiceLocator.TryGet<GameObject>(out var player) && player != null)
        {
            Vector3 direction = (player.transform.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
            }
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[ENEMY DEBUG] Enemy is not initialized, cannot take damage");
            return;
        }

        Debug.Log($"[ENEMY DEBUG] {gameObject.name} taking {damageAmount} damage. Health: {currentHealth} -> {currentHealth - damageAmount}");

        currentHealth -= damageAmount;

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DamageFlash());
        }

        if (currentHealth <= 0)
        {
            Debug.Log($"[ENEMY DEBUG] {gameObject.name} died from damage. Final health: {currentHealth}");
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"[ENEMY DEBUG] {gameObject.name} Die() called. Stack trace: {System.Environment.StackTrace}");

        if (config != null)
        {
            CreateExperienceParticles();
        }

        PlayDeathEffect();
        onReturnToPool?.Invoke();
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
            int thisParticleExp = expPerParticle;
            if (i == 0) thisParticleExp += remainingExp;

            ExperienceParticle.CreateExperienceParticle(particlePosition, thisParticleExp);
        }

        Debug.Log($"[ENEMY DEBUG] Created {particleCount} experience particles with total {expToDrop} experience");
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            var originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = originalColor;
        }
    }

    private void PlayDeathEffect()
    {
        Debug.Log($"[ENEMY DEBUG] {gameObject.name} death effect played!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[ENEMY DEBUG] {gameObject.name} collision with {other.gameObject.name} (tag: {other.tag})");

        var playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            Debug.Log($"[ENEMY DEBUG] {gameObject.name} collided with player, dealing {damage} damage but NOT dying");
            playerHealth.TakeDamage(damage);
            return;
        }

        var bullet = other.GetComponent<Bullet>();
        if (bullet != null)
        {
            Debug.Log($"[ENEMY DEBUG] {gameObject.name} hit by bullet. Owner: {bullet.GetOwner()}, Damage: {bullet.GetDamage()}");

            if (bullet.GetOwner() == BulletOwner.Player)
            {
                TakeDamage(bullet.GetDamage());
                return;
            }
            else
            {
                Debug.Log($"[ENEMY DEBUG] {gameObject.name} ignoring non-player bullet");
                return;
            }
        }

        if (bullet == null && IsPlayerBulletByName(other.gameObject))
        {
            Debug.Log($"[ENEMY DEBUG] {gameObject.name} hit by fallback player bullet, dealing 10 damage");
            TakeDamage(10f);
            Destroy(other.gameObject);
        }
        else
        {
            Debug.Log($"[ENEMY DEBUG] {gameObject.name} ignoring collision with {other.gameObject.name}");
        }
    }

    private bool IsPlayerBulletByName(GameObject bulletObject)
    {
        bool result = bulletObject.name.Contains("PlayerBullet") ||
               bulletObject.name.Contains("Player Bullet") ||
               (bulletObject.name.Contains("Bullet") && !bulletObject.name.Contains("Enemy"));

        Debug.Log($"[ENEMY DEBUG] IsPlayerBulletByName({bulletObject.name}) = {result}");
        return result;
    }

    private void OnDisable()
    {
        Debug.Log($"[ENEMY DEBUG] {gameObject.name} disabled");
    }

    private void OnDestroy()
    {
        Debug.Log($"[ENEMY DEBUG] {gameObject.name} destroyed");
        onReturnToPool = null;
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => config?.maxHealth ?? 100f;
    public float GetMoveSpeed() => moveSpeed;
    public float GetDamage() => damage;
    public bool IsAlive() => currentHealth > 0;
    public bool IsInitialized() => isInitialized;

    [ContextMenu("Take Damage (10)")]
    private void DebugTakeDamage()
    {
        TakeDamage(10f);
    }

    [ContextMenu("Kill Enemy")]
    private void DebugKillEnemy()
    {
        currentHealth = 0;
        Die();
    }

    [ContextMenu("Show Enemy Info")]
    private void DebugShowInfo()
    {
        Debug.Log($"=== Enemy Info ===");
        Debug.Log($"Health: {currentHealth}/{GetMaxHealth()}");
        Debug.Log($"Move Speed: {moveSpeed}");
        Debug.Log($"Damage: {damage}");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Alive: {IsAlive()}");
        Debug.Log($"Experience Drop: {config?.experienceDrop ?? 0}");
        Debug.Log($"==================");
    }
}