using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerUpgradeManager
{
    private readonly Dictionary<string, PlayerUpgrade> activeUpgrades = new Dictionary<string, PlayerUpgrade>();
    private readonly Dictionary<UpgradeType, List<IUpgradeEffect>> effectsByType = new Dictionary<UpgradeType, List<IUpgradeEffect>>();
    private readonly List<IUpgradeEffect> allEffects = new List<IUpgradeEffect>();
    private readonly UpgradeDatabase upgradeDatabase;
    private readonly GameObject playerObject;

    public event Action<PlayerUpgrade> OnUpgradeAdded;
    public event Action<PlayerUpgrade> OnUpgradeLeveledUp;
    public event Action<string> OnUpgradeRemoved;
    public event Action OnUpgradesReset;

    public IReadOnlyDictionary<string, PlayerUpgrade> ActiveUpgrades => activeUpgrades;
    public int TotalUpgradeCount => activeUpgrades.Count;
    public int TotalUpgradeLevels
    {
        get
        {
            int sum = 0;
            foreach (var u in activeUpgrades.Values) sum += u.CurrentLevel;
            return sum;
        }
    }

    public PlayerUpgradeManager(UpgradeDatabase database, GameObject player)
    {
        upgradeDatabase = database;
        playerObject = player;
        InitializeEffectSystem();
    }

    private void InitializeEffectSystem()
    {
        var effects = new IUpgradeEffect[]
        {
            new DamageUpgradeEffect(playerObject),
            new AttackSpeedUpgradeEffect(playerObject),
            new AttackRangeUpgradeEffect(playerObject),
            new MoveSpeedUpgradeEffect(playerObject),
            new HealthUpgradeEffect(playerObject),
            new HealthRegenUpgradeEffect(playerObject),
            new ExperienceUpgradeEffect(playerObject),
            new CriticalChanceUpgradeEffect(playerObject),
            new CriticalDamageUpgradeEffect(playerObject)
        };

        foreach (var effect in effects)
        {
            if (effect == null) continue;
            allEffects.Add(effect);
            if (!effectsByType.TryGetValue(effect.TargetType, out var list))
            {
                list = new List<IUpgradeEffect>();
                effectsByType[effect.TargetType] = list;
            }
            list.Add(effect);
        }
    }

    public bool CanApplyUpgrade(UpgradeConfig config)
    {
        if (config == null || !config.IsEnabled) return false;
        if (activeUpgrades.TryGetValue(config.UpgradeId, out var existing))
            return config.CanLevelUp(existing.CurrentLevel);
        var context = CreateUpgradeContext();
        return config.CanUnlock(context);
    }

    public bool ApplyUpgrade(UpgradeConfig config)
    {
        if (!CanApplyUpgrade(config)) return false;
        if (activeUpgrades.TryGetValue(config.UpgradeId, out var existing))
            return LevelUpUpgrade(existing, config);
        else
            return AddNewUpgrade(config);
    }

    private bool AddNewUpgrade(UpgradeConfig config)
    {
        var playerUpgrade = new PlayerUpgrade(config, 1);
        activeUpgrades[config.UpgradeId] = playerUpgrade;
        ApplyUpgradeEffects(playerUpgrade);
        OnUpgradeAdded?.Invoke(playerUpgrade);
        return true;
    }

    private bool LevelUpUpgrade(PlayerUpgrade existing, UpgradeConfig config)
    {
        var oldLevel = existing.CurrentLevel;
        existing.LevelUp();
        ApplyUpgradeEffects(existing, oldLevel);
        OnUpgradeLeveledUp?.Invoke(existing);
        return true;
    }

    private void ApplyUpgradeEffects(PlayerUpgrade upgrade, int previousLevel = 0)
    {
        if (!effectsByType.TryGetValue(upgrade.Config.Type, out var effects)) return;
        var oldValue = previousLevel > 0 ? upgrade.Config.CalculateValueAtLevel(previousLevel) : 0f;
        var newValue = upgrade.Config.CalculateValueAtLevel(upgrade.CurrentLevel);
        var deltaValue = newValue - oldValue;
        for (int i = 0; i < effects.Count; i++)
        {
            effects[i].Apply(upgrade.Config, deltaValue, upgrade.CurrentLevel);
        }
    }

    public void RemoveUpgrade(string upgradeId)
    {
        if (!activeUpgrades.TryGetValue(upgradeId, out var upgrade)) return;
        if (effectsByType.TryGetValue(upgrade.Config.Type, out var effects))
        {
            var totalValue = upgrade.Config.CalculateValueAtLevel(upgrade.CurrentLevel);
            for (int i = 0; i < effects.Count; i++)
            {
                effects[i].Remove(upgrade.Config, totalValue);
            }
        }
        activeUpgrades.Remove(upgradeId);
        OnUpgradeRemoved?.Invoke(upgradeId);
    }

    public float GetUpgradeMultiplier(UpgradeType type)
    {
        float multiplier = 1f;
        foreach (var upgrade in activeUpgrades.Values)
        {
            if (upgrade.Config.Type == type)
            {
                var value = upgrade.Config.CalculateValueAtLevel(upgrade.CurrentLevel);
                multiplier += value;
            }
        }
        return multiplier;
    }

    public float GetUpgradeBonus(UpgradeType type)
    {
        return GetUpgradeMultiplier(type) - 1f;
    }

    public PlayerUpgrade GetUpgrade(string upgradeId)
    {
        return activeUpgrades.TryGetValue(upgradeId, out var upgrade) ? upgrade : null;
    }

    public List<PlayerUpgrade> GetUpgradesByType(UpgradeType type)
    {
        var result = new List<PlayerUpgrade>();
        foreach (var u in activeUpgrades.Values)
        {
            if (u.Config.Type == type) result.Add(u);
        }
        return result;
    }

    public List<PlayerUpgrade> GetUpgradesByCategory(UpgradeCategory category)
    {
        var result = new List<PlayerUpgrade>();
        foreach (var u in activeUpgrades.Values)
        {
            if ((u.Config.Category & category) != 0) result.Add(u);
        }
        return result;
    }

    public List<PlayerUpgrade> GetUpgradesByRarity(UpgradeRarity rarity)
    {
        var result = new List<PlayerUpgrade>();
        foreach (var u in activeUpgrades.Values)
        {
            if (u.Config.Rarity == rarity) result.Add(u);
        }
        return result;
    }

    public bool HasUpgrade(string upgradeId)
    {
        return activeUpgrades.ContainsKey(upgradeId);
    }

    public int GetUpgradeLevel(string upgradeId)
    {
        return activeUpgrades.TryGetValue(upgradeId, out var upgrade) ? upgrade.CurrentLevel : 0;
    }

    public List<UpgradeConfig> GenerateUpgradeOptions(int count = 3)
    {
        var context = CreateUpgradeContext();
        var options = upgradeDatabase.GenerateUpgradeSelection(context, count);
        return options ?? new List<UpgradeConfig>();
    }

    private UpgradeContext CreateUpgradeContext()
    {
        var context = new UpgradeContext();
        if (ServiceLocator.TryGet<ScoreSystem>(out var scoreSystem))
        {
            context.PlayerLevel = scoreSystem.GetCurrentLevel();
        }
        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            context.CurrentWave = spawnSystem.GetCurrentWave();
        }
        foreach (var upgrade in activeUpgrades)
        {
            context.OwnedUpgrades[upgrade.Key] = upgrade.Value.CurrentLevel;
        }
        return context;
    }

    public void ResetAllUpgrades()
    {
        var upgradeIds = new List<string>(activeUpgrades.Keys);
        for (int i = 0; i < upgradeIds.Count; i++)
        {
            RemoveUpgrade(upgradeIds[i]);
        }
        for (int i = 0; i < allEffects.Count; i++)
        {
            allEffects[i].Reset();
        }
        OnUpgradesReset?.Invoke();
    }

    public void RefreshAllEffects()
    {
        for (int i = 0; i < allEffects.Count; i++)
        {
            allEffects[i].Reset();
        }
        foreach (var upgrade in activeUpgrades.Values)
        {
            ApplyUpgradeEffects(upgrade);
        }
    }

    public UpgradeStatistics GetStatistics()
    {
        var stats = new UpgradeStatistics();
        foreach (var upgrade in activeUpgrades.Values)
        {
            stats.TotalUpgrades++;
            stats.TotalLevels += upgrade.CurrentLevel;
            if (!stats.UpgradesByType.ContainsKey(upgrade.Config.Type))
                stats.UpgradesByType[upgrade.Config.Type] = 0;
            stats.UpgradesByType[upgrade.Config.Type]++;
            if (!stats.UpgradesByRarity.ContainsKey(upgrade.Config.Rarity))
                stats.UpgradesByRarity[upgrade.Config.Rarity] = 0;
            stats.UpgradesByRarity[upgrade.Config.Rarity]++;
        }
        return stats;
    }

    public string GetUpgradesSummary()
    {
        if (activeUpgrades.Count == 0)
            return "No upgrades acquired";
        var summary = "Active Upgrades:\n";
        var grouped = new Dictionary<UpgradeCategory, List<PlayerUpgrade>>();
        foreach (var upgrade in activeUpgrades.Values)
        {
            var category = upgrade.Config.Category;
            if (!grouped.ContainsKey(category))
                grouped[category] = new List<PlayerUpgrade>();
            grouped[category].Add(upgrade);
        }
        foreach (var kvp in grouped)
        {
            summary += $"\n{kvp.Key}:\n";
            foreach (var upgrade in kvp.Value)
            {
                summary += $"  • {upgrade.Config.DisplayName} Lv.{upgrade.CurrentLevel}\n";
            }
        }
        return summary;
    }

    public void Cleanup()
    {
        for (int i = 0; i < allEffects.Count; i++)
        {
            allEffects[i].Reset();
        }
        activeUpgrades.Clear();
        allEffects.Clear();
        effectsByType.Clear();
        OnUpgradeAdded = null;
        OnUpgradeLeveledUp = null;
        OnUpgradeRemoved = null;
        OnUpgradesReset = null;
    }
}

