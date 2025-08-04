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
            yield break;
        }
        yield return null;
    }

    private IEnumerator LoadUpgradeDatabaseStrict()
    {
        var addressableManager = ServiceLocator.Get<AddressableManager>();
        if (addressableManager == null)
        {
            yield break;
        }
        var loadTask = addressableManager.LoadAssetAsync<UpgradeDatabase>("UpgradeDatabase");
        while (!loadTask.IsCompleted)
        {
            yield return null;
        }
        upgradeDatabase = loadTask.Result;
        if (upgradeDatabase == null)
        {
            yield break;
        }
        upgradeDatabase.Initialize();
        if (upgradeDatabase.AllUpgrades == null || upgradeDatabase.AllUpgrades.Count == 0)
        {
            yield break;
        }
        isDatabaseLoaded = true;
    }

    private bool EnsurePlayerAndManagerReady()
    {
        if (isPlayerFound && upgradeManager != null)
            return true;
        if (!FindPlayer())
        {
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
        if (playerObject != null)
        {
            isPlayerFound = true;
            return true;
        }
        if (ServiceLocator.TryGet<GameObject>(out var player))
        {
            playerObject = player;
            isPlayerFound = true;
            return true;
        }
        var playerTag = GameObject.FindWithTag("Player");
        if (playerTag != null)
        {
            playerObject = playerTag;
            isPlayerFound = true;
            ServiceLocator.Register<GameObject>(playerObject);
            return true;
        }
        var playerHealth = UnityEngine.Object.FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerObject = playerHealth.gameObject;
            isPlayerFound = true;
            ServiceLocator.Register<GameObject>(playerObject);
            return true;
        }
        var playerMovement = UnityEngine.Object.FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerObject = playerMovement.gameObject;
            isPlayerFound = true;
            ServiceLocator.Register<GameObject>(playerObject);
            return true;
        }
        return false;
    }

    private void InitializeUpgradeManager()
    {
        if (upgradeDatabase == null || playerObject == null)
        {
            return;
        }
        upgradeManager = new PlayerUpgradeManager(upgradeDatabase, playerObject);
        upgradeManager.OnUpgradeAdded += HandleUpgradeAdded;
        upgradeManager.OnUpgradeLeveledUp += HandleUpgradeLeveledUp;
        upgradeManager.OnUpgradesReset += HandleUpgradesReset;
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
            return new List<UpgradeConfig>();
        }
        if (!EnsurePlayerAndManagerReady())
        {
            return new List<UpgradeConfig>();
        }
        var options = upgradeManager.GenerateUpgradeOptions(count);
        if (options == null || options.Count == 0)
        {
            return new List<UpgradeConfig>();
        }
        OnUpgradeOptionsGenerated?.Invoke(options);
        return options;
    }

    public bool SelectUpgrade(UpgradeConfig config)
    {
        if (!EnsurePlayerAndManagerReady())
        {
            return false;
        }
        if (config == null)
        {
            return false;
        }
        return upgradeManager.ApplyUpgrade(config);
    }

    public bool SelectUpgrade(int optionIndex, List<UpgradeConfig> options)
    {
        if (options == null || optionIndex < 0 || optionIndex >= options.Count)
        {
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
