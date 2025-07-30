using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, IDamageSource source);
    bool IsAlive();
    DamageTeam GetTeam();
}