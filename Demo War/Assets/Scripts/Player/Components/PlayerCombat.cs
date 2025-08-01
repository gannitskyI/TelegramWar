using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour, IInitializable
{
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackInterval = 0.5f;
    [SerializeField] private float bulletDamage = 10f;
    [SerializeField] private float bulletSpeed = 15f;

    [Header("Bullet Settings")]
    [SerializeField] private Transform firePoint;

    public int InitializationOrder => 15;

    private float attackTimer;
    private EnemyDamageReceiver currentTarget;
    private bool canAttack = true;
    private SystemsConfiguration config;

    private float targetScanInterval = 0.1f;
    private float targetScanTimer;

    public IEnumerator Initialize()
    {
        config = ServiceLocator.Get<SystemsConfiguration>();
        if (config != null)
        {
            bulletDamage = config.playerDamage;
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

        yield return null;
    }

    void Update()
    {
        if (!canAttack) return;

        attackTimer += Time.deltaTime;
        targetScanTimer += Time.deltaTime;

        if (targetScanTimer >= targetScanInterval)
        {
            FindNearestEnemy();
            targetScanTimer = 0f;
        }

        if (attackTimer >= attackInterval && currentTarget != null)
        {
            Attack(currentTarget.gameObject);
            attackTimer = 0f;
        }
    }

    private void FindNearestEnemy()
    {
        currentTarget = EnemyRegistry.Instance.FindClosestEnemy(transform.position, attackRange);
    }

    private void Attack(GameObject target)
    {
        if (target == null) return;

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
        var rb = bulletObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * bulletSpeed;
        }

        if (direction != Vector3.zero)
        {
            bulletObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        }

        string sourceName = $"Player Bullet (from {gameObject.name})";
        bulletObject.SetupProjectileDamageSource(bulletDamage, DamageTeam.Player, sourceName, gameObject);

        var projectileLifetime = bulletObject.AddComponent<ProjectileLifetime>();
        projectileLifetime.Initialize(5f);
    }

    public void UpgradeDamage(float multiplier)
    {
        bulletDamage *= multiplier;
    }

    public void UpgradeAttackSpeed(float multiplier)
    {
        attackInterval *= (1f / multiplier);
        attackInterval = Mathf.Max(0.1f, attackInterval);
    }

    public void UpgradeRange(float multiplier)
    {
        attackRange *= multiplier;
    }

    public void SetCanAttack(bool canAttack) => this.canAttack = canAttack;
    public float GetAttackRange() => attackRange;
    public float GetAttackInterval() => attackInterval;
    public float GetBulletDamage() => bulletDamage;
    public bool HasTarget() => currentTarget != null;

    public void Cleanup()
    {
        currentTarget = null;
        canAttack = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = canAttack ? Color.cyan : Color.gray;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
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
            Debug.Log($"[PROJECTILE] Lifetime expired: {gameObject.name}");
            Destroy(gameObject);
        }
    }
}