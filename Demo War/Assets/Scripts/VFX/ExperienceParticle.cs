using UnityEngine;
using System.Collections.Generic;

public class ExperienceParticle : MonoBehaviour
{
    [Header("Experience Settings")]
    [SerializeField] private int experienceValue = 10;
    [SerializeField] private float attractionRange = 3f;
    [SerializeField] private float attractionSpeed = 8f;
    [SerializeField] private float baseSpeed = 2f;
    [SerializeField] private float lifetime = 30f;

    private Transform player;
    private Rigidbody2D rb;
    private bool isBeingAttracted = false;
    private float timer;
    private bool isCollected = false;

    private SpriteRenderer spriteRenderer;
    private float pulseSpeed = 3f;
    private Color originalColor;

    private static readonly List<ExperienceParticle> allParticles = new List<ExperienceParticle>(100);
    private static readonly Queue<ExperienceParticle> particlePool = new Queue<ExperienceParticle>(50);
    private static Transform playerTransform;
    private static ScoreSystem cachedScoreSystem;
    private static readonly Vector3[] directions = new Vector3[8];

    static ExperienceParticle()
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            directions[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearDamping = 1f;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            CreateExperienceSprite();
        }
        originalColor = spriteRenderer.color;
    }

    void Start()
    {
        allParticles.Add(this);
        CachePlayerReference();

        Vector3 randomDirection = directions[Random.Range(0, directions.Length)];
        rb.linearVelocity = randomDirection * baseSpeed;

        timer = 0f;
        isCollected = false;
        isBeingAttracted = false;
    }

    private static void CachePlayerReference()
    {
        if (playerTransform == null)
        {
            if (ServiceLocator.TryGet<GameObject>(out var playerObject))
            {
                playerTransform = playerObject.transform;
            }
        }

        if (cachedScoreSystem == null)
        {
            ServiceLocator.TryGet<ScoreSystem>(out cachedScoreSystem);
        }
    }

    void Update()
    {
        if (isCollected) return;

        timer += Time.deltaTime;

        if (timer >= lifetime)
        {
            ReturnToPool();
            return;
        }

        if (playerTransform != null)
        {
            float sqrDistance = (transform.position - playerTransform.position).sqrMagnitude;
            float attractionRangeSqr = attractionRange * attractionRange;

            if (sqrDistance <= 0.64f)
            {
                CollectExperience();
                return;
            }

            if (sqrDistance <= attractionRangeSqr)
            {
                isBeingAttracted = true;
                AttractToPlayer(sqrDistance);
            }
        }

        UpdateVisualEffects();
    }

    private void AttractToPlayer(float sqrDistance)
    {
        if (playerTransform == null || isCollected) return;

        Vector3 direction = (playerTransform.position - transform.position);
        float distance = Mathf.Sqrt(sqrDistance);
        direction /= distance;

        if (distance < 0.5f)
        {
            CollectExperience();
            return;
        }

        float speedMultiplier = Mathf.Lerp(3f, 1f, distance / attractionRange);
        rb.linearVelocity = direction * attractionSpeed * speedMultiplier;
    }

    private void UpdateVisualEffects()
    {
        if (spriteRenderer == null || isCollected) return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.2f;
        transform.localScale = Vector3.one * pulse;

        if (isBeingAttracted)
        {
            spriteRenderer.color = Color.Lerp(originalColor, Color.yellow,
                Mathf.Sin(Time.time * pulseSpeed * 2f) * 0.5f + 0.5f);
        }
        else
        {
            spriteRenderer.color = originalColor;
        }

        if (timer > lifetime * 0.8f)
        {
            float alpha = 1f - ((timer - lifetime * 0.8f) / (lifetime * 0.2f));
            var color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }

    private void CollectExperience()
    {
        if (isCollected) return;
        isCollected = true;

        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        spriteRenderer.enabled = false;

        if (cachedScoreSystem != null)
        {
            cachedScoreSystem.AddExperience(experienceValue);
        }

        WebGLHelper.TriggerHapticFeedback("light");
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (allParticles.Contains(this))
        {
            allParticles.Remove(this);
        }

        gameObject.SetActive(false);
        particlePool.Enqueue(this);
    }

    public static GameObject CreateExperienceParticle(Vector3 position, int experienceValue = 10)
    {
        ExperienceParticle particle;

        if (particlePool.Count > 0)
        {
            particle = particlePool.Dequeue();
            particle.transform.position = position;
            particle.gameObject.SetActive(true);
            particle.ResetParticle();
        }
        else
        {
            var particleGO = new GameObject("ExperienceParticle");
            particleGO.transform.position = position;
            particle = particleGO.AddComponent<ExperienceParticle>();
        }

        particle.SetExperienceValue(experienceValue);
        return particle.gameObject;
    }

    private void ResetParticle()
    {
        timer = 0f;
        isCollected = false;
        isBeingAttracted = false;
        rb.isKinematic = false;
        spriteRenderer.enabled = true;
        spriteRenderer.color = originalColor;
        transform.localScale = Vector3.one;
    }

    public void SetExperienceValue(int value) => experienceValue = value;
    public int GetExperienceValue() => experienceValue;
    public bool IsCollected() => isCollected;

    private void CreateExperienceSprite()
    {
        var texture = new Texture2D(32, 32);
        var colors = new Color[32 * 32];
        Vector2 center = new Vector2(16f, 16f);

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance < 12f)
                {
                    float alpha = 1f - (distance / 12f);
                    colors[y * 32 + x] = new Color(0.2f, 1f, 0.3f, alpha);
                }
                else
                {
                    colors[y * 32 + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
        spriteRenderer.sprite = sprite;
        originalColor = new Color(0.2f, 1f, 0.3f, 1f);
        spriteRenderer.color = originalColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        var playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            CollectExperience();
        }
    }

    public static void ClearAllPools()
    {
        allParticles.Clear();
        particlePool.Clear();
        playerTransform = null;
        cachedScoreSystem = null;
    }
}