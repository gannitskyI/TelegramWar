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
    [SerializeField] private Transform firePoint; // ����� ��������

    public int InitializationOrder => 15;

    private float attackTimer;
    private GameObject nearestEnemy;
    private bool canAttack = true;
    private SystemsConfiguration config;

    // ��� ��� �����������
    private List<GameObject> enemyTargets = new List<GameObject>();
    private float targetScanInterval = 0.1f; // ��������� ���� ������ 0.1 �������
    private float targetScanTimer;

    public IEnumerator Initialize()
    {
        Debug.Log("PlayerCombat initialization started");

        // �������� ������������
        config = ServiceLocator.Get<SystemsConfiguration>();
        if (config != null)
        {
            bulletDamage = config.playerDamage;
        }

        // ���� ��� firePoint, ������� ���
        if (firePoint == null)
        {
            var firePointGO = new GameObject("FirePoint");
            firePointGO.transform.SetParent(transform);
            firePointGO.transform.localPosition = Vector3.zero;
            firePoint = firePointGO.transform;
        }

        attackTimer = 0f;
        targetScanTimer = 0f;

        Debug.Log("PlayerCombat initialized");
        yield return null;
    }

    void Update()
    {
        if (!canAttack) return;

        // ��������� �������
        attackTimer += Time.deltaTime;
        targetScanTimer += Time.deltaTime;

        // ��������� ���� ���� ��� �����������
        if (targetScanTimer >= targetScanInterval)
        {
            FindNearestEnemy();
            targetScanTimer = 0f;
        }

        // ������� ���� ���� ���� � ������ ���������� �������
        if (attackTimer >= attackInterval && nearestEnemy != null)
        {
            Attack(nearestEnemy);
            attackTimer = 0f;
        }
    }

    private void FindNearestEnemy()
    {
        // ������� ������ � ������� ���� ������ ����� ����������
        enemyTargets.Clear();
        var allEnemyBehaviours = Object.FindObjectsOfType<EnemyBehaviour>();

        GameObject closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (var enemyBehaviour in allEnemyBehaviours)
        {
            if (enemyBehaviour == null || !enemyBehaviour.IsAlive()) continue;

            var enemy = enemyBehaviour.gameObject;
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            // ��������� ��������� �� ���� � ������� �����
            if (distance <= attackRange)
            {
                enemyTargets.Add(enemy);

                // ������� ����������
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }

        // ��������� ���� ������ ���� ����� ���������� ��� �������� �������
        if (closestEnemy != null)
        {
            nearestEnemy = closestEnemy;
        }
        else if (nearestEnemy != null)
        {
            // ���������, ��� ������� ���� ��� ��� � �������
            if (Vector3.Distance(transform.position, nearestEnemy.transform.position) > attackRange)
            {
                nearestEnemy = null;
            }
        }
    }

    private void Attack(GameObject target)
    {
        if (target == null) return;

        // ��������� ����������� � ����
        Vector3 direction = (target.transform.position - firePoint.position).normalized;

        // ������� ����
        CreateBullet(firePoint.position, direction);

        // ������������ ������ � ������� ����� (�����������)
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        }

        // Haptic feedback
        WebGLHelper.TriggerHapticFeedback("light");

        Debug.Log($"Player attacked enemy at distance: {Vector3.Distance(transform.position, target.transform.position):F1}");
    }

    private async void CreateBullet(Vector3 startPosition, Vector3 direction)
    {
        // �������� ��������� ������ ���� ����� Addressables
        var addressableManager = ServiceLocator.Get<AddressableManager>();
        GameObject bulletObject = null;

        if (addressableManager != null)
        {
            try
            {
                bulletObject = await addressableManager.InstantiateAsync("PlayerBullet", startPosition);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to load bullet prefab: {e.Message}. Creating fallback bullet.");
            }
        }

        // ���� �� ������� ��������� ������, ������� ������� ����
        if (bulletObject == null)
        {
            bulletObject = CreateFallbackBullet(startPosition);
        }

        // ����������� ����
        var bullet = bulletObject.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = bulletObject.AddComponent<Bullet>();
        }

        // �������������� ���� � ����������� (��������� ��� ��� ���� ������)
        bullet.Initialize(bulletDamage, bulletSpeed, direction, BulletOwner.Player);
    }

    private GameObject CreateFallbackBullet(Vector3 position)
    {
        // ������� ������� ���� ���� ������ �� ����������
        var bulletGO = new GameObject("PlayerBullet");
        bulletGO.transform.position = position;

        // ��������� ������
        var renderer = bulletGO.AddComponent<SpriteRenderer>();

        // ������� ������� ������ ����
        var texture = new Texture2D(16, 16);
        var colors = new Color[16 * 16];

        for (int i = 0; i < colors.Length; i++)
        {
            float x = (i % 16) - 8f;
            float y = (i / 16) - 8f;
            float distance = Mathf.Sqrt(x * x + y * y);
            colors[i] = distance < 6f ? Color.yellow : Color.clear;
        }

        texture.SetPixels(colors);
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        renderer.sprite = sprite;
        renderer.sortingOrder = 5; // ���������� ��� ���� ������

        // �������
        bulletGO.transform.localScale = Vector3.one * 0.3f;

        // ���������
        var collider = bulletGO.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.1f;

        // Rigidbody ��� ������
        var rb = bulletGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        Debug.Log("Created fallback player bullet");
        return bulletGO;
    }

    // ������ ��� ��������� (���������)
    public void UpgradeDamage(float multiplier)
    {
        bulletDamage *= multiplier;
        Debug.Log($"Damage upgraded to: {bulletDamage}");
    }

    public void UpgradeAttackSpeed(float multiplier)
    {
        attackInterval *= (1f / multiplier); // ��������� �������� = ����������� ��������
        attackInterval = Mathf.Max(0.1f, attackInterval); // ����������� ��������
        Debug.Log($"Attack speed upgraded, interval: {attackInterval:F2}s");
    }

    public void UpgradeRange(float multiplier)
    {
        attackRange *= multiplier;
        Debug.Log($"Attack range upgraded to: {attackRange}");
    }

    public void SetCanAttack(bool canAttack)
    {
        this.canAttack = canAttack;
    }

    // ������� ��� UI � ����������
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
        Debug.Log("PlayerCombat cleaned up");
    }

    // �������������� ������ ��� ���������� ����
    public void ForceFindTarget()
    {
        FindNearestEnemy();
    }

    public void ForceAttack()
    {
        if (nearestEnemy != null)
        {
            Attack(nearestEnemy);
        }
    }

    public List<GameObject> GetAllTargetsInRange()
    {
        return new List<GameObject>(enemyTargets);
    }

    public bool IsEnemyInRange(GameObject enemy)
    {
        if (enemy == null) return false;
        float distance = Vector3.Distance(transform.position, enemy.transform.position);
        return distance <= attackRange;
    }

    // ������ ��� ��������� ���������� ���
    public float GetTimeSinceLastAttack()
    {
        return attackTimer;
    }

    public float GetTimeToNextAttack()
    {
        return Mathf.Max(0, attackInterval - attackTimer);
    }

    public bool CanAttackNow()
    {
        return canAttack && attackTimer >= attackInterval && nearestEnemy != null;
    }

    // ������������ ������� ����� � ���������
    void OnDrawGizmosSelected()
    {
        Gizmos.color = canAttack ? Color.red : Color.gray;

        // ������ ���������� ����� ��������
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

        // ���������� ������� ����
        if (nearestEnemy != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, nearestEnemy.transform.position);

            // ���������� ����������� �����
            if (firePoint != null)
            {
                Gizmos.color = Color.green;
                Vector3 direction = (nearestEnemy.transform.position - firePoint.position).normalized;
                Gizmos.DrawRay(firePoint.position, direction * 2f);
            }
        }

        // ���������� ��� ���� � �������
        Gizmos.color = Color.cyan;
        foreach (var target in enemyTargets)
        {
            if (target != null && target != nearestEnemy)
            {
                Gizmos.DrawWireSphere(target.transform.position, 0.3f);
            }
        }

        // ���������� ����� ��������
        if (firePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
    }

    // Debug ������
    [ContextMenu("Force Find Enemies")]
    private void DebugForceFindEnemies()
    {
        FindNearestEnemy();
        Debug.Log($"Found {enemyTargets.Count} enemies in range, nearest: {(nearestEnemy != null ? nearestEnemy.name : "none")}");
    }

    [ContextMenu("Force Attack")]
    private void DebugForceAttack()
    {
        ForceAttack();
    }

    [ContextMenu("Show Combat Info")]
    private void DebugShowCombatInfo()
    {
        Debug.Log($"=== Player Combat Info ===");
        Debug.Log($"Can Attack: {canAttack}");
        Debug.Log($"Attack Range: {attackRange}");
        Debug.Log($"Attack Interval: {attackInterval:F2}s");
        Debug.Log($"Time Since Last Attack: {attackTimer:F2}s");
        Debug.Log($"Time To Next Attack: {GetTimeToNextAttack():F2}s");
        Debug.Log($"Can Attack Now: {CanAttackNow()}");
        Debug.Log($"Bullet Damage: {bulletDamage}");
        Debug.Log($"Bullet Speed: {bulletSpeed}");
        Debug.Log($"Targets In Range: {enemyTargets.Count}");
        Debug.Log($"Nearest Enemy: {(nearestEnemy != null ? nearestEnemy.name : "none")}");
        Debug.Log($"=============================");
    }
}