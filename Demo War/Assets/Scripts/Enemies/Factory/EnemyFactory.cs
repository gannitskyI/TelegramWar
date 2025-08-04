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

    public async Task EnsureWarmedUp(int poolSize = 10)
    {
        if (isWarmedUp) return;
        await WarmupPoolsAsync(poolSize);
        isWarmedUp = true;
    }

    public async Task<GameObject> CreateEnemy(string enemyId, Vector3 position)
    {
        if (string.IsNullOrEmpty(enemyId)) return null;
        await EnsureWarmedUp();
        var enemyConfig = enemyDatabase?.GetEnemyById(enemyId);
        if (enemyConfig == null) return null;

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
        renderer.sprite = SpriteCache.GetEnemySprite(enemyConfig.tier, enemyConfig.enemyColor);
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
        return enemyGO;
    }

    public async Task<GameObject> CreateEnemyForPool(string enemyId)
    {
        var enemyConfig = enemyDatabase?.GetEnemyById(enemyId);
        if (enemyConfig == null) return null;
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

    private async Task WarmupPoolsAsync(int poolSize)
    {
        if (enemyDatabase?.allEnemies == null) return;
        foreach (var enemyConfig in enemyDatabase.allEnemies)
        {
            if (enemyConfig != null && !string.IsNullOrEmpty(enemyConfig.enemyId))
            {
                await enemyPool.WarmupAsync(enemyConfig.enemyId, poolSize);
            }
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

    public void Cleanup()
    {
        ClearAllPools();
        enemyPool = null;
        addressableManager = null;
        enemyDatabase = null;
        isWarmedUp = false;
    }
}
