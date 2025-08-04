using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EnemyPool
{
    private Dictionary<string, Queue<GameObject>> pools;
    private EnemyFactory factory;
    private Transform poolParent;
    private const int INITIAL_POOL_SIZE = 10;
    private const int MAX_POOL_SIZE = 50;

    public EnemyPool()
    {
        pools = new Dictionary<string, Queue<GameObject>>();
        var poolObject = new GameObject("EnemyPool");
        poolObject.SetActive(false);
        poolParent = poolObject.transform;
        Object.DontDestroyOnLoad(poolObject);
    }

    public void SetFactory(EnemyFactory factory)
    {
        this.factory = factory;
    }

    public GameObject Get(string enemyType)
    {
        if (string.IsNullOrEmpty(enemyType)) return null;
        if (!pools.ContainsKey(enemyType)) pools[enemyType] = new Queue<GameObject>();
        var pool = pools[enemyType];
        while (pool.Count > 0)
        {
            var enemy = pool.Dequeue();
            if (enemy != null)
            {
                enemy.transform.SetParent(null);
                return enemy;
            }
        }
        return null;
    }

    public void Return(GameObject enemy, string enemyType)
    {
        if (enemy == null) return;
        if (string.IsNullOrEmpty(enemyType)) return;
        if (!pools.ContainsKey(enemyType)) pools[enemyType] = new Queue<GameObject>();
        var pool = pools[enemyType];
        if (pool.Count >= MAX_POOL_SIZE) return;
        var enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();
        if (enemyBehaviour != null) enemyBehaviour.ResetState();
        enemy.transform.SetParent(poolParent);
        enemy.transform.position = Vector3.zero;
        enemy.transform.rotation = Quaternion.identity;
        enemy.SetActive(false);
        pool.Enqueue(enemy);
    }

    public async Task WarmupAsync(string enemyType, int count = INITIAL_POOL_SIZE)
    {
        if (factory == null) return;
        if (string.IsNullOrEmpty(enemyType)) return;
        if (!pools.ContainsKey(enemyType)) pools[enemyType] = new Queue<GameObject>();
        var pool = pools[enemyType];
        for (int i = 0; i < count; i++)
        {
            var enemy = await factory.CreateEnemyForPool(enemyType);
            if (enemy != null)
            {
                enemy.transform.SetParent(poolParent);
                enemy.SetActive(false);
                pool.Enqueue(enemy);
            }
        }
    }

    public async void Warmup(string enemyType, int count = INITIAL_POOL_SIZE)
    {
        await WarmupAsync(enemyType, count);
    }

    public void ClearAll()
    {
        foreach (var poolPair in pools)
        {
            var pool = poolPair.Value;
            while (pool.Count > 0)
            {
                var enemy = pool.Dequeue();
                if (enemy != null)
                {
                    Object.Destroy(enemy);
                }
            }
        }
        pools.Clear();
    }

    public void ClearPool(string enemyType)
    {
        if (pools.ContainsKey(enemyType))
        {
            var pool = pools[enemyType];
            while (pool.Count > 0)
            {
                var enemy = pool.Dequeue();
                if (enemy != null)
                {
                    Object.Destroy(enemy);
                }
            }
        }
    }

    public string GetPoolInfo()
    {
        if (pools.Count == 0) return "No enemy pools initialized";
        var info = "Enemy Pools:\n";
        foreach (var kvp in pools)
        {
            info += $"- {kvp.Key}: {kvp.Value.Count} enemies\n";
        }
        return info.TrimEnd('\n');
    }

    public int GetPoolSize(string enemyType)
    {
        if (pools.ContainsKey(enemyType)) return pools[enemyType].Count;
        return 0;
    }

    public bool IsPoolInitialized(string enemyType)
    {
        return pools.ContainsKey(enemyType);
    }
}
