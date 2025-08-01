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
        DestroyProjectile();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ShouldIgnoreCollision(other)) return;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            if (damageable.GetTeam() == DamageTeam.Enemy)
            {
                return;
            }

            if (!damageable.IsAlive()) return;

            var damageSource = GetComponent<IDamageSource>();
            if (damageSource != null)
            {
                damageable.TakeDamage(damage, damageSource);
            }
            else
            {
                var tempSource = new EnemyProjectileDamageSource(this);
                damageable.TakeDamage(damage, tempSource);
            }

            if (isExplosive)
            {
                CreateExplosion();
            }

            DestroyProjectile();
            return;
        }

        if (IsWallOrObstacle(other))
        {
            if (isExplosive)
            {
                CreateExplosion();
            }
            DestroyProjectile();
        }
    }

    private bool ShouldIgnoreCollision(Collider2D other)
    {
        if (HasEnemyTag(other) || other.GetComponent<EnemyBehaviour>() != null || other.GetComponent<EnemyProjectile>() != null)
        {
            return true;
        }
        return false;
    }

    private bool HasEnemyTag(Collider2D other)
    {
        try
        {
            return other.CompareTag("Enemy");
        }
        catch
        {
            return false;
        }
    }

    private bool IsWallOrObstacle(Collider2D other)
    {
        return other.name.Contains("Wall") ||
               other.name.Contains("Obstacle") ||
               other.gameObject.layer == LayerMask.NameToLayer("Walls") ||
               other.gameObject.layer == LayerMask.NameToLayer("Obstacles");
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

        CreateExplosionEffect();
    }

    private void CreateExplosionEffect()
    {
        StartCoroutine(ExplosionVisualEffect());
    }

    private IEnumerator ExplosionVisualEffect()
    {
        var originalSprite = spriteRenderer.sprite;

        CreateExplosionSprite();

        Vector3 originalScale = transform.localScale;
        Color originalColor = spriteRenderer.color;

        for (int i = 0; i < 8; i++)
        {
            float progress = i / 8f;
            transform.localScale = originalScale * (1f + progress * 3f);

            Color newColor = Color.Lerp(Color.red, Color.yellow, progress);
            newColor.a = 1f - progress;
            spriteRenderer.color = newColor;

            yield return new WaitForSeconds(0.05f);
        }
    }

    private void CreateExplosionSprite()
    {
        int size = 32;
        var texture = new Texture2D(size, size);
        var colors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance < size * 0.4f)
                {
                    float alpha = 1f - (distance / (size * 0.4f));
                    colors[y * size + x] = new Color(1f, 0.5f, 0f, alpha);
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
        spriteRenderer.sprite = sprite;
    }

    private void DestroyProjectile()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            if (Application.isPlaying)
            {
                Destroy(spriteRenderer.sprite.texture);
                Destroy(spriteRenderer.sprite);
            }
        }

        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
    }

    public float GetDamage() => damage;
    public bool IsHoming() => isHoming;
    public bool IsExplosive() => isExplosive;
    public float GetRemainingLifetime() => lifetime - timer;

    private void OnDrawGizmosSelected()
    {
        if (isExplosive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        if (isHoming && playerTransform != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}