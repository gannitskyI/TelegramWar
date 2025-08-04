using System.Collections.Generic;
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
        float usedDifficulty = 0f;

        var tierCounts = DistributeEnemiesByTier(totalEnemies, weights);

        foreach (var tierCount in tierCounts)
        {
            var tierEnemies = enemyDatabase.GetEnemiesByTier(tierCount.Key);
            if (tierEnemies.Count == 0) continue;
            for (int i = 0; i < tierCount.Value; i++)
            {
                float remainingBudget = difficultyBudget - usedDifficulty;
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
        composition.Sort((a, b) => a.spawnDelay.CompareTo(b.spawnDelay));
        return composition;
    }

    private Dictionary<EnemyTier, int> DistributeEnemiesByTier(int totalEnemies, TierWeights weights)
    {
        var distribution = new Dictionary<EnemyTier, int>();
        float totalWeight = weights.tier1Weight + weights.tier2Weight + weights.tier3Weight + weights.tier4Weight + weights.tier5Weight;
        if (totalWeight <= 0) return distribution;
        var tiers = new[] { EnemyTier.Tier1, EnemyTier.Tier2, EnemyTier.Tier3, EnemyTier.Tier4, EnemyTier.Tier5 };
        int remainingEnemies = totalEnemies;

        foreach (var tier in tiers)
        {
            float weight = weights.GetWeight(tier);
            if (weight > 0)
            {
                int count = Mathf.Min(Mathf.RoundToInt((weight / totalWeight) * totalEnemies), remainingEnemies);
                distribution[tier] = count;
                remainingEnemies -= count;
            }
            else distribution[tier] = 0;
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
        var affordableEnemies = new List<EnemyConfig>();
        for (int i = 0; i < availableEnemies.Count; i++)
        {
            if (availableEnemies[i].difficultyValue <= budget)
                affordableEnemies.Add(availableEnemies[i]);
        }
        if (affordableEnemies.Count == 0)
        {
            return availableEnemies.Count > 0 ? availableEnemies[random.Next(availableEnemies.Count)] : null;
        }

        var weights = new float[affordableEnemies.Count];
        for (int i = 0; i < affordableEnemies.Count; i++)
            weights[i] = 1f / (affordableEnemies[i].difficultyValue + 1f);

        int index = SelectWeightedRandom(weights);
        return affordableEnemies[index];
    }

    private int SelectWeightedRandom(float[] weights)
    {
        float totalWeight = 0f;
        for (int i = 0; i < weights.Length; i++)
            totalWeight += weights[i];
        float randomValue = (float)random.NextDouble() * totalWeight;
        float currentWeight = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
                return i;
        }
        return weights.Length - 1;
    }

    private float CalculateSpawnDelay(int enemyIndex, int totalEnemies)
    {
        float baseDelay = (float)enemyIndex / totalEnemies;
        float randomVariation = ((float)random.NextDouble() - 0.5f) * 0.2f;
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
}

[System.Serializable]
public class EnemySpawnData
{
    public EnemyConfig enemyConfig;
    public float spawnDelay;
}
