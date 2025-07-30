using UnityEngine;

public enum EnemyTier
{
    Tier1 = 1,  // Basic enemies
    Tier2 = 2,  // Advanced enemies  
    Tier3 = 3,  // Elite enemies
    Tier4 = 4,  // Boss-tier enemies
    Tier5 = 5   // Legendary enemies
}

[System.Serializable]
public struct TierWeights
{
    [Range(0f, 1f)] public float tier1Weight;
    [Range(0f, 1f)] public float tier2Weight;
    [Range(0f, 1f)] public float tier3Weight;
    [Range(0f, 1f)] public float tier4Weight;
    [Range(0f, 1f)] public float tier5Weight;

    public TierWeights(float t1, float t2, float t3, float t4, float t5)
    {
        tier1Weight = t1;
        tier2Weight = t2;
        tier3Weight = t3;
        tier4Weight = t4;
        tier5Weight = t5;
    }

    public float GetWeight(EnemyTier tier)
    {
        return tier switch
        {
            EnemyTier.Tier1 => tier1Weight,
            EnemyTier.Tier2 => tier2Weight,
            EnemyTier.Tier3 => tier3Weight,
            EnemyTier.Tier4 => tier4Weight,
            EnemyTier.Tier5 => tier5Weight,
            _ => 0f
        };
    }

    public float GetTotalWeight()
    {
        return tier1Weight + tier2Weight + tier3Weight + tier4Weight + tier5Weight;
    }

    public bool IsValid()
    {
        return GetTotalWeight() > 0f;
    }

    public TierWeights Normalize()
    {
        float total = GetTotalWeight();
        if (total <= 0f) return new TierWeights(1f, 0f, 0f, 0f, 0f);

        return new TierWeights(
            tier1Weight / total,
            tier2Weight / total,
            tier3Weight / total,
            tier4Weight / total,
            tier5Weight / total
        );
    }

    public static TierWeights Default => new TierWeights(1f, 0f, 0f, 0f, 0f);

    public override string ToString()
    {
        return $"T1:{tier1Weight:F2} T2:{tier2Weight:F2} T3:{tier3Weight:F2} T4:{tier4Weight:F2} T5:{tier5Weight:F2}";
    }
}