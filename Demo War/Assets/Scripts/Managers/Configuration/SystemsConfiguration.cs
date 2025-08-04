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
    public PlayerStats playerStats;

    [Header("Performance Settings")]
    public int maxEnemiesOnScreen = 100;
    public int poolInitialSize = 20;
    public bool enablePerformanceOptimizations = true;

    [Header("Debug Settings")]
    public bool showWaveDebugInfo = false;
    public bool showDifficultyDebugInfo = false;
    public bool enableWaveSkipping = false;

    [Header("Legacy Player Settings (Deprecated)")]
    [SerializeField] private float playerMoveSpeed = 5f;
    [SerializeField] private float playerHealth = 100f;
    [SerializeField] private float playerDamage = 10f;

    private void OnValidate()
    {
        ValidateReferences();
        UpdateLegacySettings();
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

        if (playerStats == null)
        {
            Debug.LogWarning("PlayerStats is not assigned! Player systems may not work correctly.");
        }
    }

    private void UpdateLegacySettings()
    {
        if (playerStats != null)
        {
            var legacyChanged = false;

            if (playerStats.BaseMoveSpeed != playerMoveSpeed)
            {
                Debug.LogWarning($"Legacy playerMoveSpeed ({playerMoveSpeed}) differs from PlayerStats.BaseMoveSpeed ({playerStats.BaseMoveSpeed}). Using PlayerStats value.");
                legacyChanged = true;
            }

            if (playerStats.BaseMaxHealth != playerHealth)
            {
                Debug.LogWarning($"Legacy playerHealth ({playerHealth}) differs from PlayerStats.BaseMaxHealth ({playerStats.BaseMaxHealth}). Using PlayerStats value.");
                legacyChanged = true;
            }

            if (playerStats.BaseDamage != playerDamage)
            {
                Debug.LogWarning($"Legacy playerDamage ({playerDamage}) differs from PlayerStats.BaseDamage ({playerStats.BaseDamage}). Using PlayerStats value.");
                legacyChanged = true;
            }

            if (legacyChanged)
            {
                Debug.LogWarning("Consider removing legacy player settings and using only PlayerStats.");
            }
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

        if (playerStats != null)
        {
            playerStats.ResetToBase();
            Debug.Log("Player stats reset to base values");
        }
    }

    public string GetSystemStatus()
    {
        var status = "Systems Configuration Status:\n";
        status += $"Wave Config: {(waveConfiguration != null ? "OK" : "MISSING")}\n";
        status += $"Enemy Database: {(enemyDatabase != null ? "OK" : "MISSING")}\n";
        status += $"Difficulty Manager: {(difficultyManager != null ? "OK" : "MISSING")}\n";
        status += $"Player Stats: {(playerStats != null ? "OK" : "MISSING")}\n";
        status += $"Max Enemies: {maxEnemiesOnScreen}\n";
        status += $"Pool Size: {poolInitialSize}\n";
        status += $"Performance Opts: {enablePerformanceOptimizations}";

        if (playerStats != null)
        {
            status += "\n\n" + playerStats.GetStatsDebugInfo();
        }

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

    [ContextMenu("Create Default Player Stats")]
    private void CreateDefaultPlayerStats()
    {
        if (playerStats == null)
        {
            playerStats = ScriptableObject.CreateInstance<PlayerStats>();
            Debug.Log("Created default player stats");
        }
    }

    [ContextMenu("Initialize All Systems")]
    private void InitializeAllSystems()
    {
        CreateDefaultWaveConfiguration();
        CreateDefaultEnemyDatabase();
        CreateDefaultDifficultyManager();
        CreateDefaultPlayerStats();
        InitializeWaveSystem();
        Debug.Log("All systems initialized");
        Debug.Log(GetSystemStatus());
    }

    [ContextMenu("Sync Legacy Settings to PlayerStats")]
    private void SyncLegacySettingsToPlayerStats()
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats is null! Cannot sync legacy settings.");
            return;
        }

        Debug.Log($"Syncing legacy settings to PlayerStats:");
        Debug.Log($"Move Speed: {playerMoveSpeed} -> PlayerStats.BaseMoveSpeed");
        Debug.Log($"Health: {playerHealth} -> PlayerStats.BaseMaxHealth");
        Debug.Log($"Damage: {playerDamage} -> PlayerStats.BaseDamage");

        Debug.Log("Note: You need to manually update the PlayerStats ScriptableObject values in the inspector.");
        Debug.Log("After updating, consider removing the legacy fields from SystemsConfiguration.");
    }
}