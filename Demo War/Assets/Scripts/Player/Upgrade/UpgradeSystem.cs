using System;
using System.Collections.Generic;
using UnityEngine;

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
    CriticalDamage
}

public class UpgradeSystem : IInitializable
{
    public int InitializationOrder => 25;

    private readonly Dictionary<string, Upgrade> availableUpgrades = new Dictionary<string, Upgrade>();
    private readonly Dictionary<string, Upgrade> playerUpgrades = new Dictionary<string, Upgrade>();

    public event Action<List<Upgrade>> OnUpgradeOptionsGenerated;
    public event Action<Upgrade> OnUpgradeSelected;

    public System.Collections.IEnumerator Initialize()
    {
        InitializeUpgrades();
        yield return null;
    }

    private void InitializeUpgrades()
    {
        var upgrades = new[]
        {
            new Upgrade("damage_boost", "Damage Boost", "+20% damage", UpgradeType.Damage, 0.2f, 5),
            new Upgrade("mega_damage", "Mega Damage", "+50% damage", UpgradeType.Damage, 0.5f, 3),
            new Upgrade("attack_speed", "Attack Speed", "+25% attack speed", UpgradeType.AttackSpeed, 0.25f, 4),
            new Upgrade("rapid_fire", "Rapid Fire", "+60% attack speed", UpgradeType.AttackSpeed, 0.6f, 2),
            new Upgrade("range_boost", "Range Boost", "+30% attack range", UpgradeType.AttackRange, 0.3f, 3),
            new Upgrade("sniper_range", "Sniper Range", "+80% attack range", UpgradeType.AttackRange, 0.8f, 2),
            new Upgrade("move_speed", "Move Speed", "+25% movement speed", UpgradeType.MoveSpeed, 0.25f, 4),
            new Upgrade("swift_feet", "Swift Feet", "+50% movement speed", UpgradeType.MoveSpeed, 0.5f, 2),
            new Upgrade("health_boost", "Health Boost", "+30% max health", UpgradeType.Health, 0.3f, 3),
            new Upgrade("tank_health", "Tank Health", "+70% max health", UpgradeType.Health, 0.7f, 2),
            new Upgrade("health_regen", "Health Regeneration", "Regenerate 2 HP/sec", UpgradeType.HealthRegen, 2f, 3),
            new Upgrade("exp_multiplier", "Experience Boost", "+40% experience gain", UpgradeType.ExperienceMultiplier, 0.4f, 3),
            new Upgrade("crit_chance", "Critical Chance", "+15% critical hit chance", UpgradeType.CriticalChance, 0.15f, 4),
            new Upgrade("crit_damage", "Critical Damage", "+50% critical hit damage", UpgradeType.CriticalDamage, 0.5f, 3)
        };

        foreach (var upgrade in upgrades)
        {
            availableUpgrades[upgrade.id] = upgrade;
        }
    }

    public List<Upgrade> GenerateUpgradeOptions(int count = 3)
    {
        var availableKeys = GetAvailableUpgradeKeys();
        var options = SelectRandomUpgrades(availableKeys, count);

        OnUpgradeOptionsGenerated?.Invoke(options);
        return options;
    }

    private List<string> GetAvailableUpgradeKeys()
    {
        var availableKeys = new List<string>();

        foreach (var kvp in availableUpgrades)
        {
            var upgrade = kvp.Value;

            if (playerUpgrades.TryGetValue(upgrade.id, out var existingUpgrade))
            {
                if (existingUpgrade.CanUpgrade)
                {
                    availableKeys.Add(upgrade.id);
                }
            }
            else
            {
                availableKeys.Add(upgrade.id);
            }
        }

        return availableKeys;
    }

    private List<Upgrade> SelectRandomUpgrades(List<string> availableKeys, int count)
    {
        var options = new List<Upgrade>();
        int actualCount = Mathf.Min(count, availableKeys.Count);

        for (int i = 0; i < actualCount; i++)
        {
            if (availableKeys.Count == 0) break;

            int randomIndex = UnityEngine.Random.Range(0, availableKeys.Count);
            string selectedKey = availableKeys[randomIndex];
            availableKeys.RemoveAt(randomIndex);

            var upgradeOption = CreateUpgradeOption(selectedKey);
            options.Add(upgradeOption);
        }

        return options;
    }

    private Upgrade CreateUpgradeOption(string upgradeId)
    {
        var baseUpgrade = availableUpgrades[upgradeId];
        var upgradeOption = new Upgrade(
            baseUpgrade.id,
            baseUpgrade.name,
            baseUpgrade.description,
            baseUpgrade.type,
            baseUpgrade.value,
            baseUpgrade.maxLevel
        );

        if (playerUpgrades.TryGetValue(upgradeId, out var existingUpgrade))
        {
            upgradeOption.currentLevel = existingUpgrade.currentLevel;
        }

        return upgradeOption;
    }

    public void SelectUpgrade(Upgrade selectedUpgrade)
    {
        if (selectedUpgrade == null)
        {
            Debug.LogError("Selected upgrade is null!");
            return;
        }

        UpdatePlayerUpgrade(selectedUpgrade);
        ApplyUpgradeToPlayer(selectedUpgrade);
        OnUpgradeSelected?.Invoke(selectedUpgrade);
    }

