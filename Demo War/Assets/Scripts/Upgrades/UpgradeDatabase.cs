using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Game/Upgrade Database")]
public class UpgradeDatabase : ScriptableObject
{
    [Header("Upgrade Configurations")]
    [SerializeField] private List<UpgradeConfig> allUpgrades = new List<UpgradeConfig>();

    [Header("Selection Settings")]
    [SerializeField] private int defaultSelectionCount = 3;
    [SerializeField] private bool allowDuplicateTypes = false;
    [SerializeField] private float rarityBonusMultiplier = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private Dictionary<string, UpgradeConfig> upgradeById;
    private Dictionary<UpgradeType, List<UpgradeConfig>> upgradesByType;
    private Dictionary<UpgradeCategory, List<UpgradeConfig>> upgradesByCategory;
    private Dictionary<UpgradeRarity, List<UpgradeConfig>> upgradesByRarity;
    private List<UpgradeConfig> enabledUpgrades;

    public IReadOnlyList<UpgradeConfig> AllUpgrades => allUpgrades;
    public int DefaultSelectionCount => defaultSelectionCount;
    public bool AllowDuplicateTypes => allowDuplicateTypes;

    private void OnEnable()
    {
        Initialize();
    }

    public void Initialize()
    {
        BuildLookupTables();
        ValidateDatabase();
    }

    private void BuildLookupTables()
    {
        upgradeById = new Dictionary<string, UpgradeConfig>();
        upgradesByType = new Dictionary<UpgradeType, List<UpgradeConfig>>();
        upgradesByCategory = new Dictionary<UpgradeCategory, List<UpgradeConfig>>();
        upgradesByRarity = new Dictionary<UpgradeRarity, List<UpgradeConfig>>();
        enabledUpgrades = new List<UpgradeConfig>();

        foreach (var upgrade in allUpgrades)
        {
            if (upgrade == null) continue;

            var id = upgrade.UpgradeId;
            if (upgradeById.ContainsKey(id))
            {
                Debug.LogError($"Duplicate upgrade ID found: {id}");
                continue;
            }

            upgradeById[id] = upgrade;

            if (upgrade.IsEnabled)
            {
                enabledUpgrades.Add(upgrade);
            }

            if (!upgradesByType.ContainsKey(upgrade.Type))
                upgradesByType[upgrade.Type] = new List<UpgradeConfig>();
            upgradesByType[upgrade.Type].Add(upgrade);

            if (!upgradesByCategory.ContainsKey(upgrade.Category))
                upgradesByCategory[upgrade.Category] = new List<UpgradeConfig>();
            upgradesByCategory[upgrade.Category].Add(upgrade);

            if (!upgradesByRarity.ContainsKey(upgrade.Rarity))
                upgradesByRarity[upgrade.Rarity] = new List<UpgradeConfig>();
            upgradesByRarity[upgrade.Rarity].Add(upgrade);
        }
    }

    private void ValidateDatabase()
    {
        var errors = new List<string>();

        if (allUpgrades.Count == 0)
        {
            errors.Add("No upgrades configured in database");
        }

        var duplicateIds = allUpgrades
            .Where(u => u != null)
            .GroupBy(u => u.UpgradeId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var id in duplicateIds)
        {
            errors.Add($"Duplicate upgrade ID: {id}");
        }

        foreach (var upgrade in allUpgrades.Where(u => u != null))
        {
            foreach (var requiredId in upgrade.RequiredUpgrades)
            {
                if (!upgradeById.ContainsKey(requiredId))
                {
                    errors.Add($"Upgrade '{upgrade.UpgradeId}' requires non-existent upgrade '{requiredId}'");
                }
            }

            foreach (var conflictingId in upgrade.ConflictingUpgrades)
            {
                if (!upgradeById.ContainsKey(conflictingId))
                {
                    errors.Add($"Upgrade '{upgrade.UpgradeId}' conflicts with non-existent upgrade '{conflictingId}'");
                }
            }
        }

        if (errors.Count > 0 && enableDebugLogs)
        {
            Debug.LogError($"UpgradeDatabase validation errors:\n{string.Join("\n", errors)}");
        }
    }

    public UpgradeConfig GetUpgrade(string upgradeId)
    {
        return upgradeById.TryGetValue(upgradeId, out var upgrade) ? upgrade : null;
    }

    public List<UpgradeConfig> GetUpgradesByType(UpgradeType type)
    {
        return upgradesByType.TryGetValue(type, out var upgrades)
            ? new List<UpgradeConfig>(upgrades)
            : new List<UpgradeConfig>();
    }

    public List<UpgradeConfig> GetUpgradesByCategory(UpgradeCategory category)
    {
        return upgradesByCategory.TryGetValue(category, out var upgrades)
            ? new List<UpgradeConfig>(upgrades)
            : new List<UpgradeConfig>();
    }

    public List<UpgradeConfig> GetUpgradesByRarity(UpgradeRarity rarity)
    {
        return upgradesByRarity.TryGetValue(rarity, out var upgrades)
            ? new List<UpgradeConfig>(upgrades)
            : new List<UpgradeConfig>();
    }

