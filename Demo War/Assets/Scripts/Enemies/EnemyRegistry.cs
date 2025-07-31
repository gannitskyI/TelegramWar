using System.Collections.Generic;
using UnityEngine;

public class EnemyRegistry : MonoBehaviour
{
    private static EnemyRegistry instance;
    public static EnemyRegistry Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[EnemyRegistry]");
                instance = go.AddComponent<EnemyRegistry>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private readonly HashSet<EnemyDamageReceiver> activeEnemies = new HashSet<EnemyDamageReceiver>();
    private readonly List<EnemyDamageReceiver> enemiesInRange = new List<EnemyDamageReceiver>();

    public void RegisterEnemy(EnemyDamageReceiver enemy)
    {
        activeEnemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyDamageReceiver enemy)
    {
        activeEnemies.Remove(enemy);
    }

    public List<EnemyDamageReceiver> GetEnemiesInRange(Vector3 position, float range)
    {
        enemiesInRange.Clear();
        float rangeSqr = range * range;

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy.IsAlive())
            {
                float distanceSqr = (enemy.transform.position - position).sqrMagnitude;
                if (distanceSqr <= rangeSqr)
                {
                    enemiesInRange.Add(enemy);
                }
            }
        }

        return enemiesInRange;
    }

    public EnemyDamageReceiver FindClosestEnemy(Vector3 position, float maxRange)
    {
        EnemyDamageReceiver closest = null;
        float closestDistanceSqr = maxRange * maxRange;

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy.IsAlive())
            {
                float distanceSqr = (enemy.transform.position - position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closest = enemy;
                    closestDistanceSqr = distanceSqr;
                }
            }
        }

        return closest;
    }

    public void Clear()
    {
        activeEnemies.Clear();
        enemiesInRange.Clear();
    }
}