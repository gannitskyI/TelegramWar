using UnityEngine;

public class EnemyExplosionDamageSource : IDamageSource
{
    private EnemyProjectile projectile;
    private float explosionDamage;

    public EnemyExplosionDamageSource(EnemyProjectile enemyProjectile, float damage)
    {
        projectile = enemyProjectile;
        explosionDamage = damage;
    }

    public float GetDamage() => explosionDamage;
    public DamageTeam GetTeam() => DamageTeam.Enemy;
    public string GetSourceName() => $"Enemy Explosion ({projectile.name})";
    public GameObject GetSourceObject() => projectile.gameObject;
    public Vector3 GetSourcePosition() => projectile.transform.position;
}

 