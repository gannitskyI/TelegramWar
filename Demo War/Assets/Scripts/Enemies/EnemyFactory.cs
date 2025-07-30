using System.Threading.Tasks;
using UnityEngine;

public class EnemyFactory
{
    private AddressableManager addressableManager;
    private EnemyDatabase enemyDatabase;
    private EnemyPool enemyPool;
    private bool isWarmedUp = false;

    public EnemyFactory(AddressableManager addressableManager, EnemyDatabase database)
    {
        this.addressableManager = addressableManager;
        this.enemyDatabase = database;
        this.enemyPool = new EnemyPool();
        enemyPool.SetFactory(this);

        if (database != null)
        {
            database.Initialize();
        }
    }

    public async Task EnsureWarmedUp()
    {
        if (isWarmedUp) return;
        await WarmupPoolsAsync();
        isWarmedUp = true;
    }

    public async Task<GameObject> CreateEnemy(string enemyId, Vector3 position)
    {
        if (string.IsNullOrEmpty(enemyId))
        {
            Debug.LogError("Enemy ID is null or empty!");
            return null;
        }

        await EnsureWarmedUp();

        var pooledEnemy = enemyPool.Get(enemyId);
        if (pooledEnemy != null)
        {
            pooledEnemy.transform.position = position;
            pooledEnemy.SetActive(true);

            var enemyComponent = pooledEnemy.GetComponent<EnemyBehaviour>();
            if (enemyComponent != null)
            {
                enemyComponent.ResetForReuse(() => ReturnToPool(pooledEnemy, enemyId));
                return pooledEnemy;
            }
            else
            {
                Debug.LogError($"EnemyBehaviour component not found on pooled enemy: {enemyId}");
                ReturnToPool(pooledEnemy, enemyId);
                return null;
            }
        }

        return await CreateNewEnemy(enemyId, position);
    }

    private async Task<GameObject> CreateNewEnemy(string enemyId, Vector3 position)
    {
        var prefabKey = $"Enemy_{enemyId}";
        var enemy = await addressableManager.InstantiateAsync(prefabKey, position);

        if (enemy == null)
        {
            Debug.LogError($"Failed to load enemy prefab: {prefabKey}");
            return null;
        }

        var enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();
        if (enemyBehaviour == null)
        {
            Debug.LogError($"Enemy prefab {prefabKey} doesn't have EnemyBehaviour component!");
            Object.Destroy(enemy);
            return null;
        }

        enemyBehaviour.InitializeFromPrefab(() => ReturnToPool(enemy, enemyId));
        return enemy;
    }

    public async Task<GameObject> CreateEnemyForPool(string enemyId)
    {
        var prefabKey = $"Enemy_{enemyId}";
        var enemy = await addressableManager.InstantiateAsync(prefabKey, Vector3.zero);

        if (enemy == null)
        {
            Debug.LogError($"Failed to create enemy for pool: {prefabKey}");
            return null;
        }

        var enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();
        if (enemyBehaviour == null)
        {
            Debug.LogError($"Enemy prefab {prefabKey} doesn't have EnemyBehaviour component!");
            Object.Destroy(enemy);
            return null;
        }

        enemyBehaviour.InitializeForPool();
        enemy.SetActive(false);
        return enemy;
    }

    private async Task WarmupPoolsAsync()
    {
        if (enemyDatabase?.allEnemies == null)
        {
            Debug.LogWarning("No enemies in database to warmup");
            return;
        }

        try
        {
            foreach (var enemyConfig in enemyDatabase.allEnemies)
            {
                if (enemyConfig != null && !string.IsNullOrEmpty(enemyConfig.enemyId))
                {
                    await enemyPool.WarmupAsync(enemyConfig.enemyId, 2);
                }
            }
            Debug.Log("Enemy pools warmed up successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to warmup pools: {e.Message}");
            isWarmedUp = false;
        }
    }

    private void ReturnToPool(GameObject enemy, string enemyId)
    {
        if (enemy != null)
        {
            enemyPool.Return(enemy, enemyId);
        }
    }

    public void ClearAllPools()
    {
        enemyPool?.ClearAll();
        isWarmedUp = false;
    }

    public string GetDebugInfo()
    {
        var info = enemyPool?.GetPoolInfo() ?? "EnemyPool not initialized";

        if (enemyDatabase?.allEnemies != null)
        {
            info += $"\n\nRegistered Enemies ({enemyDatabase.allEnemies.Count}):\n";
            foreach (var config in enemyDatabase.allEnemies)
            {
                if (config != null)
                {
                    info += $"- {config.enemyId} (Tier {(int)config.tier}, Difficulty: {config.difficultyValue:F1})\n";
                }
            }
        }
        else
        {
            info += "\n\nNo enemy database available";
        }

        return info;
    }

    public void Cleanup()
    {
        ClearAllPools();
        enemyPool = null;
        addressableManager = null;
        enemyDatabase = null;
        isWarmedUp = false;
    }
}