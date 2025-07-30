using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaveGenerator
{
    private readonly WaveConfiguration waveConfig;
    private readonly EnemyDatabase enemyDatabase;
    private readonly System.Random random;

    public WaveGenerator(WaveConfiguration config, EnemyDatabase database)
    {
        waveConfig = config;
        enemyDatabase = database;
        random = new System.Random();

        enemyDatabase.Initialize();
    }

    public WaveData GenerateWave(int waveNumber)
    {
        var waveData = new WaveData
        {
            waveNumber = waveNumber,
            enemyCount = waveConfig.CalculateEnemyCount(waveNumber),
            spawnInterval = waveConfig.CalculateSpawnInterval(waveNumber),
            difficultyPoints = waveConfig.CalculateDifficultyPoints(waveNumber),
            duration = waveConfig.waveDuration
        };

        var tierWeights = waveConfig.GetTierWeights(waveNumber);
        waveData.enemyComposition = GenerateEnemyComposition(waveData.enemyCount, tierWeights, waveData.difficultyPoints);

        return waveData;
    }

    private List<EnemySpawnData> GenerateEnemyComposition(int totalEnemies, TierWeights weights, float difficultyBudget)
    {
        var composition = new List<EnemySpawnData>();
        var usedDifficulty = 0f;

        var tierCounts = DistributeEnemiesByTier(totalEnemies, weights);

        foreach (var tierCount in tierCounts)
        {
            var tierEnemies = enemyDatabase.GetEnemiesByTier(tierCount.Key);
            if (tierEnemies.Count == 0) continue;

            for (int i = 0; i < tierCount.Value; i++)
            {
                var remainingBudget = difficultyBudget - usedDifficulty;
                var enemy = SelectEnemyWithinBudget(tierEnemies, remainingBudget);

                if (enemy != null)
                {
                    composition.Add(new EnemySpawnData
                    {
                        enemyConfig = enemy,
                        spawnDelay = CalculateSpawnDelay(composition.Count, totalEnemies)
                    });

                    usedDifficulty += enemy.difficultyValue;
                }
            }
        }

        return composition.OrderBy(x => x.spawnDelay).ToList();
    }

    private Dictionary<EnemyTier, int> DistributeEnemiesByTier(int totalEnemies, TierWeights weights)
    {
        var distribution = new Dictionary<EnemyTier, int>();
        var totalWeight = weights.tier1Weight + weights.tier2Weight + weights.tier3Weight + weights.tier4Weight + weights.tier5Weight;

        if (totalWeight <= 0) return distribution;

        var tiers = new[] { EnemyTier.Tier1, EnemyTier.Tier2, EnemyTier.Tier3, EnemyTier.Tier4, EnemyTier.Tier5 };
        var remainingEnemies = totalEnemies;

        foreach (var tier in tiers)
        {
            var weight = weights.GetWeight(tier);
            if (weight > 0)
            {
                var count = Mathf.RoundToInt((weight / totalWeight) * totalEnemies);
                count = Mathf.Min(count, remainingEnemies);
                distribution[tier] = count;
                remainingEnemies -= count;
            }
            else
            {
                distribution[tier] = 0;
            }
        }

        while (remainingEnemies > 0)
        {
            var randomTier = tiers[random.Next(tiers.Length)];
            if (weights.GetWeight(randomTier) > 0)
            {
                distribution[randomTier]++;
                remainingEnemies--;
            }
        }

        return distribution;
    }

    private EnemyConfig SelectEnemyWithinBudget(List<EnemyConfig> availableEnemies, float budget)
    {
        var affordableEnemies = availableEnemies.Where(e => e.difficultyValue <= budget).ToList();

        if (affordableEnemies.Count == 0)
        {
            return availableEnemies.Count > 0 ? availableEnemies[random.Next(availableEnemies.Count)] : null;
        }

        var weights = new float[affordableEnemies.Count];
        for (int i = 0; i < affordableEnemies.Count; i++)
        {
            weights[i] = 1f / (affordableEnemies[i].difficultyValue + 1f);
        }

        return affordableEnemies[SelectWeightedRandom(weights)];
    }

    private int SelectWeightedRandom(float[] weights)
    {
        var totalWeight = weights.Sum();
        var randomValue = (float)random.NextDouble() * totalWeight;

        var currentWeight = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
            {
                return i;
            }
        }

        return weights.Length - 1;
    }

    private float CalculateSpawnDelay(int enemyIndex, int totalEnemies)
    {
        var baseDelay = (float)enemyIndex / totalEnemies;
        var randomVariation = ((float)random.NextDouble() - 0.5f) * 0.2f;
        return Mathf.Max(0f, baseDelay + randomVariation);
    }
}

[System.Serializable]
public class WaveData
{
    public int waveNumber;
    public int enemyCount;
    public float spawnInterval;
    public float difficultyPoints;
    public float duration;
    public List<EnemySpawnData> enemyComposition;

    public string GetWaveInfo()
    {
        var info = $"Wave {waveNumber}: {enemyCount} enemies, {difficultyPoints:F1} difficulty\n";

        var tierCounts = new Dictionary<EnemyTier, int>();
        foreach (var spawn in enemyComposition)
        {
            var tier = spawn.enemyConfig.tier;
            tierCounts[tier] = tierCounts.GetValueOrDefault(tier, 0) + 1;
        }

        foreach (var tierCount in tierCounts.OrderBy(x => x.Key))
        {
            info += $"  Tier {(int)tierCount.Key}: {tierCount.Value}\n";
        }

        return info;
    }
}

[System.Serializable]
public class EnemySpawnData
{
    public EnemyConfig enemyConfig;
    public float spawnDelay;
}