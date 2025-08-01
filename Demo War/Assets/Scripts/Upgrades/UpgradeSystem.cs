using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeSystem : IInitializable
{
    public int InitializationOrder => 25;

    private UpgradeDatabase upgradeDatabase;
    private PlayerUpgradeManager upgradeManager;
    private GameObject playerObject;
    private bool isDatabaseLoaded = false;
    private bool isPlayerFound = false;

    public event Action<List<UpgradeConfig>> OnUpgradeOptionsGenerated;
    public event Action<PlayerUpgrade> OnUpgradeApplied;
    public event Action<PlayerUpgrade> OnUpgradeLeveledUp;
    public event Action OnUpgradesReset;

    public IReadOnlyDictionary<string, PlayerUpgrade> ActiveUpgrades => upgradeManager?.ActiveUpgrades;
    public int TotalUpgradeCount => upgradeManager?.TotalUpgradeCount ?? 0;
    public int TotalUpgradeLevels => upgradeManager?.TotalUpgradeLevels ?? 0;

    public IEnumerator Initialize()
    {
        yield return LoadUpgradeDatabaseStrict();

        if (!isDatabaseLoaded || upgradeDatabase == null)
        {
            Debug.LogError("UpgradeSystem: Failed to load upgrade database - system will not function!");
            yield break;
        }

        Debug.Log("UpgradeSystem: Database loaded, but player detection deferred until first upgrade request");
        yield return null;
    }

    private IEnumerator LoadUpgradeDatabaseStrict()
    {
        var addressableManager = ServiceLocator.Get<AddressableManager>();
        if (addressableManager == null)
        {
            Debug.LogError("UpgradeSystem: AddressableManager not found!");
            yield break;
        }

        Debug.Log("UpgradeSystem: Loading UpgradeDatabase from Addressables...");

        var loadTask = addressableManager.LoadAssetAsync<UpgradeDatabase>("UpgradeDatabase");

        while (!loadTask.IsCompleted)
        {
            yield return null;
        }

        upgradeDatabase = loadTask.Result;

        if (upgradeDatabase == null)
        {
            Debug.LogError("UpgradeSystem: UpgradeDatabase not found in Addressables!");
            Debug.LogError("Please ensure UpgradeDatabase is marked as Addressable with key 'UpgradeDatabase'");
            yield break;
        }

        upgradeDatabase.Initialize();

        if (upgradeDatabase.AllUpgrades == null || upgradeDatabase.AllUpgrades.Count == 0)
        {
            Debug.LogError("UpgradeSystem: UpgradeDatabase is empty! Please add UpgradeConfig assets to the database.");
            yield break;
        }

        isDatabaseLoaded = true;
        Debug.Log($"UpgradeSystem: Successfully loaded database with {upgradeDatabase.AllUpgrades.Count} upgrades");

        foreach (var upgrade in upgradeDatabase.AllUpgrades)
        {
            Debug.Log($"- Loaded upgrade: {upgrade.DisplayName} ({upgrade.UpgradeId})");
        }
    }

    private bool EnsurePlayerAndManagerReady()
    {
        if (isPlayerFound && upgradeManager != null)
            return true;

        if (!FindPlayer())
        {
            Debug.LogError("UpgradeSystem: Player object not found!");
            return false;
        }

        if (upgradeManager == null)
        {
            InitializeUpgradeManager();
        }

        return upgradeManager != null;
    }

    private bool FindPlayer()
    {
        if (ServiceLocator.TryGet<GameObject>(out var player))
        {
            playerObject = player;
            isPlayerFound = true;
            Debug.Log($"UpgradeSystem: Found player object: {player.name}");
            return true;
        }

        var playerTag = GameObject.FindWithTag("Player");
        if (playerTag != null)
        {
            playerObject = playerTag;
            isPlayerFound = true;
            ServiceLocator.Register<GameObject>(playerObject);
            Debug.Log($"UpgradeSystem: Found player by tag: {playerTag.name}");
            return true;
        }

        var playerHealth = UnityEngine.Object.FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerObject = playerHealth.gameObject;
            isPlayerFound = true;
            ServiceLocator.Register<GameObject>(playerObject);
            Debug.Log($"UpgradeSystem: Found player by PlayerHealth component: {playerObject.name}");
            return true;
        }

        var playerMovement = UnityEngine.Object.FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerObject = playerMovement.gameObject;
            isPlayerFound = true;
            ServiceLocator.Register<GameObject>(playerObject);
            Debug.Log($"UpgradeSystem: Found player by PlayerMovement component: {playerObject.name}");
            return true;
        }

        Debug.LogError("UpgradeSystem: No player object found by any method!");
        return false;
    }

    private void InitializeUpgradeManager()
    {
        if (upgradeDatabase == null || playerObject == null)
        {
            Debug.LogError("Cannot initialize upgrade manager - missing database or player");
            return;
        }

        upgradeManager = new PlayerUpgradeManager(upgradeDatabase, playerObject);

        upgradeManager.OnUpgradeAdded += HandleUpgradeAdded;
        upgradeManager.OnUpgradeLeveledUp += HandleUpgradeLeveledUp;
        upgradeManager.OnUpgradesReset += HandleUpgradesReset;

        Debug.Log("UpgradeSystem: UpgradeManager initialized successfully");
    }

    private void HandleUpgradeAdded(PlayerUpgrade upgrade)
    {
        OnUpgradeApplied?.Invoke(upgrade);
    }

    private void HandleUpgradeLeveledUp(PlayerUpgrade upgrade)
    {
        OnUpgradeLeveledUp?.Invoke(upgrade);
    }

    private void HandleUpgradesReset()
    {
        OnUpgradesReset?.Invoke();
    }

    public List<UpgradeConfig> GenerateUpgradeOptions(int count = 3)
    {
        if (!isDatabaseLoaded || upgradeDatabase == null)
        {
            Debug.LogError("UpgradeSystem: Database not loaded, cannot generate options");
            return new List<UpgradeConfig>();
        }

        if (!EnsurePlayerAndManagerReady())
        {
            Debug.LogError("UpgradeSystem: Player or manager not ready, cannot generate options");
            return new List<UpgradeConfig>();
        }

        var options = upgradeManager.GenerateUpgradeOptions(count);

        if (options == null || options.Count == 0)
        {
            Debug.LogError("UpgradeSystem: Failed to generate upgrade options from database!");
            Debug.LogError("Check that your UpgradeDatabase has enabled upgrades with proper conditions");
            return new List<UpgradeConfig>();
        }

        Debug.Log($"UpgradeSystem: Generated {options.Count} upgrade options from YOUR database:");
        foreach (var option in options)
        {
            Debug.Log($"- {option.DisplayName}: {option.Description} (ID: {option.UpgradeId})");
        }

        OnUpgradeOptionsGenerated?.Invoke(options);
        return options;
    }

    public bool SelectUpgrade(UpgradeConfig config)
    {
        if (!EnsurePlayerAndManagerReady())
        {
            Debug.LogError("UpgradeSystem: Cannot select upgrade - player or manager not ready");
            return false;
        }

        if (config == null)
        {
            Debug.LogError("UpgradeSystem: Cannot select null upgrade config");
            return false;
        }

        return upgradeManager.ApplyUpgrade(config);
    }

    public bool SelectUpgrade(int optionIndex, List<UpgradeConfig> options)
    {
        if (options == null || optionIndex < 0 || optionIndex >= options.Count)
        {
            Debug.LogError($"UpgradeSystem: Invalid option index {optionIndex} for {options?.Count} options");
            return false;
        }

        return SelectUpgrade(options[optionIndex]);
    }

    public float GetUpgradeMultiplier(UpgradeType type)
    {
        if (!EnsurePlayerAndManagerReady())
            return 1f;

        return upgradeManager.GetUpgradeMultiplier(type);
    }

    public float GetUpgradeBonus(UpgradeType type)
    {
        if (!EnsurePlayerAndManagerReady())
            return 0f;

        return upgradeManager.GetUpgradeBonus(type);
    }

    public PlayerUpgrade GetUpgrade(string upgradeId)
    {
        if (!EnsurePlayerAndManagerReady())
            return null;

        return upgradeManager.GetUpgrade(upgradeId);
    }

    public List<PlayerUpgrade> GetUpgradesByType(UpgradeType type)
    {
        if (!EnsurePlayerAndManagerReady())
            return new List<PlayerUpgrade>();

        return upgradeManager.GetUpgradesByType(type);
    }

    public List<PlayerUpgrade> GetUpgradesByCategory(UpgradeCategory category)
    {
        if (!EnsurePlayerAndManagerReady())
            return new List<PlayerUpgrade>();

        return upgradeManager.GetUpgradesByCategory(category);
    }

    public List<PlayerUpgrade> GetUpgradesByRarity(UpgradeRarity rarity)
    {
        if (!EnsurePlayerAndManagerReady())
            return new List<PlayerUpgrade>();

        return upgradeManager.GetUpgradesByRarity(rarity);
    }

    public bool HasUpgrade(string upgradeId)
    {
        if (!EnsurePlayerAndManagerReady())
            return false;

        return upgradeManager.HasUpgrade(upgradeId);
    }

    public int GetUpgradeLevel(string upgradeId)
    {
        if (!EnsurePlayerAndManagerReady())
            return 0;

        return upgradeManager.GetUpgradeLevel(upgradeId);
    }

    public bool CanApplyUpgrade(UpgradeConfig config)
    {
        if (!EnsurePlayerAndManagerReady())
            return false;

        return upgradeManager.CanApplyUpgrade(config);
    }

    public void ResetUpgrades()
    {
        if (EnsurePlayerAndManagerReady())
        {
            upgradeManager.ResetAllUpgrades();
        }
    }

    public void RefreshAllEffects()
    {
        if (EnsurePlayerAndManagerReady())
        {
            upgradeManager.RefreshAllEffects();
        }
    }

    public UpgradeStatistics GetStatistics()
    {
        if (!EnsurePlayerAndManagerReady())
            return new UpgradeStatistics();

        return upgradeManager.GetStatistics();
    }

    public string GetUpgradesSummary()
    {
        if (!EnsurePlayerAndManagerReady())
            return "No upgrades acquired";

        return upgradeManager.GetUpgradesSummary();
    }

    public string GetDatabaseInfo()
    {
        return upgradeDatabase?.GetDatabaseInfo() ?? "No database loaded";
    }

    public bool IsDatabaseLoaded() => isDatabaseLoaded && upgradeDatabase != null;

    public bool IsPlayerFound() => isPlayerFound && playerObject != null;

    public bool IsFullyReady() => IsDatabaseLoaded() && IsPlayerFound() && upgradeManager != null;

    public void Cleanup()
    {
        if (upgradeManager != null)
        {
            upgradeManager.OnUpgradeAdded -= HandleUpgradeAdded;
            upgradeManager.OnUpgradeLeveledUp -= HandleUpgradeLeveledUp;
            upgradeManager.OnUpgradesReset -= HandleUpgradesReset;
            upgradeManager.Cleanup();
            upgradeManager = null;
        }

        playerObject = null;
        upgradeDatabase = null;
        isDatabaseLoaded = false;
        isPlayerFound = false;

        OnUpgradeOptionsGenerated = null;
        OnUpgradeApplied = null;
        OnUpgradeLeveledUp = null;
        OnUpgradesReset = null;
    }
}