    public List<UpgradeConfig> GetAvailableUpgrades(UpgradeContext context)
    {
        return enabledUpgrades
            .Where(upgrade => !upgrade.IsHidden && upgrade.CanUnlock(context))
            .ToList();
    }

    public List<UpgradeConfig> GenerateUpgradeSelection(UpgradeContext context, int count = -1)
    {
        if (count <= 0) count = defaultSelectionCount;

        var availableUpgrades = GetAvailableUpgrades(context);
        if (availableUpgrades.Count == 0)
        {
            return new List<UpgradeConfig>();
        }

        var selection = new List<UpgradeConfig>();
        var usedTypes = new HashSet<UpgradeType>();
        var weightedUpgrades = CreateWeightedList(availableUpgrades, context);

        for (int i = 0; i < count && weightedUpgrades.Count > 0; i++)
        {
            var selectedUpgrade = SelectWeightedRandom(weightedUpgrades);
            selection.Add(selectedUpgrade);

            weightedUpgrades.RemoveAll(wu => wu.upgrade == selectedUpgrade);

            if (!allowDuplicateTypes)
            {
                usedTypes.Add(selectedUpgrade.Type);
                weightedUpgrades.RemoveAll(wu => usedTypes.Contains(wu.upgrade.Type));
            }
        }

        return selection;
    }

    private List<WeightedUpgrade> CreateWeightedList(List<UpgradeConfig> upgrades, UpgradeContext context)
    {
        var weightedList = new List<WeightedUpgrade>();

        foreach (var upgrade in upgrades)
        {
            var baseWeight = upgrade.GetSelectionWeightAtPlayerLevel(context.PlayerLevel);
            var rarityMultiplier = GetRarityMultiplier(upgrade.Rarity);
            var finalWeight = baseWeight * rarityMultiplier;

            weightedList.Add(new WeightedUpgrade { upgrade = upgrade, weight = finalWeight });
        }

        return weightedList;
    }

    private float GetRarityMultiplier(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Common => 1f,
            UpgradeRarity.Uncommon => 1f / rarityBonusMultiplier,
            UpgradeRarity.Rare => 1f / (rarityBonusMultiplier * rarityBonusMultiplier),
            UpgradeRarity.Epic => 1f / (rarityBonusMultiplier * rarityBonusMultiplier * rarityBonusMultiplier),
            UpgradeRarity.Legendary => 1f / (rarityBonusMultiplier * rarityBonusMultiplier * rarityBonusMultiplier * rarityBonusMultiplier),
            _ => 1f
        };
    }

    private UpgradeConfig SelectWeightedRandom(List<WeightedUpgrade> weightedUpgrades)
    {
        if (weightedUpgrades == null || weightedUpgrades.Count == 0)
        {
            return null; // Возвращаем null, если список пуст
        }

        var totalWeight = weightedUpgrades.Sum(wu => wu.weight);
        var randomValue = Random.Range(0f, totalWeight);
        var currentWeight = 0f;

        foreach (var weightedUpgrade in weightedUpgrades)
        {
            currentWeight += weightedUpgrade.weight;
            if (randomValue <= currentWeight)
            {
                return weightedUpgrade.upgrade;
            }
        }

        // Возвращаем последний элемент, если ни один не выбран
        return weightedUpgrades.Last().upgrade;
    }

    public bool ValidateUpgradeChain(List<string> upgradeIds)
    {
        var processedUpgrades = new HashSet<string>();

        foreach (var upgradeId in upgradeIds)
        {
            var upgrade = GetUpgrade(upgradeId);
            if (upgrade == null) return false;

            foreach (var requiredId in upgrade.RequiredUpgrades)
            {
                if (!processedUpgrades.Contains(requiredId))
                    return false;
            }

            foreach (var conflictingId in upgrade.ConflictingUpgrades)
            {
                if (processedUpgrades.Contains(conflictingId))
                    return false;
            }

            processedUpgrades.Add(upgradeId);
        }

        return true;
    }

    public string GetDatabaseInfo()
    {
        var info = $"Upgrade Database ({allUpgrades.Count} total upgrades)\n";
        info += $"Enabled: {enabledUpgrades.Count}\n";
        info += "By Type:\n";

        foreach (var kvp in upgradesByType.OrderBy(k => k.Key))
        {
            info += $"  {kvp.Key}: {kvp.Value.Count}\n";
        }

        info += "By Rarity:\n";
        foreach (var kvp in upgradesByRarity.OrderBy(k => k.Key))
        {
            info += $"  {kvp.Key}: {kvp.Value.Count}\n";
        }

        return info;
    }

    [ContextMenu("Validate Database")]
    private void ValidateDatabaseFromMenu()
    {
        Initialize();
        Debug.Log(GetDatabaseInfo());
    }

    [ContextMenu("Auto-Assign Missing IDs")]
    private void AutoAssignMissingIds()
    {
        foreach (var upgrade in allUpgrades.Where(u => u != null))
        {
            if (string.IsNullOrEmpty(upgrade.UpgradeId))
            {
                var id = upgrade.name.ToLower().Replace(" ", "_");
                Debug.Log($"Auto-assigned ID '{id}' to upgrade '{upgrade.name}'");
            }
        }
    }

    private struct WeightedUpgrade
    {
        public UpgradeConfig upgrade;
        public float weight;
    }
}