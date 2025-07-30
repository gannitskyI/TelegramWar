using UnityEngine;
using System.Collections;
public class EnemyContactDamageSource : IDamageSource
{
    private EnemyBehaviour enemy;

    public EnemyContactDamageSource(EnemyBehaviour enemyBehaviour)
    {
        enemy = enemyBehaviour;
    }

    public float GetDamage() => enemy.GetExplosionDamage();
    public DamageTeam GetTeam() => DamageTeam.Enemy;
    public string GetSourceName() => $"Enemy Contact ({enemy.GetConfig()?.enemyName ?? "Unknown"})";
    public GameObject GetSourceObject() => enemy.gameObject;
    public Vector3 GetSourcePosition() => enemy.transform.position;
}