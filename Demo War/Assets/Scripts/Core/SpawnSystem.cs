using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSystem : IInitializable, IUpdatable
{
    public int InitializationOrder => 20;

    private EnemyFactory enemyFactory;
    private bool isSpawning;
    private float spawnTimer;
    private int currentWave = 1;
    private int enemiesSpawnedThisWave = 0;
    private int enemiesToSpawnThisWave = 3;
    private float spawnInterval = 2f;
    private float waveTimer = 0f;
    private float waveDuration = 30f;
    private bool waveInProgress = false;

    private SystemsConfiguration config;
    private readonly string[] availableEnemyTypes = { "weak", "normal", "strong", "fast", "tank" };
    private readonly HashSet<GameObject> activeEnemies = new HashSet<GameObject>();

    private Camera mainCamera;
    private Vector3 bottomLeft;
    private Vector3 topRight;
    private float screenBoundaryOffset = 2f;
    private bool boundsCalculated = false;

    public IEnumerator Initialize()
    {
        config = ServiceLocator.Get<SystemsConfiguration>();
        if (config != null)
        {
            spawnInterval = config.spawnInterval;
            enemiesToSpawnThisWave = config.enemiesPerWave;
            waveDuration = config.roundDuration;
        }
        else
        {
            waveDuration = 30f;
        }

        var addressableManager = ServiceLocator.Get<AddressableManager>();
        if (addressableManager != null)
        {
            enemyFactory = new EnemyFactory(addressableManager);
            yield return CoroutineRunner.Instance.StartCoroutine(WarmupPoolsCoroutine());
        }

        mainCamera = Camera.main;
        CalculateScreenBounds();
        yield return null;
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
        isSpawning = true;
        waveInProgress = true;
        spawnTimer = 0f;
        waveTimer = 0f;
        currentWave = 1;
        enemiesSpawnedThisWave = 0;
        activeEnemies.Clear();

        if (config != null)
        {
            enemiesToSpawnThisWave = config.enemiesPerWave;
            spawnInterval = config.spawnInterval;
        }

        NotifyUIWaveStarted();
        spawnTimer = spawnInterval;
    }

    public void StopSpawning()
    {
        isSpawning = false;
        waveInProgress = false;
    }

    public void OnUpdate(float deltaTime)
    {
        if (!isSpawning || !waveInProgress) return;

        activeEnemies.RemoveWhere(enemy => enemy == null);

        spawnTimer += deltaTime;
        waveTimer += deltaTime;

        if (spawnTimer >= spawnInterval && enemiesSpawnedThisWave < enemiesToSpawnThisWave)
        {
            SpawnEnemy();
            spawnTimer = 0f;
        }

        CheckWaveCompletion();
    }

    private void CheckWaveCompletion()
    {
        bool timeExpired = waveTimer >= waveDuration; // Изменено для отсчета вперед
        bool allEnemiesSpawned = enemiesSpawnedThisWave >= enemiesToSpawnThisWave;
        bool noActiveEnemies = activeEnemies.Count == 0;

        if (timeExpired || (allEnemiesSpawned && noActiveEnemies))
        {
            StartNextWave();
        }
    }

    private void SpawnEnemy()
    {
        if (enemyFactory == null) return;

        string enemyType = GetRandomEnemyTypeOptimized();
        Vector3 spawnPosition = GetRandomSpawnPositionOptimized();
        _ = SpawnEnemyAsync(enemyType, spawnPosition);
        enemiesSpawnedThisWave++;
    }

    private string GetRandomEnemyTypeOptimized()
    {
        if (currentWave <= 2)
        {
            return Random.Range(0, 100) < 80 ? "weak" : "normal";
        }
        else if (currentWave <= 5)
        {
            int rand = Random.Range(0, 100);
            if (rand < 40) return "weak";
            if (rand < 70) return "normal";
            if (rand < 90) return "fast";
            return "strong";
        }
        else
        {
            int rand = Random.Range(0, 100);
            if (rand < 20) return "normal";
            if (rand < 40) return "strong";
            if (rand < 60) return "fast";
            if (rand < 80) return "tank";
            return "strong";
        }
    }

    private Vector3 GetRandomSpawnPositionOptimized()
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

    private async System.Threading.Tasks.Task SpawnEnemyAsync(string enemyType, Vector3 position)
    {
        try
        {
            var enemy = await enemyFactory.CreateEnemy(enemyType, position);
            if (enemy != null)
            {
                activeEnemies.Add(enemy);
                NotifyEnemySpawned(enemy);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error spawning enemy: {e.Message}");
        }
    }

    private void StartNextWave()
    {
        currentWave++;
        enemiesSpawnedThisWave = 0;
        waveTimer = 0f;
        activeEnemies.Clear();

        if (config != null)
        {
            enemiesToSpawnThisWave = Mathf.RoundToInt(config.enemiesPerWave * (1 + currentWave * config.difficultyIncreaseRate));
            spawnInterval = Mathf.Max(0.5f, config.spawnInterval * (1 - currentWave * 0.05f));
        }
        else
        {
            enemiesToSpawnThisWave += 2;
            spawnInterval = Mathf.Max(0.5f, spawnInterval - 0.1f);
        }

        NotifyUIWaveStarted();
        CoroutineRunner.StartRoutine(WaveTransitionDelay());
    }

    private IEnumerator WaveTransitionDelay()
    {
        yield return new WaitForSeconds(2f);
        spawnTimer = spawnInterval;
    }

    private void NotifyUIWaveStarted()
    {
        if (ServiceLocator.TryGet<UISystem>(out var uiSystem))
        {
            var gameplayUI = uiSystem.GetUIController<GameplayUIController>("GameUI");
            gameplayUI?.UpdateWave(currentWave);
        }
    }

    private void NotifyEnemySpawned(GameObject enemy)
    {
    }

    public void Cleanup()
    {
        StopSpawning();
        ClearAllEnemies();

        if (enemyFactory != null)
        {
            enemyFactory.Cleanup();
            enemyFactory = null;
        }

        activeEnemies.Clear();
        boundsCalculated = false;
    }

    private void ClearAllEnemies()
    {
        var enemiesToDestroy = new List<GameObject>(activeEnemies);
        foreach (var enemy in enemiesToDestroy)
        {
            if (enemy != null)
            {
                UnityEngine.Object.Destroy(enemy);
            }
        }
        activeEnemies.Clear();
    }

    public int GetCurrentWave() => currentWave;
    public int GetEnemiesSpawnedThisWave() => enemiesSpawnedThisWave;
    public int GetEnemiesToSpawnThisWave() => enemiesToSpawnThisWave;
    public bool IsSpawning() => isSpawning;
    public float GetWaveTimeRemaining() => waveTimer;  
    public int GetActiveEnemiesCount() => activeEnemies.Count;
    public bool IsWaveInProgress() => waveInProgress;
}