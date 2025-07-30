using UnityEngine;

[CreateAssetMenu(fileName = "SystemsConfiguration", menuName = "Game/Systems Configuration")]
public class SystemsConfiguration : ScriptableObject
{
    [Header("Game Settings")]
    public float roundDuration = 30f;
    public int startingLives = 3;

    [Header("Wave System")]
    public WaveConfiguration waveConfiguration;
    public EnemyDatabase enemyDatabase;
    public DifficultyManager difficultyManager;

    [Header("Player Settings")]
    public float playerMoveSpeed = 5f;
    public float playerHealth = 100f;
    public float playerDamage = 10f;

    [Header("Performance Settings")]
    public int maxEnemiesOnScreen = 100;
    public int poolInitialSize = 20;
    public bool enablePerformanceOptimizations = true;

    [Header("Debug Settings")]
    public bool showWaveDebugInfo = false;
    public bool showDifficultyDebugInfo = false;
    public bool enableWaveSkipping = false;

    private void OnValidate()
    {
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (waveConfiguration == null)
        {
            Debug.LogWarning("WaveConfiguration is not assigned!");
        }

        if (enemyDatabase == null)
        {
            Debug.LogWarning("EnemyDatabase is not assigned!");
        }

        if (difficultyManager == null)
        {
            Debug.LogWarning("DifficultyManager is not assigned!");
        }
    }

    public void InitializeWaveSystem()
    {
        if (enemyDatabase != null)
        {
            enemyDatabase.Initialize();
        }

        if (difficultyManager != null)
        {
            difficultyManager.ResetDifficulty();
        }
    }

    public string GetSystemStatus()
    {
        var status = "Systems Configuration Status:\n";
        status += $"Wave Config: {(waveConfiguration != null ? "OK" : "MISSING")}\n";
        status += $"Enemy Database: {(enemyDatabase != null ? "OK" : "MISSING")}\n";
        status += $"Difficulty Manager: {(difficultyManager != null ? "OK" : "MISSING")}\n";
        status += $"Max Enemies: {maxEnemiesOnScreen}\n";
        status += $"Pool Size: {poolInitialSize}\n";
        status += $"Performance Opts: {enablePerformanceOptimizations}";

        if (enemyDatabase != null)
        {
            status += "\n\n" + enemyDatabase.GetDatabaseInfo();
        }

        if (difficultyManager != null && showDifficultyDebugInfo)
        {
            status += "\n\n" + difficultyManager.GetDifficultyStatus();
        }

        return status;
    }

    [ContextMenu("Create Default Wave Configuration")]
    private void CreateDefaultWaveConfiguration()
    {
        if (waveConfiguration == null)
        {
            waveConfiguration = ScriptableObject.CreateInstance<WaveConfiguration>();
            Debug.Log("Created default wave configuration");
        }
    }

    [ContextMenu("Create Default Enemy Database")]
    private void CreateDefaultEnemyDatabase()
    {
        if (enemyDatabase == null)
        {
            enemyDatabase = ScriptableObject.CreateInstance<EnemyDatabase>();
            Debug.Log("Created empty enemy database. Please assign your EnemyConfig assets manually.");
        }
    }

    [ContextMenu("Create Default Difficulty Manager")]
    private void CreateDefaultDifficultyManager()
    {
        if (difficultyManager == null)
        {
            difficultyManager = ScriptableObject.CreateInstance<DifficultyManager>();
            Debug.Log("Created default difficulty manager");
        }
    }

    [ContextMenu("Initialize All Systems")]
    private void InitializeAllSystems()
    {
        CreateDefaultWaveConfiguration();
        CreateDefaultEnemyDatabase();
        CreateDefaultDifficultyManager();
        InitializeWaveSystem();
        Debug.Log("All systems initialized");
        Debug.Log(GetSystemStatus());
    }
}