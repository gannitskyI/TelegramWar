using UnityEngine;

public class ProjectileDamageSource : MonoBehaviour, IDamageSource
{
    [SerializeField] private float damage;
    [SerializeField] private DamageTeam team;
    [SerializeField] private string sourceName;
    [SerializeField] private GameObject sourceObject;

    private bool hasDealtDamage = false;

    public void Initialize(float projectileDamage, DamageTeam projectileTeam, string projectileSourceName, GameObject creator = null)
    {
        damage = projectileDamage;
        team = projectileTeam;
        sourceName = projectileSourceName;
        sourceObject = creator != null ? creator : gameObject;
        hasDealtDamage = false;

        Debug.Log($"[DAMAGE SYSTEM] Projectile initialized: {sourceName}, Team: {team}, Damage: {damage}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDealtDamage) return;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;

        if (damageable.GetTeam() == team)
        {
            Debug.Log($"[DAMAGE SYSTEM] Projectile ignored friendly target: {other.name} (Same team: {team})");
            return;
        }

        if (!damageable.IsAlive())
        {
            Debug.Log($"[DAMAGE SYSTEM] Projectile ignored dead target: {other.name}");
            return;
        }

        Debug.Log($"[DAMAGE SYSTEM] Projectile hit valid target: {other.name}");
        damageable.TakeDamage(damage, this);
        hasDealtDamage = true;

        DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        Debug.Log($"[DAMAGE SYSTEM] Destroying projectile: {sourceName}");
        Destroy(gameObject);
    }

    public float GetDamage() => damage;
    public DamageTeam GetTeam() => team;
    public string GetSourceName() => sourceName;
    public GameObject GetSourceObject() => sourceObject;
    public Vector3 GetSourcePosition() => transform.position;
}

 

 