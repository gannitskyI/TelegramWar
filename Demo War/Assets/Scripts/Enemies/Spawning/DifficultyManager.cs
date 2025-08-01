using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyManager", menuName = "Game/Difficulty Manager")]
public class DifficultyManager : ScriptableObject
{
    [Header("Global Difficulty")]
    public AnimationCurve globalDifficultyMultiplier = AnimationCurve.Linear(0, 1, 100, 3);
    public float playerPowerCompensation = 0.1f;

    [Header("Adaptive Difficulty")]
    public bool enableAdaptiveDifficulty = true;
    public float adaptationRate = 0.05f;
    public float minDifficultyMultiplier = 0.5f;
    public float maxDifficultyMultiplier = 2f;

    [Header("Performance Metrics")]
    public float targetPlayerSurvivalRate = 0.7f;
    public float targetAverageWaveTime = 25f;
    public int metricsWindowSize = 10;

    private float currentDifficultyMultiplier = 1f;
    private readonly float[] recentWaveTimes = new float[10];
    private readonly bool[] recentSurvivalResults = new bool[10];
    private int metricsIndex = 0;

    public float CalculateFinalDifficulty(int waveNumber, float baseDifficulty)
    {
        float globalMultiplier = globalDifficultyMultiplier.Evaluate(waveNumber);
        float adaptiveMultiplier = enableAdaptiveDifficulty ? currentDifficultyMultiplier : 1f;

        return baseDifficulty * globalMultiplier * adaptiveMultiplier;
    }

    public void RecordWaveCompletion(float waveTime, bool playerSurvived)
    {
        if (!enableAdaptiveDifficulty) return;

        recentWaveTimes[metricsIndex] = waveTime;
        recentSurvivalResults[metricsIndex] = playerSurvived;
        metricsIndex = (metricsIndex + 1) % metricsWindowSize;

        UpdateAdaptiveDifficulty();
    }

    private void UpdateAdaptiveDifficulty()
    {
        float averageWaveTime = CalculateAverageWaveTime();
        float survivalRate = CalculateSurvivalRate();

        float timeDeviation = (averageWaveTime - targetAverageWaveTime) / targetAverageWaveTime;
        float survivalDeviation = survivalRate - targetPlayerSurvivalRate;

        float adjustment = 0f;

        if (survivalDeviation > 0.1f && timeDeviation < -0.2f)
        {
            adjustment = adaptationRate;
        }
        else if (survivalDeviation < -0.1f || timeDeviation > 0.3f)
        {
            adjustment = -adaptationRate;
        }

        currentDifficultyMultiplier = Mathf.Clamp(
            currentDifficultyMultiplier + adjustment,
            minDifficultyMultiplier,
            maxDifficultyMultiplier
        );

        Debug.Log($"Difficulty adjusted: {currentDifficultyMultiplier:F2} (Survival: {survivalRate:P1}, Avg Time: {averageWaveTime:F1}s)");
    }

    private float CalculateAverageWaveTime()
    {
        float total = 0f;
        int count = 0;

        for (int i = 0; i < metricsWindowSize; i++)
        {
            if (recentWaveTimes[i] > 0)
            {
                total += recentWaveTimes[i];
                count++;
            }
        }

        return count > 0 ? total / count : targetAverageWaveTime;
    }

    private float CalculateSurvivalRate()
    {
        int survivals = 0;
        int total = 0;

        for (int i = 0; i < metricsWindowSize; i++)
        {
            if (recentWaveTimes[i] > 0)
            {
                if (recentSurvivalResults[i]) survivals++;
                total++;
            }
        }

        return total > 0 ? (float)survivals / total : targetPlayerSurvivalRate;
    }

    public void ResetDifficulty()
    {
        currentDifficultyMultiplier = 1f;
        System.Array.Clear(recentWaveTimes, 0, metricsWindowSize);
        System.Array.Clear(recentSurvivalResults, 0, metricsWindowSize);
        metricsIndex = 0;
    }

    public string GetDifficultyStatus()
    {
        return $"Current Multiplier: {currentDifficultyMultiplier:F2}\n" +
               $"Survival Rate: {CalculateSurvivalRate():P1}\n" +
               $"Avg Wave Time: {CalculateAverageWaveTime():F1}s";
    }

    [ContextMenu("Test Difficulty Calculation")]
    private void TestDifficultyCalculation()
    {
        for (int wave = 1; wave <= 50; wave += 5)
        {
            float baseDifficulty = 10f;
            float finalDifficulty = CalculateFinalDifficulty(wave, baseDifficulty);
            Debug.Log($"Wave {wave}: Base={baseDifficulty}, Final={finalDifficulty:F1}");
        }
    }
}