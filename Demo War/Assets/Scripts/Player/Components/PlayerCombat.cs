using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour, IInitializable
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Transform firePoint;

    public int InitializationOrder => 15;

    private float attackTimer;
    private EnemyDamageReceiver currentTarget;
    private bool canAttack = true;
    private float targetScanInterval = 0.1f;
    private float targetScanTimer;
    private IBulletPool bulletPool;

    public IEnumerator Initialize()
    {
        if (playerStats == null)
        {
            playerStats = Resources.Load<PlayerStats>("PlayerStats");
            if (playerStats == null)
            {
                playerStats = ScriptableObject.CreateInstance<PlayerStats>();
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
        }
        bulletPool = ServiceLocator.TryGet<IBulletPool>(out var pool) ? pool : null;
        yield return null;
    }

    private void OnStatsChanged(PlayerStats stats)
    {
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
        FireBullet(firePoint.position, direction);
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        }
    }

    private void FireBullet(Vector3 startPosition, Vector3 direction)
    {
        GameObject bulletObject = null;
        if (bulletPool != null)
        {
            bulletObject = bulletPool.GetBullet(startPosition);
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
        var projectileLifetime = bulletGO.AddComponent<ProjectileLifetime>();
        projectileLifetime.Initialize(5f);
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
        string sourceName = $"Player Bullet";
        bulletObject.SetupProjectileDamageSource(currentDamage, DamageTeam.Player, sourceName, gameObject);
        var projectileLifetime = bulletObject.GetComponent<ProjectileLifetime>();
        if (projectileLifetime == null)
        {
            projectileLifetime = bulletObject.AddComponent<ProjectileLifetime>();
        }
        projectileLifetime.Initialize(5f);
        bulletObject.SetActive(true);
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
            gameObject.SetActive(false);
        }
    }
}

public interface IBulletPool
{
    GameObject GetBullet(Vector3 position);
}
