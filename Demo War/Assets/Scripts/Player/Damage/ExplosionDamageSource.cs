using UnityEngine;

public class ExplosionDamageSource : MonoBehaviour, IDamageSource
{
    [SerializeField] private float damage;
    [SerializeField] private DamageTeam team;
    [SerializeField] private string sourceName;
    [SerializeField] private float explosionRadius = 2f;

    public void Initialize(float explosionDamage, DamageTeam explosionTeam, string explosionSourceName, float radius = 2f)
    {
        damage = explosionDamage;
        team = explosionTeam;
        sourceName = explosionSourceName;
        explosionRadius = radius;
    }

    public void Explode()
    {
        Debug.Log($"[DAMAGE SYSTEM] Explosion triggered: {sourceName}, Radius: {explosionRadius}");

        var hitTargets = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (var target in hitTargets)
        {
            var damageable = target.GetComponent<IDamageable>();
            if (damageable == null) continue;

            if (damageable.GetTeam() == team)
            {
                Debug.Log($"[DAMAGE SYSTEM] Explosion ignored friendly target: {target.name}");
                continue;
            }

            if (!damageable.IsAlive()) continue;

            float distance = Vector2.Distance(transform.position, target.transform.position);
            float damageMultiplier = 1f - (distance / explosionRadius);
            float finalDamage = damage * damageMultiplier;

            Debug.Log($"[DAMAGE SYSTEM] Explosion damage to {target.name}: {finalDamage:F1} (Distance: {distance:F2})");
            damageable.TakeDamage(finalDamage, this);
        }
    }

    public float GetDamage() => damage;
    public DamageTeam GetTeam() => team;
    public string GetSourceName() => sourceName;
    public GameObject GetSourceObject() => gameObject;
    public Vector3 GetSourcePosition() => transform.position;
}

