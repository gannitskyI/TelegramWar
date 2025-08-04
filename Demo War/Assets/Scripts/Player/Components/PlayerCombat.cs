using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour, IInitializable
{
    [Header("Player Stats Reference")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Bullet Settings")]
    [SerializeField] private Transform firePoint;

    public int InitializationOrder => 15;

    private float attackTimer;
    private EnemyDamageReceiver currentTarget;
    private bool canAttack = true;

    private float targetScanInterval = 0.1f;
    private float targetScanTimer;

    public IEnumerator Initialize()
    {
        if (playerStats == null)
        {
            playerStats = Resources.Load<PlayerStats>("PlayerStats");
            if (playerStats == null)
            {
                Debug.LogError("PlayerStats not found! Creating default stats.");
                playerStats = CreateDefaultStats();
            }
        }

        if (firePoint == null)
        {
            var firePointGO = new GameObject("FirePoint");
            firePointGO.transform.SetParent(transform);
            firePointGO.transform.localPosition = Vector3.zero;
            firePoint = firePointGO.transform;
        }

        attackTimer = 0f;
        targetScanTimer = 0f;

        ServiceLocator.Register<PlayerStats>(playerStats);

        if (playerStats != null)
        {
            playerStats.OnStatsChanged += OnStatsChanged;
            Debug.Log($"PlayerCombat initialized with stats: Damage={playerStats.FinalDamage:F1}, AttackSpeed={playerStats.FinalAttackSpeed:F1}");
        }

        yield return null;
    }

    private PlayerStats CreateDefaultStats()
    {
        var stats = ScriptableObject.CreateInstance<PlayerStats>();
        Debug.LogWarning("Using temporary PlayerStats. Please create PlayerStats asset in Resources folder.");
        return stats;
    }

    private void OnStatsChanged(PlayerStats stats)
    {
        Debug.Log($"Player stats updated: Damage={stats.FinalDamage:F1}, AttackSpeed={stats.FinalAttackSpeed:F1}");
    }

    void Update()
    {
        if (!canAttack || playerStats == null) return;

        attackTimer += Time.deltaTime;
        targetScanTimer += Time.deltaTime;

        if (targetScanTimer >= targetScanInterval)
        {
            FindNearestEnemy();
            targetScanTimer = 0f;
        }

        if (attackTimer >= playerStats.AttackInterval && currentTarget != null)
        {
            Attack(currentTarget.gameObject);
            attackTimer = 0f;
        }
    }

    private void FindNearestEnemy()
    {
        currentTarget = EnemyRegistry.Instance.FindClosestEnemy(transform.position, playerStats.FinalAttackRange);
    }

    private void Attack(GameObject target)
    {
        if (target == null || playerStats == null) return;

        Vector3 direction = (target.transform.position - firePoint.position).normalized;
        CreatePlayerBullet(firePoint.position, direction);

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        }

        WebGLHelper.TriggerHapticFeedback("light");
    }

    private async void CreatePlayerBullet(Vector3 startPosition, Vector3 direction)
    {
        var addressableManager = ServiceLocator.Get<AddressableManager>();
        GameObject bulletObject = null;

        if (addressableManager != null)
        {
            bulletObject = await addressableManager.InstantiateAsync("PlayerBullet", startPosition);
        }

        if (bulletObject == null)
        {
            bulletObject = CreateFallbackPlayerBullet(startPosition);
        }

        SetupPlayerBullet(bulletObject, direction);
    }

    private GameObject CreateFallbackPlayerBullet(Vector3 position)
    {
        var bulletGO = new GameObject("PlayerBullet");
        bulletGO.transform.position = position;

        var renderer = bulletGO.AddComponent<SpriteRenderer>();
        renderer.sprite = SpriteCache.GetSprite("bullet_player");
        renderer.sortingOrder = 10;

        var rb = bulletGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var collider = bulletGO.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.1f;

        return bulletGO;
    }

    private void SetupPlayerBullet(GameObject bulletObject, Vector3 direction)
    {
        if (playerStats == null) return;

        var rb = bulletObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * playerStats.FinalBulletSpeed;
        }

        if (direction != Vector3.zero)
        {
            bulletObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        }

        float currentDamage = playerStats.FinalDamage;
        string sourceName = $"Player Bullet ({currentDamage:F1} dmg)";
        bulletObject.SetupProjectileDamageSource(currentDamage, DamageTeam.Player, sourceName, gameObject);

        var projectileLifetime = bulletObject.AddComponent<ProjectileLifetime>();
        projectileLifetime.Initialize(5f);
    }

    public void SetCanAttack(bool canAttack) => this.canAttack = canAttack;

    public float GetAttackRange() => playerStats?.FinalAttackRange ?? 5f;
    public float GetAttackInterval() => playerStats?.AttackInterval ?? 0.5f;
    public float GetBulletDamage() => playerStats?.FinalDamage ?? 10f;
    public bool HasTarget() => currentTarget != null;

    public void Cleanup()
    {
        currentTarget = null;
        canAttack = false;

        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= OnStatsChanged;
        }
    }

    void OnDrawGizmosSelected()
    {
        float range = playerStats?.FinalAttackRange ?? 5f;
        Gizmos.color = canAttack ? Color.cyan : Color.gray;
        Gizmos.DrawWireSphere(transform.position, range);

        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
    }

    [ContextMenu("Log Player Stats")]
    private void LogPlayerStats()
    {
        if (playerStats != null)
        {
            Debug.Log(playerStats.GetStatsDebugInfo());
        }
        else
        {
            Debug.LogError("PlayerStats not assigned!");
        }
    }
}

public class ProjectileLifetime : MonoBehaviour
{
    private float lifetime;
    private float timer;

    public void Initialize(float projectileLifetime)
    {
        lifetime = projectileLifetime;
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}