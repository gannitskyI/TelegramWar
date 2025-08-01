using UnityEngine;
using System;
using System.Collections.Generic;

[Flags]
public enum UpgradeCategory
{
    None = 0,
    Combat = 1 << 0,
    Movement = 1 << 1,
    Defense = 1 << 2,
    Experience = 1 << 3,
    Special = 1 << 4,
    Passive = 1 << 5
}

public enum UpgradeRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

public enum UpgradeScalingType
{
    Linear,
    Exponential,
    Logarithmic,
    Custom
}
public enum UpgradeType
{
    Damage,
    AttackSpeed,
    AttackRange,
    MoveSpeed,
    Health,
    HealthRegen,
    ExperienceMultiplier,
    CriticalChance,
    CriticalDamage,
    MultiShot,
    PiercingShots,
    ExplosiveRounds,
    Shield,
    Armor,
    Special
}

[CreateAssetMenu(fileName = "UpgradeConfig", menuName = "Game/Upgrade Configuration")]
public class UpgradeConfig : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField] private string upgradeId = "";
    [SerializeField] private string displayName = "Upgrade";
    [SerializeField, TextArea(2, 4)] private string description = "";
    [SerializeField] private Sprite icon;

    [Header("Classification")]
    [SerializeField] private UpgradeType upgradeType;
    [SerializeField] private UpgradeCategory category;
    [SerializeField] private UpgradeRarity rarity = UpgradeRarity.Common;

    [Header("Availability")]
    [SerializeField] private int minLevel = 1;
    [SerializeField] private int maxLevel = 5;
    [SerializeField] private bool isEnabled = true;
    [SerializeField] private List<string> requiredUpgrades = new List<string>();
    [SerializeField] private List<string> conflictingUpgrades = new List<string>();

    [Header("Values and Scaling")]
    [SerializeField] private float baseValue = 1f;
    [SerializeField] private UpgradeScalingType scalingType = UpgradeScalingType.Linear;
    [SerializeField] private float scalingFactor = 1f;
    [SerializeField] private AnimationCurve customScalingCurve = AnimationCurve.Linear(1, 1, 5, 5);

    [Header("Selection Weight")]
    [SerializeField, Range(0.1f, 10f)] private float selectionWeight = 1f;
    [SerializeField] private AnimationCurve levelWeightModifier = AnimationCurve.Constant(1, 50, 1);

    [Header("Visual")]
    [SerializeField] private Color rarityColor = Color.white;
    [SerializeField] private Color categoryColor = Color.gray;
    [SerializeField] private string flavorText = "";

    [Header("Advanced")]
    [SerializeField] private bool stacksWithSameType = true;
    [SerializeField] private bool isHidden = false;
    [SerializeField] private int maxStackCount = -1;
    [SerializeField] private List<UpgradeCondition> unlockConditions = new List<UpgradeCondition>();

    public string UpgradeId => string.IsNullOrEmpty(upgradeId) ? name : upgradeId;
    public string DisplayName => displayName;
    public string Description => description;
    public string FlavorText => flavorText;
    public Sprite Icon => icon;
    public UpgradeType Type => upgradeType;
    public UpgradeCategory Category => category;
    public UpgradeRarity Rarity => rarity;
    public int MinLevel => minLevel;
    public int MaxLevel => maxLevel;
    public bool IsEnabled => isEnabled;
    public bool IsHidden => isHidden;
    public bool StacksWithSameType => stacksWithSameType;
    public int MaxStackCount => maxStackCount;
    public float BaseValue => baseValue;
    public float ScalingFactor => scalingFactor;
    public float SelectionWeight => selectionWeight;
    public Color RarityColor => rarityColor;
    public Color CategoryColor => categoryColor;
    public IReadOnlyList<string> RequiredUpgrades => requiredUpgrades;
    public IReadOnlyList<string> ConflictingUpgrades => conflictingUpgrades;
    public IReadOnlyList<UpgradeCondition> UnlockConditions => unlockConditions;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(upgradeId))
            upgradeId = name.ToLower().Replace(" ", "_");

        if (string.IsNullOrEmpty(displayName))
            displayName = name;

        minLevel = Mathf.Max(1, minLevel);
        maxLevel = Mathf.Max(minLevel, maxLevel);
        selectionWeight = Mathf.Max(0.1f, selectionWeight);

        if (rarityColor == Color.clear)
            rarityColor = GetDefaultRarityColor();
    }

    public float CalculateValueAtLevel(int level)
    {
        if (level <= 0) return 0f;

        return scalingType switch
        {
            UpgradeScalingType.Linear => baseValue + (scalingFactor * (level - 1)),
            UpgradeScalingType.Exponential => baseValue * Mathf.Pow(scalingFactor, level - 1),
            UpgradeScalingType.Logarithmic => baseValue * (1 + scalingFactor * Mathf.Log(level)),
            UpgradeScalingType.Custom => baseValue * customScalingCurve.Evaluate(level),
            _ => baseValue
        };
    }

    public float GetSelectionWeightAtPlayerLevel(int playerLevel)
    {
        return selectionWeight * levelWeightModifier.Evaluate(playerLevel);
    }

    public bool CanUnlock(UpgradeContext context)
    {
        if (!isEnabled || context.PlayerLevel < minLevel)
            return false;

        foreach (var requiredId in requiredUpgrades)
        {
            if (!context.HasUpgrade(requiredId))
                return false;
        }

        foreach (var conflictingId in conflictingUpgrades)
        {
            if (context.HasUpgrade(conflictingId))
                return false;
        }

        foreach (var condition in unlockConditions)
        {
            if (!condition.IsMet(context))
                return false;
        }

        return true;
    }

    public bool CanLevelUp(int currentLevel)
    {
        return currentLevel < maxLevel;
    }
    public string GetFormattedDescriptionWithLevel(int currentLevel, int nextLevel)
    {
        if (currentLevel <= 0)
        {
            return GetFormattedDescription(nextLevel);
        }

        var currentValue = CalculateValueAtLevel(currentLevel);
        var nextValue = CalculateValueAtLevel(nextLevel);

        var baseDesc = description;
        baseDesc = baseDesc.Replace("{value}", FormatValue(nextValue));
        baseDesc = baseDesc.Replace("{level}", nextLevel.ToString());
        baseDesc = baseDesc.Replace("{max_level}", maxLevel.ToString());

        var levelInfo = $"Level {currentLevel} ? {nextLevel}";
        var valueChange = $"{FormatValue(currentValue)} ? {FormatValue(nextValue)}";

        return $"{baseDesc}\n{levelInfo} ({valueChange})";
    }
    public string GetFormattedDescription(int level = 1)
    {
        var value = CalculateValueAtLevel(level);
        var formattedDesc = description;

        formattedDesc = formattedDesc.Replace("{value}", FormatValue(value));
        formattedDesc = formattedDesc.Replace("{level}", level.ToString());
        formattedDesc = formattedDesc.Replace("{max_level}", maxLevel.ToString());

        return formattedDesc;
    }

    private string FormatValue(float value)
    {
        return upgradeType switch
        {
            UpgradeType.Damage or UpgradeType.AttackSpeed or UpgradeType.AttackRange
            or UpgradeType.MoveSpeed or UpgradeType.ExperienceMultiplier
            or UpgradeType.CriticalChance or UpgradeType.CriticalDamage => $"+{value * 100:F0}%",
            UpgradeType.Health => $"+{value:F0}",
            UpgradeType.HealthRegen => $"{value:F1}/sec",
            _ => value.ToString("F2")
        };
    }

    private Color GetDefaultRarityColor()
    {
        return rarity switch
        {
            UpgradeRarity.Common => Color.white,
            UpgradeRarity.Uncommon => Color.green,
            UpgradeRarity.Rare => Color.blue,
            UpgradeRarity.Epic => new Color(0.8f, 0.3f, 1f),
            UpgradeRarity.Legendary => new Color(1f, 0.8f, 0f),
            _ => Color.white
        };
    }

    [ContextMenu("Auto-Fill Values")]
    private void AutoFillValues()
    {
        if (string.IsNullOrEmpty(upgradeId))
            upgradeId = name.ToLower().Replace(" ", "_");

        if (string.IsNullOrEmpty(displayName))
            displayName = name;

        rarityColor = GetDefaultRarityColor();

        categoryColor = category switch
        {
            UpgradeCategory.Combat => Color.red,
            UpgradeCategory.Movement => Color.cyan,
            UpgradeCategory.Defense => Color.blue,
            UpgradeCategory.Experience => Color.yellow,
            UpgradeCategory.Special => Color.magenta,
            UpgradeCategory.Passive => Color.green,
            _ => Color.gray
        };
    }

    [ContextMenu("Test Scaling Values")]
    private void TestScalingValues()
    {
        Debug.Log($"Upgrade: {displayName}");
        for (int i = 1; i <= maxLevel; i++)
        {
            var value = CalculateValueAtLevel(i);
            Debug.Log($"Level {i}: {FormatValue(value)}");
        }
    }
}

