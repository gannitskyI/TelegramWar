using UnityEngine;

public class ContactDamageSource : MonoBehaviour, IDamageSource
{
    [SerializeField] private float damage;
    [SerializeField] private DamageTeam team;
    [SerializeField] private string sourceName;
    [SerializeField] private float damageInterval = 1f;

    private float lastDamageTime;

    public void Initialize(float contactDamage, DamageTeam contactTeam, string contactSourceName)
    {
        damage = contactDamage;
        team = contactTeam;
        sourceName = contactSourceName;
        lastDamageTime = 0f;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time - lastDamageTime < damageInterval) return;

        var damageable = other.GetComponent<IDamageable>();
        if (damageable == null) return;

        if (damageable.GetTeam() == team)
        {
            Debug.Log($"[DAMAGE SYSTEM] Contact damage ignored friendly target: {other.name}");
            return;
        }

        if (!damageable.IsAlive()) return;

        Debug.Log($"[DAMAGE SYSTEM] Contact damage to: {other.name}");
        damageable.TakeDamage(damage, this);
        lastDamageTime = Time.time;
    }

    public float GetDamage() => damage;
    public DamageTeam GetTeam() => team;
    public string GetSourceName() => sourceName;
    public GameObject GetSourceObject() => gameObject;
    public Vector3 GetSourcePosition() => transform.position;
}