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
    [SerializeField] private Transform firePoint; // Точка выстрела

    public int InitializationOrder => 15;

    private float attackTimer;
    private GameObject nearestEnemy;
    private bool canAttack = true;
    private SystemsConfiguration config;

    // Кэш для оптимизации
    private List<GameObject> enemyTargets = new List<GameObject>();
    private float targetScanInterval = 0.1f; // Сканируем цели каждые 0.1 секунды
    private float targetScanTimer;

    public IEnumerator Initialize()
    {
        Debug.Log("PlayerCombat initialization started");

        // Получаем конфигурацию
        config = ServiceLocator.Get<SystemsConfiguration>();
        if (config != null)
        {
            bulletDamage = config.playerDamage;
        }

        // Если нет firePoint, создаем его
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

        // Обновляем таймеры
        attackTimer += Time.deltaTime;
        targetScanTimer += Time.deltaTime;

        // Сканируем цели реже для оптимизации
        if (targetScanTimer >= targetScanInterval)
        {
            FindNearestEnemy();
            targetScanTimer = 0f;
        }

        // Атакуем если есть цель и прошло достаточно времени
        if (attackTimer >= attackInterval && nearestEnemy != null)
        {
            Attack(nearestEnemy);
            attackTimer = 0f;
        }
    }

    private void FindNearestEnemy()
    {
        // Очищаем список и находим всех врагов через компоненты
        enemyTargets.Clear();
        var allEnemyBehaviours = Object.FindObjectsOfType<EnemyBehaviour>();

        GameObject closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (var enemyBehaviour in allEnemyBehaviours)
        {
            if (enemyBehaviour == null || !enemyBehaviour.IsAlive()) continue;

            var enemy = enemyBehaviour.gameObject;
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            // Проверяем находится ли враг в радиусе атаки
            if (distance <= attackRange)
            {
                enemyTargets.Add(enemy);

                // Находим ближайшего
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }

        // Обновляем цель только если нашли ближайшего или потеряли текущую
        if (closestEnemy != null)
        {
            nearestEnemy = closestEnemy;
        }
        else if (nearestEnemy != null)
        {
            // Проверяем, что текущая цель все еще в радиусе
            if (Vector3.Distance(transform.position, nearestEnemy.transform.position) > attackRange)
            {
                nearestEnemy = null;
            }
        }
    }

    private void Attack(GameObject target)
    {
        if (target == null) return;

        // Вычисляем направление к цели
        Vector3 direction = (target.transform.position - firePoint.position).normalized;

        // Создаем пулю
        CreateBullet(firePoint.position, direction);

        // Поворачиваем игрока в сторону атаки (опционально)
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
        // Пытаемся загрузить префаб пули через Addressables
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

        // Если не удалось загрузить префаб, создаем простую пулю
        if (bulletObject == null)
        {
            bulletObject = CreateFallbackBullet(startPosition);
        }

        // Настраиваем пулю
        var bullet = bulletObject.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = bulletObject.AddComponent<Bullet>();
        }

        // Инициализируем пулю с параметрами (указываем что это пуля игрока)
        bullet.Initialize(bulletDamage, bulletSpeed, direction, BulletOwner.Player);
    }

    private GameObject CreateFallbackBullet(Vector3 position)
    {
        // Создаем простую пулю если префаб не загрузился
        var bulletGO = new GameObject("PlayerBullet");
        bulletGO.transform.position = position;

        // Добавляем визуал
        var renderer = bulletGO.AddComponent<SpriteRenderer>();

        // Создаем простой спрайт пули
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
        renderer.sortingOrder = 5; // Убеждаемся что пуля видима

        // Масштаб
        bulletGO.transform.localScale = Vector3.one * 0.3f;

        // Коллайдер
        var collider = bulletGO.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.1f;

        // Rigidbody для физики
        var rb = bulletGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        Debug.Log("Created fallback player bullet");
        return bulletGO;
    }

    // Методы для улучшений (апгрейдов)
    public void UpgradeDamage(float multiplier)
    {
        bulletDamage *= multiplier;
        Debug.Log($"Damage upgraded to: {bulletDamage}");
    }

    public void UpgradeAttackSpeed(float multiplier)
    {
        attackInterval *= (1f / multiplier); // Уменьшаем интервал = увеличиваем скорость
        attackInterval = Mathf.Max(0.1f, attackInterval); // Минимальный интервал
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

    // Геттеры для UI и статистики
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

    // Дополнительные методы для управления боем
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

    // Методы для получения статистики боя
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

    // Визуализация радиуса атаки в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = canAttack ? Color.red : Color.gray;

        // Рисуем окружность через сегменты
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

        // Показываем текущую цель
        if (nearestEnemy != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, nearestEnemy.transform.position);

            // Показываем направление атаки
            if (firePoint != null)
            {
                Gizmos.color = Color.green;
                Vector3 direction = (nearestEnemy.transform.position - firePoint.position).normalized;
                Gizmos.DrawRay(firePoint.position, direction * 2f);
            }
        }

        // Показываем все цели в радиусе
        Gizmos.color = Color.cyan;
        foreach (var target in enemyTargets)
        {
            if (target != null && target != nearestEnemy)
            {
                Gizmos.DrawWireSphere(target.transform.position, 0.3f);
            }
        }

        // Показываем точку выстрела
        if (firePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
    }

    // Debug методы
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