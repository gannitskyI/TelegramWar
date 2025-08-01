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

        var enemyConfig = enemyDatabase?.GetEnemyById(enemyId);
        if (enemyConfig == null)
        {
            Debug.LogError($"Enemy config not found for ID: {enemyId}");
            return null;
        }

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

        return await CreateNewEnemy(enemyId, position, enemyConfig);
    }

    private async Task<GameObject> CreateNewEnemy(string enemyId, Vector3 position, EnemyConfig enemyConfig)
    {
        var prefabKey = $"Enemy_{enemyId}";
        var enemy = await addressableManager.InstantiateAsync(prefabKey, position);

        if (enemy == null)
        {
            enemy = CreateFallbackEnemy(position, enemyConfig);
        }

        var enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();
        if (enemyBehaviour == null)
        {
            enemyBehaviour = enemy.AddComponent<EnemyBehaviour>();
        }

        enemyBehaviour.Initialize(enemyConfig, () => ReturnToPool(enemy, enemyId));
        return enemy;
    }

    private GameObject CreateFallbackEnemy(Vector3 position, EnemyConfig enemyConfig)
    {
        var enemyGO = new GameObject($"Enemy_{enemyConfig.enemyId}");
        enemyGO.transform.position = position;

        var renderer = enemyGO.AddComponent<SpriteRenderer>();
        string spriteKey = GetSpriteKeyForTier(enemyConfig.tier);
        renderer.sprite = SpriteCache.GetSprite(spriteKey);
        renderer.color = enemyConfig.enemyColor;
        renderer.sortingOrder = 5;

        var rb = enemyGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 2f;

        var collider = enemyGO.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f * enemyConfig.scale;

        enemyGO.AddComponent<EnemyBehaviour>();

        try { enemyGO.tag = "Enemy"; } catch { }

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) enemyGO.layer = enemyLayer;

        Debug.Log($"Created fallback enemy: {enemyConfig.enemyName}");
        return enemyGO;
    }

    private string GetSpriteKeyForTier(EnemyTier tier)
    {
        return tier switch
        {
            EnemyTier.Tier1 => "enemy_basic",
            EnemyTier.Tier2 => "enemy_basic",
            EnemyTier.Tier3 => "enemy_elite",
            EnemyTier.Tier4 => "enemy_boss",
            EnemyTier.Tier5 => "enemy_boss",
            _ => "enemy_basic"
        };
    }

    public async Task<GameObject> CreateEnemyForPool(string enemyId)
    {
        var enemyConfig = enemyDatabase?.GetEnemyById(enemyId);
        if (enemyConfig == null)
        {
            Debug.LogError($"Enemy config not found for pool creation: {enemyId}");
            return null;
        }

        var prefabKey = $"Enemy_{enemyId}";
        var enemy = await addressableManager.InstantiateAsync(prefabKey, Vector3.zero);

        if (enemy == null)
        {
            enemy = CreateFallbackEnemy(Vector3.zero, enemyConfig);
        }

        var enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();
        if (enemyBehaviour == null)
        {
            enemyBehaviour = enemy.AddComponent<EnemyBehaviour>();
        }

        enemyBehaviour.InitializeForPool(enemyConfig);
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