    private void UpdatePlayerUpgrade(Upgrade selectedUpgrade)
    {
        if (playerUpgrades.TryGetValue(selectedUpgrade.id, out var existingUpgrade))
        {
            existingUpgrade.currentLevel++;
        }
        else
        {
            var newUpgrade = new Upgrade(
                selectedUpgrade.id,
                selectedUpgrade.name,
                selectedUpgrade.description,
                selectedUpgrade.type,
                selectedUpgrade.value,
                selectedUpgrade.maxLevel
            );
            newUpgrade.currentLevel = 1;
            playerUpgrades[selectedUpgrade.id] = newUpgrade;
        }
    }

    private void ApplyUpgradeToPlayer(Upgrade upgrade)
    {
        var player = PlayerFinder.FindPlayer();

        if (player == null)
        {
            Debug.LogError("Player not found! Cannot apply upgrade.");
            return;
        }

        var upgradeApplier = new UpgradeApplier(player);
        upgradeApplier.ApplyUpgrade(upgrade);

        WebGLHelper.TriggerHapticFeedback("medium");
    }

    public float GetUpgradeMultiplier(UpgradeType type)
    {
        float totalMultiplier = 1f;

        foreach (var upgrade in playerUpgrades.Values)
        {
            if (upgrade.type == type)
            {
                totalMultiplier += upgrade.value * upgrade.currentLevel;
            }
        }

        return totalMultiplier;
    }

    public Dictionary<string, Upgrade> GetPlayerUpgrades() =>
        new Dictionary<string, Upgrade>(playerUpgrades);

    public bool HasUpgrade(string upgradeId) =>
        playerUpgrades.ContainsKey(upgradeId);

    public int GetUpgradeLevel(string upgradeId) =>
        playerUpgrades.TryGetValue(upgradeId, out var upgrade) ? upgrade.currentLevel : 0;

    public void ResetUpgrades()
    {
        playerUpgrades.Clear();
    }

    public string GetUpgradesSummary()
    {
        if (playerUpgrades.Count == 0)
            return "No upgrades acquired";

        var summary = "Current Upgrades:\n";
        foreach (var upgrade in playerUpgrades.Values)
        {
            summary += $"- {upgrade.name} Lv.{upgrade.currentLevel}\n";
        }

        return summary.TrimEnd('\n');
    }

    public void Cleanup()
    {
        ResetUpgrades();
        availableUpgrades.Clear();
        OnUpgradeOptionsGenerated = null;
        OnUpgradeSelected = null;
    }
}

public static class PlayerFinder
{
    public static GameObject FindPlayer()
    {
        if (ServiceLocator.TryGet<GameObject>(out var player) && player != null)
            return player;

        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            return player;

        var playerHealth = UnityEngine.Object.FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
            return playerHealth.gameObject;

        var playerCombat = UnityEngine.Object.FindObjectOfType<PlayerCombat>();
        if (playerCombat != null)
            return playerCombat.gameObject;

        var playerMovement = UnityEngine.Object.FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
            return playerMovement.gameObject;

        return null;
    }
}

public class UpgradeApplier
{
    private readonly GameObject player;
    private readonly PlayerCombat combat;
    private readonly PlayerMovement movement;
    private readonly PlayerHealth health;

    public UpgradeApplier(GameObject player)
    {
        this.player = player;
        combat = player.GetComponent<PlayerCombat>();
        movement = player.GetComponent<PlayerMovement>();
        health = player.GetComponent<PlayerHealth>();
    }

    public void ApplyUpgrade(Upgrade upgrade)
    {
        switch (upgrade.type)
        {
            case UpgradeType.Damage:
                ApplyDamageUpgrade(upgrade.value);
                break;
            case UpgradeType.AttackSpeed:
                ApplyAttackSpeedUpgrade(upgrade.value);
                break;
            case UpgradeType.AttackRange:
                ApplyRangeUpgrade(upgrade.value);
                break;
            case UpgradeType.MoveSpeed:
                ApplyMoveSpeedUpgrade(upgrade.value);
                break;
            case UpgradeType.Health:
                ApplyHealthUpgrade(upgrade.value);
                break;
            case UpgradeType.HealthRegen:
                ApplyHealthRegenUpgrade(upgrade.value);
                break;
            case UpgradeType.ExperienceMultiplier:
                ApplyExperienceUpgrade(upgrade.value);
                break;
            case UpgradeType.CriticalChance:
            case UpgradeType.CriticalDamage:
                ApplyCriticalUpgrade(upgrade);
                break;
        }
    }

    private void ApplyDamageUpgrade(float value)
    {
        combat?.UpgradeDamage(1f + value);
    }

    private void ApplyAttackSpeedUpgrade(float value)
    {
        combat?.UpgradeAttackSpeed(1f + value);
    }

    private void ApplyRangeUpgrade(float value)
    {
        combat?.UpgradeRange(1f + value);
    }

    private void ApplyMoveSpeedUpgrade(float value)
    {
        movement?.SetMoveSpeed(movement.GetMoveSpeed() * (1f + value));
    }

    private void ApplyHealthUpgrade(float value)
    {
        if (health != null)
        {
            var newMaxHealth = 100f * (1f + value);
            health.Heal(newMaxHealth - 100f);
        }
    }

    private void ApplyHealthRegenUpgrade(float value)
    {
    }

    private void ApplyExperienceUpgrade(float value)
    {
    }

    private void ApplyCriticalUpgrade(Upgrade upgrade)
    {
    }
}