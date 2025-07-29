using UnityEngine;

public enum BulletOwner
{
    Player,
    Enemy,
    Neutral
}

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool destroyOnImpact = true;
    [SerializeField] private BulletOwner owner = BulletOwner.Player; // Заменяем тег

    private Rigidbody2D rb;
    private float timer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Отключаем гравитацию для пули
        rb.gravityScale = 0f;

        // Добавляем коллайдер если его нет
        if (GetComponent<Collider2D>() == null)
        {
            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.1f;
        }
    }

    private void Start()
    {
        // Задаем пуле скорость
        if (rb != null)
        {
            rb.linearVelocity = transform.up * speed;
        }

        timer = lifetime;

        // Устанавливаем имя в зависимости от владельца
        SetBulletName();
    }

    private void SetBulletName()
    {
        switch (owner)
        {
            case BulletOwner.Player:
                if (string.IsNullOrEmpty(gameObject.name) || gameObject.name == "GameObject")
                {
                    gameObject.name = "PlayerBullet";
                }
                break;
            case BulletOwner.Enemy:
                if (string.IsNullOrEmpty(gameObject.name) || gameObject.name == "GameObject")
                {
                    gameObject.name = "EnemyBullet";
                }
                break;
            case BulletOwner.Neutral:
                if (string.IsNullOrEmpty(gameObject.name) || gameObject.name == "GameObject")
                {
                    gameObject.name = "Bullet";
                }
                break;
        }
    }

    private void Update()
    {
        // Уничтожаем пулю после определенного времени
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            DestroyBullet();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Игнорируем столкновение с создателем пули
        if (ShouldIgnoreCollision(other))
        {
            return;
        }

        // Обрабатываем столкновения в зависимости от владельца пули
        HandleCollision(other);
    }

    private bool ShouldIgnoreCollision(Collider2D other)
    {
        switch (owner)
        {
            case BulletOwner.Player:
                // Пули игрока игнорируют игрока и другие пули игрока
                return IsPlayer(other) || IsPlayerBullet(other);

            case BulletOwner.Enemy:
                // Пули врагов игнорируют врагов и другие пули врагов
                return IsEnemy(other) || IsEnemyBullet(other);

            case BulletOwner.Neutral:
                // Нейтральные пули игнорируют только другие нейтральные пули
                return IsNeutralBullet(other);

            default:
                return false;
        }
    }

    private void HandleCollision(Collider2D other)
    {
        bool shouldDealDamage = false;

        switch (owner)
        {
            case BulletOwner.Player:
                // Пули игрока наносят урон врагам
                if (IsEnemy(other))
                {
                    var enemy = other.GetComponent<EnemyBehaviour>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(damage);
                        shouldDealDamage = true;
                    }
                }
                break;

            case BulletOwner.Enemy:
                // Пули врагов наносят урон игроку
                if (IsPlayer(other))
                {
                    var enemyPlayerHealth = other.GetComponent<PlayerHealth>();
                    if (enemyPlayerHealth != null)
                    {
                        enemyPlayerHealth.TakeDamage(damage);
                        shouldDealDamage = true;
                    }
                }
                break;

            case BulletOwner.Neutral:
                // Нейтральные пули наносят урон всем
                var enemyBehaviour = other.GetComponent<EnemyBehaviour>();
                var neutralPlayerHealth = other.GetComponent<PlayerHealth>();

                if (enemyBehaviour != null)
                {
                    enemyBehaviour.TakeDamage(damage);
                    shouldDealDamage = true;
                }
                else if (neutralPlayerHealth != null)
                {
                    neutralPlayerHealth.TakeDamage(damage);
                    shouldDealDamage = true;
                }
                break;
        }

        // Столкновение со стенами и препятствиями
        if (IsWallOrObstacle(other))
        {
            DestroyBullet();
            return;
        }

        // Уничтожаем пулю если нанесли урон и настроено уничтожение при попадании
        if (shouldDealDamage && destroyOnImpact)
        {
            DestroyBullet();
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        // Проверяем наличие компонентов игрока
        return other.GetComponent<PlayerHealth>() != null ||
               other.GetComponent<PlayerMovement>() != null ||
               other.GetComponent<PlayerCombat>() != null ||
               other.name.Contains("Player");
    }

    private bool IsEnemy(Collider2D other)
    {
        // Проверяем наличие компонента врага
        return other.GetComponent<EnemyBehaviour>() != null ||
               other.name.Contains("Enemy");
    }

    private bool IsPlayerBullet(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        return bullet != null && bullet.owner == BulletOwner.Player;
    }

    private bool IsEnemyBullet(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        return bullet != null && bullet.owner == BulletOwner.Enemy;
    }

    private bool IsNeutralBullet(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        return bullet != null && bullet.owner == BulletOwner.Neutral;
    }

    private bool IsWallOrObstacle(Collider2D other)
    {
        // Проверяем по имени или слою
        return other.name.Contains("Wall") ||
               other.name.Contains("Obstacle") ||
               other.gameObject.layer == LayerMask.NameToLayer("Walls") ||
               other.gameObject.layer == LayerMask.NameToLayer("Obstacles");
    }

    /// <summary>
    /// Инициализировать пулю с параметрами и направлением
    /// </summary>
    public void Initialize(float bulletDamage, float bulletSpeed, Vector3 direction, BulletOwner bulletOwner = BulletOwner.Player)
    {
        damage = bulletDamage;
        speed = bulletSpeed;
        owner = bulletOwner;

        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
        }

        // Поворачиваем пулю в направлении движения
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        }

        // Обновляем имя после установки владельца
        SetBulletName();
    }

    /// <summary>
    /// Получить урон пули
    /// </summary>
    public float GetDamage()
    {
        return damage;
    }

    /// <summary>
    /// Получить владельца пули
    /// </summary>
    public BulletOwner GetOwner()
    {
        return owner;
    }

    /// <summary>
    /// Установить владельца пули
    /// </summary>
    public void SetOwner(BulletOwner newOwner)
    {
        owner = newOwner;
        SetBulletName();
    }

    /// <summary>
    /// Установить урон пули
    /// </summary>
    public void SetDamage(float newDamage)
    {
        damage = Mathf.Max(0, newDamage);
    }

    /// <summary>
    /// Установить скорость пули
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0, newSpeed);
        if (rb != null)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }
    }

    /// <summary>
    /// Установить время жизни пули
    /// </summary>
    public void SetLifetime(float newLifetime)
    {
        lifetime = Mathf.Max(0.1f, newLifetime);
        timer = lifetime;
    }

    /// <summary>
    /// Уничтожить пулю с эффектами
    /// </summary>
    private void DestroyBullet()
    {
        // Здесь можно добавить эффекты уничтожения
        // например, частицы, звуки, анимации и т.д.
        CreateDestructionEffect();

        Destroy(gameObject);
    }

    private void CreateDestructionEffect()
    {
        // Простой эффект исчезновения - можно расширить
        if (owner == BulletOwner.Player)
        {
            // Эффект для пуль игрока
            Debug.Log($"Player bullet destroyed at {transform.position}");
        }
        else if (owner == BulletOwner.Enemy)
        {
            // Эффект для пуль врагов
            Debug.Log($"Enemy bullet destroyed at {transform.position}");
        }
    }

    /// <summary>
    /// Изменить направление пули
    /// </summary>
    public void ChangeDirection(Vector3 newDirection)
    {
        if (rb != null)
        {
            rb.linearVelocity = newDirection.normalized * speed;

            // Поворачиваем пулю
            if (newDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(Vector3.forward, newDirection);
            }
        }
    }

    /// <summary>
    /// Остановить пулю
    /// </summary>
    public void Stop()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Проверить, движется ли пуля
    /// </summary>
    public bool IsMoving()
    {
        return rb != null && rb.linearVelocity.magnitude > 0.1f;
    }

    /// <summary>
    /// Получить текущую скорость пули
    /// </summary>
    public float GetCurrentSpeed()
    {
        return rb != null ? rb.linearVelocity.magnitude : 0f;
    }

    /// <summary>
    /// Получить направление движения пули
    /// </summary>
    public Vector3 GetDirection()
    {
        return rb != null ? rb.linearVelocity.normalized : Vector3.zero;
    }

    /// <summary>
    /// Получить оставшееся время жизни
    /// </summary>
    public float GetRemainingLifetime()
    {
        return timer;
    }

    // Debug методы
    [ContextMenu("Destroy Bullet")]
    private void DebugDestroyBullet()
    {
        DestroyBullet();
    }

    [ContextMenu("Show Bullet Info")]
    private void DebugShowInfo()
    {
        Debug.Log($"=== Bullet Info ===");
        Debug.Log($"Owner: {owner}");
        Debug.Log($"Damage: {damage}");
        Debug.Log($"Speed: {speed}");
        Debug.Log($"Current Speed: {GetCurrentSpeed():F2}");
        Debug.Log($"Direction: {GetDirection()}");
        Debug.Log($"Remaining Lifetime: {timer:F1}s");
        Debug.Log($"Is Moving: {IsMoving()}");
        Debug.Log($"Destroy On Impact: {destroyOnImpact}");
        Debug.Log($"==================");
    }

    [ContextMenu("Change to Enemy Bullet")]
    private void DebugChangeToEnemyBullet()
    {
        SetOwner(BulletOwner.Enemy);
        Debug.Log("Changed to enemy bullet");
    }

    [ContextMenu("Change to Player Bullet")]
    private void DebugChangeToPlayerBullet()
    {
        SetOwner(BulletOwner.Player);
        Debug.Log("Changed to player bullet");
    }
}