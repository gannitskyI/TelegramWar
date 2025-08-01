using UnityEngine;

[CreateAssetMenu(fileName = "WaveConfiguration", menuName = "Game/Wave Configuration")]
public class WaveConfiguration : ScriptableObject
{
    [Header("Base Wave Settings")]
    public int baseEnemyCount = 5;
    public float enemyCountGrowthRate = 0.15f;
    public float waveDuration = 30f;
    public float spawnInterval = 1.5f;
    public float spawnIntervalDecreaseRate = 0.02f;
    public float minSpawnInterval = 0.3f;

    [Header("Difficulty Scaling")]
    public AnimationCurve difficultyProgression = AnimationCurve.Linear(0, 1, 100, 10);
    public float baseDifficultyPoints = 10f;
    public float difficultyGrowthRate = 1.2f;

    [Header("Tier Distribution Per Wave")]
    public TierDistribution[] tierDistributions = new TierDistribution[]
    {
        new TierDistribution { waveRange = new Vector2Int(1, 5), weights = new TierWeights(1f, 0f, 0f, 0f, 0f) },
        new TierDistribution { waveRange = new Vector2Int(6, 10), weights = new TierWeights(0.7f, 0.3f, 0f, 0f, 0f) },
        new TierDistribution { waveRange = new Vector2Int(11, 20), weights = new TierWeights(0.5f, 0.4f, 0.1f, 0f, 0f) },
        new TierDistribution { waveRange = new Vector2Int(21, 30), weights = new TierWeights(0.3f, 0.4f, 0.25f, 0.05f, 0f) },
        new TierDistribution { waveRange = new Vector2Int(31, 50), weights = new TierWeights(0.2f, 0.3f, 0.35f, 0.15f, 0f) },
        new TierDistribution { waveRange = new Vector2Int(51, 100), weights = new TierWeights(0.1f, 0.2f, 0.4f, 0.25f, 0.05f) },
        new TierDistribution { waveRange = new Vector2Int(101, int.MaxValue), weights = new TierWeights(0.05f, 0.15f, 0.35f, 0.35f, 0.1f) }
    };

    [System.Serializable]
    public struct TierDistribution
    {
        public Vector2Int waveRange;
        public TierWeights weights;
    }

    public int CalculateEnemyCount(int waveNumber)
    {
        float multiplier = difficultyProgression.Evaluate(waveNumber);
        return Mathf.RoundToInt(baseEnemyCount * (1 + enemyCountGrowthRate * (waveNumber - 1)) * multiplier);
    }

    public float CalculateSpawnInterval(int waveNumber)
    {
        float reduction = spawnIntervalDecreaseRate * (waveNumber - 1);
        return Mathf.Max(minSpawnInterval, spawnInterval - reduction);
    }

    public float CalculateDifficultyPoints(int waveNumber)
    {
        return baseDifficultyPoints * Mathf.Pow(difficultyGrowthRate, waveNumber - 1);
    }

    public TierWeights GetTierWeights(int waveNumber)
    {
        foreach (var distribution in tierDistributions)
        {
            if (waveNumber >= distribution.waveRange.x && waveNumber <= distribution.waveRange.y)
            {
                return distribution.weights;
            }
        }
        return tierDistributions[^1].weights;
    }

    [ContextMenu("Validate Configuration")]
    private void ValidateConfiguration()
    {
        Debug.Log($"Wave 1: {CalculateEnemyCount(1)} enemies, {CalculateSpawnInterval(1):F2}s interval");
        Debug.Log($"Wave 10: {CalculateEnemyCount(10)} enemies, {CalculateSpawnInterval(10):F2}s interval");
        Debug.Log($"Wave 25: {CalculateEnemyCount(25)} enemies, {CalculateSpawnInterval(25):F2}s interval");
        Debug.Log($"Wave 50: {CalculateEnemyCount(50)} enemies, {CalculateSpawnInterval(50):F2}s interval");
        Debug.Log($"Wave 100: {CalculateEnemyCount(100)} enemies, {CalculateSpawnInterval(100):F2}s interval");
    }
}