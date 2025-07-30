using UnityEngine;

public class EnemyDamageReceiver : MonoBehaviour, IDamageable
{
    private EnemyBehaviour enemyBehaviour;

    private void Awake()
    {
        enemyBehaviour = GetComponent<EnemyBehaviour>();
        if (enemyBehaviour == null)
        {
            Debug.LogError("EnemyDamageReceiver: No EnemyBehaviour component found!");
        }
    }

    public void TakeDamage(float damage, IDamageSource source)
    {
        if (enemyBehaviour == null || !IsAlive()) return;

        if (source.GetTeam() == DamageTeam.Enemy)
        {
            Debug.LogWarning($"[DAMAGE SYSTEM] Enemy damage blocked from friendly source: {source.GetSourceName()}");
            return;
        }

        Debug.Log($"[DAMAGE SYSTEM] Enemy taking {damage} damage from {source.GetSourceName()} (Team: {source.GetTeam()})");
        enemyBehaviour.TakeDamage(damage);
    }

    public bool IsAlive()
    {
        return enemyBehaviour != null && enemyBehaviour.IsAlive();
    }

    public DamageTeam GetTeam()
    {
        return DamageTeam.Enemy;
    }
}

