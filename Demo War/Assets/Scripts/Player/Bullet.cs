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
    [SerializeField] private BulletOwner owner = BulletOwner.Player;

    private Rigidbody2D rb;
    private float timer;
    private GameObject creator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.gravityScale = 0f;

        if (GetComponent<Collider2D>() == null)
        {
            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.1f;
        }

        SetupBulletLayer();
    }

    private void SetupBulletLayer()
    {
        int bulletLayer = LayerMask.NameToLayer("Bullet");
        if (bulletLayer != -1)
        {
            gameObject.layer = bulletLayer;
        }
    }

    private void Start()
    {
        if (rb != null)
        {
            rb.linearVelocity = transform.up * speed;
        }

        timer = lifetime;
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
                try { gameObject.tag = "PlayerBullet"; } catch { }
                break;
            case BulletOwner.Enemy:
                if (string.IsNullOrEmpty(gameObject.name) || gameObject.name == "GameObject")
                {
                    gameObject.name = "EnemyBullet";
                }
                try { gameObject.tag = "EnemyBullet"; } catch { }
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
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            DestroyBullet();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ShouldIgnoreCollision(other))
        {
            return;
        }

        HandleCollision(other);
    }

    private bool ShouldIgnoreCollision(Collider2D other)
    {
        if (other.gameObject == creator)
        {
            return true;
        }

        switch (owner)
        {
            case BulletOwner.Player:
                return IsPlayerOrPlayerBullet(other);

            case BulletOwner.Enemy:
                return IsEnemyOrEnemyBullet(other);

            case BulletOwner.Neutral:
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
                if (IsEnemy(other))
                {
                    var enemy = other.GetComponent<EnemyBehaviour>();
                    if (enemy != null && enemy.IsAlive())
                    {
                        enemy.TakeDamage(damage);
                        shouldDealDamage = true;
                    }
                }
                break;

            case BulletOwner.Enemy:
                var damageable = other.GetComponent<IDamageable>();
                if (damageable != null && damageable.GetTeam() == DamageTeam.Player && damageable.IsAlive())
                {
                    var bulletSource = new BulletDamageSource(this);
                    damageable.TakeDamage(damage, bulletSource);
                    shouldDealDamage = true;
                }
                break;

            case BulletOwner.Neutral:
                var enemyDamageable = other.GetComponent<IDamageable>();
                if (enemyDamageable != null && enemyDamageable.IsAlive())
                {
                    var bulletSource = new BulletDamageSource(this);
                    enemyDamageable.TakeDamage(damage, bulletSource);
                    shouldDealDamage = true;
                }
                break;
        }

        if (IsWallOrObstacle(other))
        {
            DestroyBullet();
            return;
        }

        if (shouldDealDamage && destroyOnImpact)
        {
            DestroyBullet();
        }
    }

    private bool IsPlayerStrict(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            return true;
        }

        var playerHealth = other.GetComponent<PlayerHealth>();
        var playerMovement = other.GetComponent<PlayerMovement>();
        var playerCombat = other.GetComponent<PlayerCombat>();

        return playerHealth != null || playerMovement != null || playerCombat != null;
    }

    private bool IsPlayerOrPlayerBullet(Collider2D other)
    {
        return IsPlayerStrict(other) || IsPlayerBullet(other);
    }

    private bool IsEnemy(Collider2D other)
    {
        return other.GetComponent<EnemyBehaviour>() != null || other.CompareTag("Enemy");
    }

    private bool IsEnemyOrEnemyBullet(Collider2D other)
    {
        return IsEnemy(other) || IsEnemyBullet(other);
    }

    private bool IsPlayerBullet(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        return (bullet != null && bullet.owner == BulletOwner.Player) || other.CompareTag("PlayerBullet");
    }

    private bool IsEnemyBullet(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        return (bullet != null && bullet.owner == BulletOwner.Enemy) || other.CompareTag("EnemyBullet");
    }

    private bool IsNeutralBullet(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        return bullet != null && bullet.owner == BulletOwner.Neutral;
    }

    private bool IsWallOrObstacle(Collider2D other)
    {
        return other.name.Contains("Wall") ||
               other.name.Contains("Obstacle") ||
               other.gameObject.layer == LayerMask.NameToLayer("Walls") ||
               other.gameObject.layer == LayerMask.NameToLayer("Obstacles");
    }

    public void Initialize(float bulletDamage, float bulletSpeed, Vector3 direction, BulletOwner bulletOwner = BulletOwner.Player, GameObject bulletCreator = null)
    {
        damage = bulletDamage;
        speed = bulletSpeed;
        owner = bulletOwner;
        creator = bulletCreator;

        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
        }

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        }

        SetBulletName();
    }

    public float GetDamage() => damage;
    public BulletOwner GetOwner() => owner;
    public void SetOwner(BulletOwner newOwner) { owner = newOwner; SetBulletName(); }
    public void SetDamage(float newDamage) => damage = Mathf.Max(0, newDamage);
    public void SetSpeed(float newSpeed) { speed = Mathf.Max(0, newSpeed); if (rb != null) rb.linearVelocity = rb.linearVelocity.normalized * speed; }
    public void SetLifetime(float newLifetime) { lifetime = Mathf.Max(0.1f, newLifetime); timer = lifetime; }

    private void DestroyBullet()
    {
        CreateDestructionEffect();
        Destroy(gameObject);
    }

    private void CreateDestructionEffect()
    {
        if (owner == BulletOwner.Player)
        {
            Debug.Log($"Player bullet destroyed at {transform.position}");
        }
        else if (owner == BulletOwner.Enemy)
        {
            Debug.Log($"Enemy bullet destroyed at {transform.position}");
        }
    }

    public void ChangeDirection(Vector3 newDirection)
    {
        if (rb != null)
        {
            rb.linearVelocity = newDirection.normalized * speed;

            if (newDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(Vector3.forward, newDirection);
            }
        }
    }

    public void Stop()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public bool IsMoving() => rb != null && rb.linearVelocity.magnitude > 0.1f;
    public float GetCurrentSpeed() => rb != null ? rb.linearVelocity.magnitude : 0f;
    public Vector3 GetDirection() => rb != null ? rb.linearVelocity.normalized : Vector3.zero;
    public float GetRemainingLifetime() => timer;
}

public class BulletDamageSource : IDamageSource
{
    private Bullet bullet;

    public BulletDamageSource(Bullet bulletInstance)
    {
        bullet = bulletInstance;
    }

    public float GetDamage() => bullet.GetDamage();
    public DamageTeam GetTeam() => ConvertBulletOwnerToTeam(bullet.GetOwner());
    public string GetSourceName() => $"{bullet.GetOwner()} Bullet";
    public GameObject GetSourceObject() => bullet.gameObject;
    public Vector3 GetSourcePosition() => bullet.transform.position;

    private DamageTeam ConvertBulletOwnerToTeam(BulletOwner owner)
    {
        switch (owner)
        {
            case BulletOwner.Player: return DamageTeam.Player;
            case BulletOwner.Enemy: return DamageTeam.Enemy;
            case BulletOwner.Neutral: return DamageTeam.Neutral;
            default: return DamageTeam.Neutral;
        }
    }
}