public class PlayerUpgrade
{
    public UpgradeConfig Config { get; }
    public int CurrentLevel { get; private set; }
    public DateTime AcquiredTime { get; }
    public DateTime LastLevelUpTime { get; private set; }
    public bool IsMaxLevel => CurrentLevel >= Config.MaxLevel;
    public float CurrentValue => Config.CalculateValueAtLevel(CurrentLevel);
    public float NextLevelValue => Config.CalculateValueAtLevel(CurrentLevel + 1);
    public bool CanLevelUp => Config.CanLevelUp(CurrentLevel);

    public PlayerUpgrade(UpgradeConfig config, int initialLevel = 1)
    {
        Config = config;
        CurrentLevel = Mathf.Max(1, initialLevel);
        AcquiredTime = DateTime.Now;
        LastLevelUpTime = AcquiredTime;
    }

    public void LevelUp()
    {
        if (!CanLevelUp) throw new InvalidOperationException();
        CurrentLevel++;
        LastLevelUpTime = DateTime.Now;
    }

    public string GetProgressText()
    {
        return $"{CurrentLevel}/{Config.MaxLevel}";
    }

    public float GetProgressPercentage()
    {
        return (float)CurrentLevel / Config.MaxLevel;
    }
}

public class UpgradeStatistics
{
    public int TotalUpgrades { get; set; }
    public int TotalLevels { get; set; }
    public Dictionary<UpgradeType, int> UpgradesByType { get; set; } = new Dictionary<UpgradeType, int>();
    public Dictionary<UpgradeRarity, int> UpgradesByRarity { get; set; } = new Dictionary<UpgradeRarity, int>();
}
