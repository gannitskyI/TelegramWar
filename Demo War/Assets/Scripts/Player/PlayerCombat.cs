using System.Collections;
using System.Collections.Generic;
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
    private GameObject nearestEnemy;
    private bool canAttack = true;
    private SystemsConfiguration config;

    private List<GameObject> enemyTargets = new List<GameObject>();
    private float targetScanInterval = 0.1f;
    private float targetScanTimer;

    public IEnumerator Initialize()
    {
        Debug.Log("[PLAYER COMBAT] Initialization started");

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

        Debug.Log("[PLAYER COMBAT] Initialized successfully");
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

        if (attackTimer >= attackInterval && nearestEnemy != null)
        {
            Attack(nearestEnemy);
            attackTimer = 0f;
        }
    }

    private void FindNearestEnemy()
    {
        enemyTargets.Clear();
        var allEnemyReceivers = Object.FindObjectsOfType<EnemyDamageReceiver>();

        GameObject closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (var enemyReceiver in allEnemyReceivers)
        {
            if (enemyReceiver == null || !enemyReceiver.IsAlive()) continue;

            var enemy = enemyReceiver.gameObject;
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance <= attackRange)
            {
                enemyTargets.Add(enemy);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }

        if (closestEnemy != null)
        {
            nearestEnemy = closestEnemy;
        }
        else if (nearestEnemy != null)
        {
            if (Vector3.Distance(transform.position, nearestEnemy.transform.position) > attackRange)
            {
                nearestEnemy = null;
            }
        }
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
        Debug.Log($"[PLAYER COMBAT] Attacked enemy at distance: {Vector3.Distance(transform.position, target.transform.position):F1}");
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
        CreatePlayerBulletSprite(renderer);

        var rb = bulletGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var collider = bulletGO.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.1f;

        Debug.Log("[PLAYER COMBAT] Created fallback player bullet");
        return bulletGO;
    }

    private void CreatePlayerBulletSprite(SpriteRenderer renderer)
    {
        int size = 16;
        var texture = new Texture2D(size, size);
        var colors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance < size * 0.35f)
                {
                    colors[y * size + x] = Color.cyan;
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        renderer.sprite = sprite;
        renderer.sortingOrder = 10;
        renderer.color = Color.cyan;
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

        Debug.Log($"[PLAYER COMBAT] Created player bullet: {sourceName}, Damage: {bulletDamage}, Speed: {bulletSpeed}");
    }

    public void UpgradeDamage(float multiplier)
    {
        bulletDamage *= multiplier;
        Debug.Log($"[PLAYER COMBAT] Damage upgraded to: {bulletDamage}");
    }

    public void UpgradeAttackSpeed(float multiplier)
    {
        attackInterval *= (1f / multiplier);
        attackInterval = Mathf.Max(0.1f, attackInterval);
        Debug.Log($"[PLAYER COMBAT] Attack speed upgraded, interval: {attackInterval:F2}s");
    }

    public void UpgradeRange(float multiplier)
    {
        attackRange *= multiplier;
        Debug.Log($"[PLAYER COMBAT] Attack range upgraded to: {attackRange}");
    }

    public void SetCanAttack(bool canAttack) => this.canAttack = canAttack;
    public float GetAttackRange() => attackRange;
    public float GetAttackInterval() => attackInterval;
    public float GetBulletDamage() => bulletDamage;
    public int GetTargetsInRange() => enemyTargets.Count;
    public bool HasTarget() => nearestEnemy != null;

    public void Cleanup()
    {
        nearestEnemy = null;
        enemyTargets.Clear();
        canAttack = false;
        Debug.Log("[PLAYER COMBAT] Cleaned up");
    }

    public void ForceFindTarget() => FindNearestEnemy();
    public void ForceAttack() { if (nearestEnemy != null) Attack(nearestEnemy); }
    public List<GameObject> GetAllTargetsInRange() => new List<GameObject>(enemyTargets);
    public bool IsEnemyInRange(GameObject enemy) => enemy != null && Vector3.Distance(transform.position, enemy.transform.position) <= attackRange;
    public float GetTimeSinceLastAttack() => attackTimer;
    public float GetTimeToNextAttack() => Mathf.Max(0, attackInterval - attackTimer);
    public bool CanAttackNow() => canAttack && attackTimer >= attackInterval && nearestEnemy != null;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = canAttack ? Color.cyan : Color.gray;

        Vector3 center = transform.position;
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + Vector3.right * attackRange;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * attackRange;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }

        if (nearestEnemy != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, nearestEnemy.transform.position);

            if (firePoint != null)
            {
                Gizmos.color = Color.green;
                Vector3 direction = (nearestEnemy.transform.position - firePoint.position).normalized;
                Gizmos.DrawRay(firePoint.position, direction * 2f);
            }
        }

        Gizmos.color = Color.cyan;
        foreach (var target in enemyTargets)
        {
            if (target != null && target != nearestEnemy)
            {
                Gizmos.DrawWireSphere(target.transform.position, 0.3f);
            }
        }

        if (firePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
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