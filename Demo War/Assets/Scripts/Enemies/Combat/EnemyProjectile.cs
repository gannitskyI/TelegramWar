using UnityEngine;
using System.Collections;

public class EnemyProjectile : MonoBehaviour
{
    private float damage;
    private float speed;
    private float lifetime;
    private bool isHoming;
    private bool isExplosive;
    private float timer;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;

    private float homingStrength = 3f;
    private float explosionRadius = 2f;
    private bool isDead = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(float projectileDamage, float projectileSpeed, float projectileLifetime, bool homingEnabled, bool explosiveEnabled)
    {
        damage = projectileDamage;
        speed = projectileSpeed;
        lifetime = projectileLifetime;
        isHoming = homingEnabled;
        isExplosive = explosiveEnabled;
        timer = 0f;
        isDead = false;
        if (isHoming)
        {
            CachePlayerReference();
        }
        if (rb != null)
        {
            rb.gravityScale = 0f;
        }
        StartCoroutine(LifetimeCoroutine());
    }

    private void CachePlayerReference()
    {
        if (ServiceLocator.TryGet<GameObject>(out var playerObject))
        {
            playerTransform = playerObject.transform;
        }
    }

    private void Update()
    {
        if (isDead) return;
        timer += Time.deltaTime;
        if (isHoming && playerTransform != null && timer > 0.2f)
        {
            UpdateHomingMovement();
        }
        UpdateVisualEffects();
    }

    private void UpdateHomingMovement()
    {
        if (rb == null) return;
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Vector2 currentVelocity = rb.linearVelocity.normalized;
        Vector2 steerForce = Vector2.Lerp(currentVelocity, directionToPlayer, homingStrength * Time.deltaTime);
        rb.linearVelocity = steerForce * speed;
        if (steerForce != Vector2.zero)
        {
            float angle = Mathf.Atan2(steerForce.y, steerForce.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }
    }

    private void UpdateVisualEffects()
    {
        if (spriteRenderer == null) return;
        if (isHoming)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.3f;
            transform.localScale = Vector3.one * pulse * 0.8f;
        }
        else if (isExplosive)
        {
            float flicker = 1f + Mathf.Sin(Time.time * 8f) * 0.2f;
            var color = spriteRenderer.color;
            color.r = flicker;
            spriteRenderer.color = color;
        }
    }

    private IEnumerator LifetimeCoroutine()
    {
        yield return new WaitForSeconds(lifetime);
        DisableProjectile();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || ShouldIgnoreCollision(other)) return;
        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null && damageable.GetTeam() != DamageTeam.Enemy && damageable.IsAlive())
        {
            var damageSource = GetComponent<IDamageSource>();
            if (damageSource != null)
                damageable.TakeDamage(damage, damageSource);
            else
                damageable.TakeDamage(damage, new EnemyProjectileDamageSource(this));
            if (isExplosive)
            {
                CreateExplosion();
            }
            DisableProjectile();
            return;
        }
        if (IsWallOrObstacle(other))
        {
            if (isExplosive)
            {
                CreateExplosion();
            }
            DisableProjectile();
        }
    }

    private bool ShouldIgnoreCollision(Collider2D other)
    {
        if (other.CompareTag("Enemy")) return true;
        if (other.GetComponent<EnemyBehaviour>() != null) return true;
        if (other.GetComponent<EnemyProjectile>() != null) return true;
        return false;
    }

    private bool IsWallOrObstacle(Collider2D other)
    {
        int wallLayer = LayerMask.NameToLayer("Walls");
        int obsLayer = LayerMask.NameToLayer("Obstacles");
        return other.name.Contains("Wall") || other.name.Contains("Obstacle") ||
               other.gameObject.layer == wallLayer || other.gameObject.layer == obsLayer;
    }

    private void CreateExplosion()
    {
        var hitObjects = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hitObject in hitObjects)
        {
            var damageable = hitObject.GetComponent<IDamageable>();
            if (damageable != null && damageable.GetTeam() != DamageTeam.Enemy && damageable.IsAlive())
            {
                float distance = Vector2.Distance(transform.position, hitObject.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                float finalDamage = damage * damageMultiplier;
                var explosionSource = new EnemyExplosionDamageSource(this, finalDamage);
                damageable.TakeDamage(finalDamage, explosionSource);
            }
        }
        
    }

    private void DisableProjectile()
    {
        isDead = true;
        if (spriteRenderer != null && spriteRenderer.sprite != null && Application.isPlaying)
        {
            Destroy(spriteRenderer.sprite.texture);
            Destroy(spriteRenderer.sprite);
        }
        if (Application.isPlaying)
        {
            gameObject.SetActive(false);
        }
    }

    public float GetDamage() => damage;
    public bool IsHoming() => isHoming;
    public bool IsExplosive() => isExplosive;
    public float GetRemainingLifetime() => lifetime - timer;
}
