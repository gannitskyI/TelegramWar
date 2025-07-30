using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Game/Enemy Database")]
public class EnemyDatabase : ScriptableObject
{
    [Header("Enemy Configurations")]
    [Tooltip("Assign your enemy configs here. Each config should have a unique enemyId.")]
    public List<EnemyConfig> allEnemies = new List<EnemyConfig>();

    private Dictionary<EnemyTier, List<EnemyConfig>> enemiesByTier;
    private Dictionary<string, EnemyConfig> enemiesById;

    public void Initialize()
    {
        enemiesByTier = new Dictionary<EnemyTier, List<EnemyConfig>>();
        enemiesById = new Dictionary<string, EnemyConfig>();

        // Initialize all tier lists
        for (int tier = 1; tier <= 5; tier++)
        {
            enemiesByTier[(EnemyTier)tier] = new List<EnemyConfig>();
        }

        // Process all assigned enemy configs
        foreach (var enemy in allEnemies)
        {
            if (enemy == null)
            {
                Debug.LogWarning("Null enemy config found in database!");
                continue;
            }

            if (string.IsNullOrEmpty(enemy.enemyId))
            {
                Debug.LogError($"Enemy config '{enemy.name}' has empty enemyId! Please assign a unique ID.");
                continue;
            }

            if (enemiesById.ContainsKey(enemy.enemyId))
            {
                Debug.LogError($"Duplicate enemy ID '{enemy.enemyId}' found! Each enemy must have a unique ID.");
                continue;
            }

            enemiesByTier[enemy.tier].Add(enemy);
            enemiesById[enemy.enemyId] = enemy;
        }

        Debug.Log($"EnemyDatabase initialized with {enemiesById.Count} valid enemies");
    }

    public List<EnemyConfig> GetEnemiesByTier(EnemyTier tier)
    {
        if (enemiesByTier == null) Initialize();
        return enemiesByTier.TryGetValue(tier, out var enemies) ? enemies : new List<EnemyConfig>();
    }

    public EnemyConfig GetRandomEnemyByTier(EnemyTier tier)
    {
        var enemies = GetEnemiesByTier(tier);
        return enemies.Count > 0 ? enemies[Random.Range(0, enemies.Count)] : null;
    }

    public EnemyConfig GetEnemyById(string enemyId)
    {
        if (enemiesById == null) Initialize();
        return enemiesById.TryGetValue(enemyId, out var enemy) ? enemy : null;
    }

    public List<EnemyConfig> GetAvailableEnemiesForWave(int waveNumber, TierWeights weights)
    {
        var availableEnemies = new List<EnemyConfig>();

        for (int tier = 1; tier <= 5; tier++)
        {
            var tierEnum = (EnemyTier)tier;
            if (weights.GetWeight(tierEnum) > 0f)
            {
                var tierEnemies = GetEnemiesByTier(tierEnum)
                    .Where(e => e.minWaveNumber <= waveNumber)
                    .ToList();
                availableEnemies.AddRange(tierEnemies);
            }
        }

        return availableEnemies;
    }

    public string GetDatabaseInfo()
    {
        if (enemiesByTier == null) Initialize();

        var info = "Enemy Database:\n";

        for (int tier = 1; tier <= 5; tier++)
        {
            var tierEnum = (EnemyTier)tier;
            var count = enemiesByTier[tierEnum].Count;
            info += $"Tier {tier}: {count} enemies";

            if (count > 0)
            {
                var enemyNames = enemiesByTier[tierEnum].Select(e => e.enemyId).ToArray();
                info += $" ({string.Join(", ", enemyNames)})";
            }
            info += "\n";
        }

        if (allEnemies.Count == 0)
        {
            info += "\nWARNING: No enemy configs assigned! Please add EnemyConfig assets to the allEnemies list.";
        }

        return info;
    }

    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (allEnemies.Count == 0)
        {
            errors.Add("No enemy configurations assigned to database");
            return errors;
        }

        var usedIds = new HashSet<string>();

        foreach (var enemy in allEnemies)
        {
            if (enemy == null)
            {
                errors.Add("Null enemy config found in list");
                continue;
            }

            if (string.IsNullOrEmpty(enemy.enemyId))
            {
                errors.Add($"Enemy '{enemy.name}' has no enemyId assigned");
                continue;
            }

            if (usedIds.Contains(enemy.enemyId))
            {
                errors.Add($"Duplicate enemyId '{enemy.enemyId}' found");
            }
            else
            {
                usedIds.Add(enemy.enemyId);
            }

            if (enemy.difficultyValue <= 0)
            {
                errors.Add($"Enemy '{enemy.enemyId}' has invalid difficulty value: {enemy.difficultyValue}");
            }

            if (enemy.maxHealth <= 0)
            {
                errors.Add($"Enemy '{enemy.enemyId}' has invalid health: {enemy.maxHealth}");
            }

            if (enemy.minWaveNumber < 1)
            {
                errors.Add($"Enemy '{enemy.enemyId}' has invalid minWaveNumber: {enemy.minWaveNumber}");
            }
        }

        return errors;
    }

    [ContextMenu("Initialize Database")]
    private void EditorInitialize()
    {
        Initialize();
        Debug.Log(GetDatabaseInfo());
    }

    [ContextMenu("Validate All Enemies")]
    private void ValidateAllEnemies()
    {
        var errors = GetValidationErrors();

        if (errors.Count == 0)
        {
            Debug.Log("? All enemy configurations are valid!");
            Debug.Log(GetDatabaseInfo());
        }
        else
        {
            Debug.LogError($"Found {errors.Count} validation errors:");
            foreach (var error in errors)
            {
                Debug.LogError($"- {error}");
            }
        }
    }

    [ContextMenu("Show Missing Enemy IDs")]
    private void ShowMissingEnemyIds()
    {
        var missingIds = new List<string>();

        foreach (var enemy in allEnemies)
        {
            if (enemy != null && string.IsNullOrEmpty(enemy.enemyId))
            {
                missingIds.Add(enemy.name);
            }
        }

        if (missingIds.Count > 0)
        {
            Debug.LogWarning($"Enemies missing IDs: {string.Join(", ", missingIds)}");
        }
        else
        {
            Debug.Log("All enemies have IDs assigned");
        }
    }
}