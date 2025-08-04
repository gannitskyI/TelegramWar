using System.Collections.Generic;
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

    private Dictionary<string, UpgradeConfig> upgradeById;
    private Dictionary<UpgradeType, List<UpgradeConfig>> upgradesByType;
    private Dictionary<UpgradeCategory, List<UpgradeConfig>> upgradesByCategory;
    private Dictionary<UpgradeRarity, List<UpgradeConfig>> upgradesByRarity;
    private List<UpgradeConfig> enabledUpgrades;

    public IReadOnlyList<UpgradeConfig> AllUpgrades => allUpgrades;
    public int DefaultSelectionCount => defaultSelectionCount;
    public bool AllowDuplicateTypes => allowDuplicateTypes;

    private void OnEnable() => Initialize();

    public void Initialize()
    {
        upgradeById = new Dictionary<string, UpgradeConfig>(allUpgrades.Count);
        upgradesByType = new Dictionary<UpgradeType, List<UpgradeConfig>>();
        upgradesByCategory = new Dictionary<UpgradeCategory, List<UpgradeConfig>>();
        upgradesByRarity = new Dictionary<UpgradeRarity, List<UpgradeConfig>>();
        enabledUpgrades = new List<UpgradeConfig>(allUpgrades.Count);

        for (int i = 0; i < allUpgrades.Count; i++)
        {
            var upgrade = allUpgrades[i];
            if (upgrade == null) continue;
            var id = upgrade.UpgradeId;
            if (!upgradeById.ContainsKey(id)) upgradeById[id] = upgrade;
            if (upgrade.IsEnabled) enabledUpgrades.Add(upgrade);

            if (!upgradesByType.TryGetValue(upgrade.Type, out var byType))
            {
                byType = new List<UpgradeConfig>();
                upgradesByType[upgrade.Type] = byType;
            }
            byType.Add(upgrade);

            if (!upgradesByCategory.TryGetValue(upgrade.Category, out var byCat))
            {
                byCat = new List<UpgradeConfig>();
                upgradesByCategory[upgrade.Category] = byCat;
            }
            byCat.Add(upgrade);

            if (!upgradesByRarity.TryGetValue(upgrade.Rarity, out var byRar))
            {
                byRar = new List<UpgradeConfig>();
                upgradesByRarity[upgrade.Rarity] = byRar;
            }
            byRar.Add(upgrade);
        }
    }

    public UpgradeConfig GetUpgrade(string upgradeId)
    {
        return upgradeById.TryGetValue(upgradeId, out var upgrade) ? upgrade : null;
    }

    public List<UpgradeConfig> GetUpgradesByType(UpgradeType type)
    {
        return upgradesByType.TryGetValue(type, out var upgrades) ? new List<UpgradeConfig>(upgrades) : new List<UpgradeConfig>();
    }

    public List<UpgradeConfig> GetUpgradesByCategory(UpgradeCategory category)
    {
        return upgradesByCategory.TryGetValue(category, out var upgrades) ? new List<UpgradeConfig>(upgrades) : new List<UpgradeConfig>();
    }

    public List<UpgradeConfig> GetUpgradesByRarity(UpgradeRarity rarity)
    {
        return upgradesByRarity.TryGetValue(rarity, out var upgrades) ? new List<UpgradeConfig>(upgrades) : new List<UpgradeConfig>();
    }

    public List<UpgradeConfig> GetAvailableUpgrades(UpgradeContext context)
    {
        var result = new List<UpgradeConfig>(enabledUpgrades.Count);
        for (int i = 0; i < enabledUpgrades.Count; i++)
        {
            var upgrade = enabledUpgrades[i];
            if (!upgrade.IsHidden && upgrade.CanUnlock(context))
                result.Add(upgrade);
        }
        return result;
    }

    public List<UpgradeConfig> GenerateUpgradeSelection(UpgradeContext context, int count = -1)
    {
        if (count <= 0) count = defaultSelectionCount;
        var availableUpgrades = GetAvailableUpgrades(context);
        if (availableUpgrades.Count == 0) return new List<UpgradeConfig>();

        var selection = new List<UpgradeConfig>();
        var usedTypes = new HashSet<UpgradeType>();
        var weightedUpgrades = CreateWeightedList(availableUpgrades, context);

        for (int i = 0; i < count && weightedUpgrades.Count > 0; i++)
        {
            var selected = SelectWeightedRandom(weightedUpgrades);
            selection.Add(selected.upgrade);
            weightedUpgrades.RemoveAll(wu => wu.upgrade == selected.upgrade);
            if (!allowDuplicateTypes)
            {
                usedTypes.Add(selected.upgrade.Type);
                weightedUpgrades.RemoveAll(wu => usedTypes.Contains(wu.upgrade.Type));
            }
        }
        return selection;
    }

    private List<WeightedUpgrade> CreateWeightedList(List<UpgradeConfig> upgrades, UpgradeContext context)
    {
        var weightedList = new List<WeightedUpgrade>(upgrades.Count);
        for (int i = 0; i < upgrades.Count; i++)
        {
            var upgrade = upgrades[i];
            var baseWeight = upgrade.GetSelectionWeightAtPlayerLevel(context.PlayerLevel);
            var rarityMultiplier = GetRarityMultiplier(upgrade.Rarity);
            weightedList.Add(new WeightedUpgrade { upgrade = upgrade, weight = baseWeight * rarityMultiplier });
        }
        return weightedList;
    }

    private float GetRarityMultiplier(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Uncommon: return 1f / rarityBonusMultiplier;
            case UpgradeRarity.Rare: return 1f / (rarityBonusMultiplier * rarityBonusMultiplier);
            case UpgradeRarity.Epic: return 1f / (rarityBonusMultiplier * rarityBonusMultiplier * rarityBonusMultiplier);
            case UpgradeRarity.Legendary: return 1f / (rarityBonusMultiplier * rarityBonusMultiplier * rarityBonusMultiplier * rarityBonusMultiplier);
            default: return 1f;
        }
    }

    private WeightedUpgrade SelectWeightedRandom(List<WeightedUpgrade> weightedUpgrades)
    {
        float totalWeight = 0f;
        for (int i = 0; i < weightedUpgrades.Count; i++)
            totalWeight += weightedUpgrades[i].weight;
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        for (int i = 0; i < weightedUpgrades.Count; i++)
        {
            currentWeight += weightedUpgrades[i].weight;
            if (randomValue <= currentWeight)
                return weightedUpgrades[i];
        }
        return weightedUpgrades[weightedUpgrades.Count - 1];
    }

    private struct WeightedUpgrade
    {
        public UpgradeConfig upgrade;
        public float weight;
    }
}
