using UnityEngine;

public enum EnemyAttackType
{
    None,
    SingleShot,
    BurstFire,
    Spray,
    Homing,
    Laser,
    Orbital,
    Explosive
}

public enum EnemyMovementType
{
    DirectChase,
    CircularOrbit,
    ZigZag,
    Stop,
    Teleport,
    Wave,
    Spiral,
    Bounce
}

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Enemy Configuration")]
public class EnemyConfig : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] public string enemyId = "";
    [Tooltip("ID will be auto-generated from asset name if left empty")]
    public string enemyName = "Enemy";
    public EnemyTier tier = EnemyTier.Tier1;
    public int minWaveNumber = 1;
    public float difficultyValue = 1f;

    [Header("Basic Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 3f;
    public float collisionDamage = 10f;
    public float explosionDamage = 20f;
    public int experienceDrop = 10;
    public float scale = 1f;

    [Header("Movement Behavior")]
    public EnemyMovementType movementType = EnemyMovementType.DirectChase;
    public float detectionRange = 10f;
    public float optimalDistance = 5f;
    public float movementAmplitude = 2f;
    public float movementFrequency = 1f;
    public bool canMoveOffScreen = false;

    [Header("Attack System")]
    public EnemyAttackType attackType = EnemyAttackType.None;
    public float attackRange = 8f;
    public float attackDamage = 15f;
    public float attackInterval = 2f;
    public float projectileSpeed = 10f;
    public float projectileLifetime = 5f;
    public int projectileCount = 1;
    public float spreadAngle = 0f;
    public bool canAttackWhileMoving = true;

    [Header("Burst Fire Settings")]
    [Tooltip("Only for BurstFire attack type")]
    public int burstCount = 3;
    public float burstInterval = 0.2f;
    public float burstCooldown = 3f;

    [Header("Advanced Behavior")]
    public float aggroRange = 12f;
    public float retreatThreshold = 0.3f;
    public bool fleeWhenLowHealth = false;
    public float stunDuration = 0f;
    public bool immuneToPlayerBullets = false;
    public float armor = 0f;

    [Header("Visual Effects")]
    public Color enemyColor = Color.red;
    public Color attackWarningColor = Color.yellow;
    public float attackWarningDuration = 0.5f;
    public bool hasDeathAnimation = true;
    public float deathAnimationDuration = 0.3f;

    [Header("Special Abilities")]
    public bool canSplit = false;
    public int splitCount = 2;
    public float splitHealthRatio = 0.5f;
    public bool regeneratesHealth = false;
    public float healthRegenRate = 5f;
    public bool hasShield = false;
    public float shieldHealth = 50f;

    [Header("Audio")]
    public string attackSoundKey = "";
    public string deathSoundKey = "";
    public string movementSoundKey = "";

    // Public property for accessing the ID
    public string EnemyId
    {
        get
        {
            if (string.IsNullOrEmpty(enemyId))
            {
                GenerateEnemyId();
            }
            return enemyId;
        }
    }

    private void GenerateEnemyId()
    {
        if (string.IsNullOrEmpty(name))
        {
            enemyId = "unknown_enemy";
            return;
        }

        // Remove "Enemy_" prefix if exists
        string baseName = name;
        if (baseName.StartsWith("Enemy_"))
        {
            baseName = baseName.Substring(6);
        }

        // Convert to lowercase and replace spaces/special chars with underscores
        enemyId = baseName.ToLower()
            .Replace(" ", "_")
            .Replace("-", "_")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "");

        // Remove multiple underscores
        while (enemyId.Contains("__"))
        {
            enemyId = enemyId.Replace("__", "_");
        }

        // Remove leading/trailing underscores
        enemyId = enemyId.Trim('_');

        // Ensure it's not empty
        if (string.IsNullOrEmpty(enemyId))
        {
            enemyId = "enemy_" + GetInstanceID().ToString().Replace("-", "");
        }

        Debug.Log($"Auto-generated Enemy ID: '{enemyId}' for asset '{name}'");
    }

    private void OnValidate()
    {
        // Auto-generate ID when asset is modified
        if (string.IsNullOrEmpty(enemyId))
        {
            GenerateEnemyId();
        }
    }

    private void Awake()
    {
        // Ensure ID is generated when asset is loaded
        if (string.IsNullOrEmpty(enemyId))
        {
            GenerateEnemyId();
        }
    }

    public float GetEffectiveHealth()
    {
        return hasShield ? maxHealth + shieldHealth : maxHealth;
    }

    public float CalculateDamageReduction(float incomingDamage)
    {
        if (armor <= 0f) return incomingDamage;

        float reduction = armor / (armor + 100f);
        return incomingDamage * (1f - reduction);
    }

    public bool ShouldFlee(float currentHealth)
    {
        return fleeWhenLowHealth && (currentHealth / maxHealth) <= retreatThreshold;
    }

    public Vector2 GetOptimalPosition(Vector2 currentPos, Vector2 targetPos)
    {
        Vector2 direction = (targetPos - currentPos).normalized;
        return targetPos - direction * optimalDistance;
    }

    public bool IsInAttackRange(Vector2 enemyPos, Vector2 targetPos)
    {
        return Vector2.Distance(enemyPos, targetPos) <= attackRange;
    }

    public bool IsInDetectionRange(Vector2 enemyPos, Vector2 targetPos)
    {
        return Vector2.Distance(enemyPos, targetPos) <= detectionRange;
    }

    public bool IsInAggroRange(Vector2 enemyPos, Vector2 targetPos)
    {
        return Vector2.Distance(enemyPos, targetPos) <= aggroRange;
    }

    public float GetTierMultiplier()
    {
        return tier switch
        {
            EnemyTier.Tier1 => 1f,
            EnemyTier.Tier2 => 1.5f,
            EnemyTier.Tier3 => 2.25f,
            EnemyTier.Tier4 => 3.5f,
            EnemyTier.Tier5 => 5f,
            _ => 1f
        };
    }

    [ContextMenu("Regenerate Enemy ID")]
    private void RegenerateId()
    {
        enemyId = "";
        GenerateEnemyId();
        Debug.Log($"Regenerated Enemy ID: {enemyId}");
    }

    [ContextMenu("Validate Configuration")]
    private void ValidateConfig()
    {
        var issues = new System.Collections.Generic.List<string>();

        if (string.IsNullOrEmpty(EnemyId))
            issues.Add("Enemy ID could not be generated");

        if (maxHealth <= 0f)
            issues.Add("Health should be greater than 0");

        if (attackType != EnemyAttackType.None && attackDamage <= 0f)
            issues.Add("Attack damage should be greater than 0 for attacking enemies");

        if (attackRange > detectionRange)
            issues.Add("Attack range should not exceed detection range");

        if (optimalDistance > attackRange && attackType != EnemyAttackType.None)
            issues.Add("Optimal distance should be within attack range for attacking enemies");

        if (difficultyValue <= 0)
            issues.Add("Difficulty value should be greater than 0");

        if (issues.Count == 0)
        {
            Debug.Log($"? Enemy config '{enemyName}' (ID: {EnemyId}) validation passed!");
        }
        else
        {
            Debug.LogWarning($"Enemy config '{enemyName}' has {issues.Count} issues:");
            foreach (var issue in issues)
            {
                Debug.LogWarning($"- {issue}");
            }
        }
    }

    [ContextMenu("Auto-Calculate Difficulty")]
    private void AutoCalculateDifficulty()
    {
        float baseDifficulty = GetTierMultiplier();

        float healthFactor = maxHealth / 100f;
        float damageFactor = (attackDamage + collisionDamage) / 20f;
        float speedFactor = moveSpeed / 3f;
        float specialFactor = 1f;

        if (hasShield) specialFactor += 0.5f;
        if (regeneratesHealth) specialFactor += 0.3f;
        if (armor > 0) specialFactor += armor / 100f;
        if (canSplit) specialFactor += 0.4f;

        difficultyValue = baseDifficulty * healthFactor * damageFactor * speedFactor * specialFactor;

        Debug.Log($"Auto-calculated difficulty for {enemyName}: {difficultyValue:F2}");
    }
}