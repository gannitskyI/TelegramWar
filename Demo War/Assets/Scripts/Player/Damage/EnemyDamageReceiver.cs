using UnityEngine;

public class EnemyDamageReceiver : MonoBehaviour, IDamageable
{
    private EnemyBehaviour enemyBehaviour;
    private IDamageable enemyDamageable;

    private void Awake()
    {
        enemyBehaviour = GetComponent<EnemyBehaviour>();
        enemyDamageable = GetComponent<IDamageable>();
    }

    public void TakeDamage(float damage, IDamageSource source)
    {
        if (enemyBehaviour == null || !IsAlive()) return;
        if (source.GetTeam() == DamageTeam.Enemy) return;
        if (enemyDamageable != null)
        {
            enemyDamageable.TakeDamage(damage, source);
        }
        else
        {
            enemyBehaviour.TakeDamage(damage, source);
        }
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
