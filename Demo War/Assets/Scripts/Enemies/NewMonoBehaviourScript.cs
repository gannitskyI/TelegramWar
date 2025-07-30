using UnityEngine;

public class EnemyProjectileDamageSource : IDamageSource
{
    private EnemyProjectile projectile;

    public EnemyProjectileDamageSource(EnemyProjectile enemyProjectile)
    {
        projectile = enemyProjectile;
    }

    public float GetDamage() => projectile.GetDamage();
    public DamageTeam GetTeam() => DamageTeam.Enemy;
    public string GetSourceName() => $"Enemy Projectile ({projectile.name})";
    public GameObject GetSourceObject() => projectile.gameObject;
    public Vector3 GetSourcePosition() => projectile.transform.position;
}