[System.Serializable]
public class UpgradeCondition
{
    [SerializeField] private UpgradeConditionType conditionType;
    [SerializeField] private string targetValue;
    [SerializeField] private float numericValue;
    [SerializeField] private ComparisonOperator comparisonOperator;

    public bool IsMet(UpgradeContext context)
    {
        return conditionType switch
        {
            UpgradeConditionType.PlayerLevel => CompareValue(context.PlayerLevel, numericValue),
            UpgradeConditionType.HasUpgrade => context.HasUpgrade(targetValue),
            UpgradeConditionType.UpgradeLevel => CompareValue(context.GetUpgradeLevel(targetValue), numericValue),
            UpgradeConditionType.PlayTime => CompareValue(context.PlayTimeSeconds, numericValue),
            UpgradeConditionType.EnemiesKilled => CompareValue(context.EnemiesKilled, numericValue),
            UpgradeConditionType.CurrentWave => CompareValue(context.CurrentWave, numericValue),
            UpgradeConditionType.Custom => EvaluateCustomCondition(context),
            _ => true
        };
    }

    private bool CompareValue(float actual, float target)
    {
        return comparisonOperator switch
        {
            ComparisonOperator.Equal => Mathf.Approximately(actual, target),
            ComparisonOperator.NotEqual => !Mathf.Approximately(actual, target),
            ComparisonOperator.Greater => actual > target,
            ComparisonOperator.GreaterOrEqual => actual >= target,
            ComparisonOperator.Less => actual < target,
            ComparisonOperator.LessOrEqual => actual <= target,
            _ => true
        };
    }

    private bool EvaluateCustomCondition(UpgradeContext context)
    {
        return true;
    }
}

public enum UpgradeConditionType
{
    PlayerLevel,
    HasUpgrade,
    UpgradeLevel,
    PlayTime,
    EnemiesKilled,
    CurrentWave,
    Custom
}

public enum ComparisonOperator
{
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual
}

public class UpgradeContext
{
    public int PlayerLevel { get; set; }
    public float PlayTimeSeconds { get; set; }
    public int EnemiesKilled { get; set; }
    public int CurrentWave { get; set; }
    public Dictionary<string, int> OwnedUpgrades { get; set; } = new Dictionary<string, int>();

    public bool HasUpgrade(string upgradeId) => OwnedUpgrades.ContainsKey(upgradeId);
    public int GetUpgradeLevel(string upgradeId) => OwnedUpgrades.GetValueOrDefault(upgradeId, 0);
}