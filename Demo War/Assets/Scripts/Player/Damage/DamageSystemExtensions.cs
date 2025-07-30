using UnityEngine;

public static class DamageSystemExtensions
{
    public static void SetupPlayerDamageReceiver(this GameObject playerObject)
    {
        if (playerObject.GetComponent<PlayerDamageReceiver>() == null)
        {
            playerObject.AddComponent<PlayerDamageReceiver>();
            Debug.Log($"[DAMAGE SYSTEM] Added PlayerDamageReceiver to {playerObject.name}");
        }
    }

    public static void SetupEnemyDamageReceiver(this GameObject enemyObject)
    {
        if (enemyObject.GetComponent<EnemyDamageReceiver>() == null)
        {
            enemyObject.AddComponent<EnemyDamageReceiver>();
            Debug.Log($"[DAMAGE SYSTEM] Added EnemyDamageReceiver to {enemyObject.name}");
        }
    }

    public static void SetupProjectileDamageSource(this GameObject projectile, float damage, DamageTeam team, string sourceName, GameObject creator = null)
    {
        var damageSource = projectile.GetComponent<ProjectileDamageSource>();
        if (damageSource == null)
        {
            damageSource = projectile.AddComponent<ProjectileDamageSource>();
        }

        damageSource.Initialize(damage, team, sourceName, creator);
    }

    public static void SetupContactDamageSource(this GameObject contactObject, float damage, DamageTeam team, string sourceName)
    {
        var damageSource = contactObject.GetComponent<ContactDamageSource>();
        if (damageSource == null)
        {
            damageSource = contactObject.AddComponent<ContactDamageSource>();
        }

        damageSource.Initialize(damage, team, sourceName);
    }

    public static void SetupExplosionDamageSource(this GameObject explosionObject, float damage, DamageTeam team, string sourceName, float radius = 2f)
    {
        var damageSource = explosionObject.GetComponent<ExplosionDamageSource>();
        if (damageSource == null)
        {
            damageSource = explosionObject.AddComponent<ExplosionDamageSource>();
        }

        damageSource.Initialize(damage, team, sourceName, radius);
    }
}