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
    }

    private void OnEnable()
    {
        hasDealtDamage = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDealtDamage) return;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;
        if (damageable.GetTeam() == team) return;
        if (!damageable.IsAlive()) return;

        damageable.TakeDamage(damage, this);
        hasDealtDamage = true;
        DisableProjectile();
    }

    private void DisableProjectile()
    {
        gameObject.SetActive(false);
    }

    public float GetDamage() => damage;
    public DamageTeam GetTeam() => team;
    public string GetSourceName() => sourceName;
    public GameObject GetSourceObject() => sourceObject;
    public Vector3 GetSourcePosition() => transform.position;
}
