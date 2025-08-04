using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSystem : IInitializable, IUpdatable
{
    public int InitializationOrder => 20;

    private EnemyFactory enemyFactory;
    private WaveGenerator waveGenerator;
    private bool isSpawning;
    private int currentWave = 1;
    private WaveData currentWaveData;
    private readonly Queue<EnemySpawnData> pendingSpawns = new Queue<EnemySpawnData>();
    private readonly HashSet<GameObject> activeEnemies = new HashSet<GameObject>();

    private float waveTimer;
    private float spawnTimer;
    private bool waveInProgress;
    private bool isInitialized;

    private Camera mainCamera;
    private Vector3 bottomLeft;
    private Vector3 topRight;
    private float screenBoundaryOffset = 2f;
    private bool boundsCalculated = false;

    private WaveConfiguration waveConfig;
    private EnemyDatabase enemyDatabase;

    public IEnumerator Initialize()
    {
        var config = ServiceLocator.Get<SystemsConfiguration>();
        LoadConfigurations();
        var addressableManager = ServiceLocator.Get<AddressableManager>();
        if (addressableManager != null)
        {
            enemyFactory = new EnemyFactory(addressableManager, enemyDatabase);
            yield return CoroutineRunner.Instance.StartCoroutine(WarmupPoolsCoroutine());
        }
        if (waveConfig != null && enemyDatabase != null)
        {
            waveGenerator = new WaveGenerator(waveConfig, enemyDatabase);
        }
        else
        {
            CreateFallbackConfiguration();
        }
        mainCamera = Camera.main;
        CalculateScreenBounds();
        isInitialized = true;
        yield return null;
    }

    private void LoadConfigurations()
    {
        waveConfig = Resources.Load<WaveConfiguration>("WaveConfiguration");
        enemyDatabase = Resources.Load<EnemyDatabase>("EnemyDatabase");
    }

    private void CreateFallbackConfiguration()
    {
        if (waveConfig == null)
        {
            waveConfig = ScriptableObject.CreateInstance<WaveConfiguration>();
        }
        if (enemyDatabase == null)
        {
            enemyDatabase = ScriptableObject.CreateInstance<EnemyDatabase>();
            enemyDatabase.allEnemies = CreateFallbackEnemies();
        }
        waveGenerator = new WaveGenerator(waveConfig, enemyDatabase);
    }

    private List<EnemyConfig> CreateFallbackEnemies()
    {
        var enemies = new List<EnemyConfig>();
        var basicEnemy = ScriptableObject.CreateInstance<EnemyConfig>();
        basicEnemy.enemyId = "fallback_basic";
        basicEnemy.enemyName = "Fallback Enemy";
        basicEnemy.tier = EnemyTier.Tier1;
        basicEnemy.difficultyValue = 1f;
        basicEnemy.maxHealth = 50f;
        basicEnemy.moveSpeed = 3f;
        basicEnemy.attackType = EnemyAttackType.None;
        basicEnemy.movementType = EnemyMovementType.DirectChase;
        basicEnemy.collisionDamage = 10f;
        basicEnemy.explosionDamage = 15f;
        basicEnemy.experienceDrop = 5;
        basicEnemy.enemyColor = Color.gray;
        enemies.Add(basicEnemy);
        return enemies;
    }

    private void CalculateScreenBounds()
    {
        if (mainCamera != null && !boundsCalculated)
        {
            bottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
            topRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.nearClipPlane));
            boundsCalculated = true;
        }
    }

    private IEnumerator WarmupPoolsCoroutine()
    {
        if (enemyFactory == null) yield break;
        var warmupTask = enemyFactory.EnsureWarmedUp();
        while (!warmupTask.IsCompleted)
        {
            yield return null;
        }
    }

    public void StartSpawning()
    {
        if (!isInitialized) return;
        isSpawning = true;
        if (!waveInProgress)
        {
            StartNewWave();
        }
        else
        {
            NotifyUIWaveStarted();
        }
    }

    private void StartNewWave()
    {
        if (waveGenerator == null) return;
        currentWaveData = waveGenerator.GenerateWave(currentWave);
        pendingSpawns.Clear();
        foreach (var spawnData in currentWaveData.enemyComposition)
        {
            pendingSpawns.Enqueue(spawnData);
        }
        waveTimer = 0f;
        spawnTimer = 0f;
        waveInProgress = true;
        activeEnemies.RemoveWhere(enemy => enemy == null);
        NotifyUIWaveStarted();
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }

    public void OnUpdate(float deltaTime)
    {
        if (!isSpawning || !waveInProgress || !isInitialized) return;
        activeEnemies.RemoveWhere(enemy => enemy == null);
        waveTimer += deltaTime;
        spawnTimer += deltaTime;
        ProcessPendingSpawns();
        CheckWaveCompletion();
    }

    private void ProcessPendingSpawns()
    {
        if (pendingSpawns.Count == 0) return;
        var nextSpawn = pendingSpawns.Peek();
        var waveProgress = waveTimer / currentWaveData.duration;
        if (waveProgress >= nextSpawn.spawnDelay)
        {
            pendingSpawns.Dequeue();
            SpawnEnemy(nextSpawn.enemyConfig);
        }
    }

    private void CheckWaveCompletion()
    {
        bool allEnemiesSpawned = pendingSpawns.Count == 0;
        bool noActiveEnemies = activeEnemies.Count == 0;
        bool timeExpired = waveTimer >= currentWaveData.duration;
        if (allEnemiesSpawned && (noActiveEnemies || timeExpired))
        {
            currentWave++;
            StartNewWave();
        }
    }

    private async void SpawnEnemy(EnemyConfig enemyConfig)
    {
        if (enemyFactory == null || enemyConfig == null) return;
        Vector3 spawnPosition = GetRandomSpawnPosition();
        var enemy = await enemyFactory.CreateEnemy(enemyConfig.enemyId, spawnPosition);
        if (enemy != null)
        {
            activeEnemies.Add(enemy);
            NotifyEnemySpawned(enemy);
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (!boundsCalculated)
        {
            CalculateScreenBounds();
        }
        int side = Random.Range(0, 4);
        Vector3 spawnPosition = Vector3.zero;
        switch (side)
        {
            case 0:
                spawnPosition = new Vector3(
                    bottomLeft.x - screenBoundaryOffset,
                    Random.Range(bottomLeft.y, topRight.y),
                    0);
                break;
            case 1:
                spawnPosition = new Vector3(
                    topRight.x + screenBoundaryOffset,
                    Random.Range(bottomLeft.y, topRight.y),
                    0);
                break;
            case 2:
                spawnPosition = new Vector3(
                    Random.Range(bottomLeft.x, topRight.x),
                    topRight.y + screenBoundaryOffset,
                    0);
                break;
            case 3:
                spawnPosition = new Vector3(
                    Random.Range(bottomLeft.x, topRight.x),
                    bottomLeft.y - screenBoundaryOffset,
                    0);
                break;
        }
        return spawnPosition;
    }

    private void NotifyUIWaveStarted()
    {
        if (ServiceLocator.TryGet<UISystem>(out var uiSystem))
        {
            var gameplayUI = uiSystem.GetUIController<GameplayUIController>("GameUI");
            if (gameplayUI != null)
            {
                gameplayUI.UpdateWave(currentWave);
                if (currentWaveData != null)
                {
                    gameplayUI.UpdateWaveInfo(currentWaveData);
                }
            }
        }
    }

    private void NotifyEnemySpawned(GameObject enemy)
    {
    }

    public void Cleanup()
    {
        StopSpawning();
        ClearAllEnemies();
        enemyFactory?.Cleanup();
        enemyFactory = null;
        waveGenerator = null;
        activeEnemies.Clear();
        pendingSpawns.Clear();
        boundsCalculated = false;
        isInitialized = false;
        waveInProgress = false;
        currentWave = 1;
        waveTimer = 0f;
    }

    private void ClearAllEnemies()
    {
        var enemiesToReturn = new List<GameObject>(activeEnemies);
        foreach (var enemy in enemiesToReturn)
        {
            if (enemy != null)
            {
                var pool = ServiceLocator.TryGet<EnemyPool>(out var enemyPool) ? enemyPool : null;
                if (pool != null)
                {
                    var behaviour = enemy.GetComponent<EnemyBehaviour>();
                    string enemyType = behaviour != null ? behaviour.GetConfig()?.enemyId : null;
                    pool.Return(enemy, enemyType);
                }
                else
                {
                    Object.Destroy(enemy);
                }
            }
        }
        activeEnemies.Clear();
    }

    public int GetCurrentWave() => currentWave;
    public bool IsSpawning() => isSpawning;
    public float GetWaveProgress() => currentWaveData != null ? waveTimer / currentWaveData.duration : 0f;
    public int GetActiveEnemiesCount() => activeEnemies.Count;
    public bool IsWaveInProgress() => waveInProgress;
    public int GetPendingSpawnsCount() => pendingSpawns.Count;
    public float GetCurrentWaveDifficulty() => currentWaveData?.difficultyPoints ?? 0f;

    public string GetCurrentWaveInfo()
    {
        if (currentWaveData == null) return "No active wave";
        var info = $"Wave {currentWave}\n";
        info += $"Progress: {GetWaveProgress():P1}\n";
        info += $"Active Enemies: {GetActiveEnemiesCount()}\n";
        info += $"Pending Spawns: {GetPendingSpawnsCount()}\n";
        info += $"Difficulty: {GetCurrentWaveDifficulty():F1}";
        return info;
    }

    public WaveData GetCurrentWaveData() => currentWaveData;

    public TierWeights GetCurrentTierWeights()
    {
        return waveConfig?.GetTierWeights(currentWave) ?? new TierWeights(1f, 0f, 0f, 0f, 0f);
    }

    public void ForceNextWave()
    {
        if (Application.isPlaying && isSpawning)
        {
            pendingSpawns.Clear();
            ClearAllEnemies();
            currentWave++;
            StartNewWave();
        }
    }

    public void ShowCurrentWaveInfo()
    {
    }
}
