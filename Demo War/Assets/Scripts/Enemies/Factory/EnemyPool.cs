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
        this.pools = new Dictionary<string, Queue<GameObject>>();

        // ������� ������������ ������ ��� ����
        var poolObject = new GameObject("EnemyPool");
        poolObject.SetActive(false);
        this.poolParent = poolObject.transform;
        Object.DontDestroyOnLoad(poolObject);
    }

    /// <summary>
    /// ������������� ������ �� ������� (������ �������� ����������� �����������)
    /// </summary>
    public void SetFactory(EnemyFactory factory)
    {
        this.factory = factory;
    }

    /// <summary>
    /// �������� ����� �� ����
    /// </summary>
    public GameObject Get(string enemyType)
    {
        if (string.IsNullOrEmpty(enemyType))
        {
            Debug.LogError("Enemy type is null or empty!");
            return null;
        }

        if (!pools.ContainsKey(enemyType))
        {
            pools[enemyType] = new Queue<GameObject>();
        }

        var pool = pools[enemyType];

        while (pool.Count > 0)
        {
            var enemy = pool.Dequeue();
            if (enemy != null)
            {
                enemy.transform.SetParent(null);
                return enemy;
            }
            // ���� ������ ��� ���������, ���������� �����
        }

        return null; // ��� ���� ��� ��� ������� ����������
    }

    /// <summary>
    /// ������� ����� � ���
    /// </summary>
    public void Return(GameObject enemy, string enemyType)
    {
        if (enemy == null)
        {
            Debug.LogWarning("Trying to return null enemy to pool");
            return;
        }

        if (string.IsNullOrEmpty(enemyType))
        {
            Debug.LogError("Enemy type is null or empty when returning to pool!");
            Object.Destroy(enemy);
            return;
        }

        if (!pools.ContainsKey(enemyType))
        {
            pools[enemyType] = new Queue<GameObject>();
        }

        var pool = pools[enemyType];

        // ��������� ����� ����
        if (pool.Count >= MAX_POOL_SIZE)
        {
            Debug.LogWarning($"Pool for {enemyType} is at max capacity, destroying enemy");
            Object.Destroy(enemy);
            return;
        }

        // ���������� ��������� �����
        var enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();
        if (enemyBehaviour != null)
        {
            enemyBehaviour.ResetState();
        }
        else
        {
            Debug.LogWarning($"EnemyBehaviour not found on enemy being returned to pool: {enemyType}");
        }

        // �������� � ���
        enemy.transform.SetParent(poolParent);
        enemy.transform.position = Vector3.zero;
        enemy.transform.rotation = Quaternion.identity;
        enemy.SetActive(false);
        pool.Enqueue(enemy);

        Debug.Log($"Enemy {enemyType} returned to pool. Pool size: {pool.Count}");
    }

    /// <summary>
    /// ������������ ���� ��� ������������� ���� ����� (����������� ������)
    /// </summary>
    public async Task WarmupAsync(string enemyType, int count = INITIAL_POOL_SIZE)
    {
        if (factory == null)
        {
            Debug.LogError("Factory not set! Cannot warmup pool.");
            return;
        }

        if (string.IsNullOrEmpty(enemyType))
        {
            Debug.LogError("Enemy type is null or empty!");
            return;
        }

        if (!pools.ContainsKey(enemyType))
        {
            pools[enemyType] = new Queue<GameObject>();
        }

        var pool = pools[enemyType];
        int successCount = 0;

        for (int i = 0; i < count; i++)
        {
            try
            {
                var enemy = await factory.CreateEnemyForPool(enemyType);
                if (enemy != null)
                {
                    enemy.transform.SetParent(poolParent);
                    enemy.SetActive(false);
                    pool.Enqueue(enemy);
                    successCount++;
                }
                else
                {
                    Debug.LogWarning($"Failed to create enemy {enemyType} for pool warmup (attempt {i + 1})");
                }

                // ��������� �������� ����� ��������� ��������
                await Task.Delay(10);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception during pool warmup for {enemyType} (attempt {i + 1}): {e.Message}");
                break;
            }
        }
 
    }

    /// <summary>
    /// ���������� ������ ��� �������� �������������
    /// </summary>
    public async void Warmup(string enemyType, int count = INITIAL_POOL_SIZE)
    {
        await WarmupAsync(enemyType, count);
    }

    /// <summary>
    /// �������� ��� ����
    /// </summary>
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
        Debug.Log("All enemy pools cleared");
    }

    /// <summary>
    /// �������� ���������� ���
    /// </summary>
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
            Debug.Log($"Pool for {enemyType} cleared");
        }
    }

    /// <summary>
    /// �������� ���������� � ����� ��� ������
    /// </summary>
    public string GetPoolInfo()
    {
        if (pools.Count == 0)
        {
            return "No enemy pools initialized";
        }

        var info = "Enemy Pools:\n";
        foreach (var kvp in pools)
        {
            info += $"- {kvp.Key}: {kvp.Value.Count} enemies\n";
        }
        return info.TrimEnd('\n');
    }

    /// <summary>
    /// �������� ���������� ������ � ���������� ����
    /// </summary>
    public int GetPoolSize(string enemyType)
    {
        if (pools.ContainsKey(enemyType))
        {
            return pools[enemyType].Count;
        }
        return 0;
    }

    /// <summary>
    /// ���������, ��������������� �� ��� ��� ���� �����
    /// </summary>
    public bool IsPoolInitialized(string enemyType)
    {
        return pools.ContainsKey(enemyType);
    }
}