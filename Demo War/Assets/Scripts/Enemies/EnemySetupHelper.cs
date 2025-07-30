using UnityEngine;

public static class EnemySetupHelper
{
    [System.Serializable]
    public struct EnemyTemplate
    {
        public string enemyId;
        public string enemyName;
        public EnemyTier tier;
        public int minWaveNumber;
        public float difficultyValue;
        public float maxHealth;
        public float moveSpeed;
        public EnemyMovementType movementType;
        public EnemyAttackType attackType;
        public float attackDamage;
        public float attackRange;
        public float attackInterval;
        public Color enemyColor;
        public int experienceDrop;
    }

    public static EnemyConfig CreateEnemyConfig(EnemyTemplate template)
    {
        var config = ScriptableObject.CreateInstance<EnemyConfig>();

        config.enemyId = template.enemyId;
        config.enemyName = template.enemyName;
        config.tier = template.tier;
        config.minWaveNumber = template.minWaveNumber;
        config.difficultyValue = template.difficultyValue;
        config.maxHealth = template.maxHealth;
        config.moveSpeed = template.moveSpeed;
        config.movementType = template.movementType;
        config.attackType = template.attackType;
        config.attackDamage = template.attackDamage;
        config.attackRange = template.attackRange;
        config.attackInterval = template.attackInterval;
        config.enemyColor = template.enemyColor;
        config.experienceDrop = template.experienceDrop;

        SetDefaultValues(config);
        return config;
    }

    private static void SetDefaultValues(EnemyConfig config)
    {
        config.collisionDamage = 10f;
        config.explosionDamage = 15f;
        config.detectionRange = 10f;
        config.optimalDistance = 5f;
        config.scale = 1f;
        config.projectileSpeed = 10f;
        config.projectileLifetime = 5f;
        config.movementAmplitude = 2f;
        config.movementFrequency = 1f;
        config.canAttackWhileMoving = true;

        if (config.attackType == EnemyAttackType.BurstFire)
        {
            config.burstCount = 3;
            config.burstInterval = 0.3f;
            config.burstCooldown = 3f;
        }

        if (config.attackType == EnemyAttackType.Spray)
        {
            config.projectileCount = 3;
            config.spreadAngle = 15f;
        }
    }

    public static EnemyTemplate[] GetBasicTemplates()
    {
        return new EnemyTemplate[]
        {
            new EnemyTemplate
            {
                enemyId = "basic_runner",
                enemyName = "Basic Runner",
                tier = EnemyTier.Tier1,
                minWaveNumber = 1,
                difficultyValue = 1f,
                maxHealth = 40f,
                moveSpeed = 4f,
                movementType = EnemyMovementType.DirectChase,
                attackType = EnemyAttackType.None,
                enemyColor = Color.green,
                experienceDrop = 5
            },

            new EnemyTemplate
            {
                enemyId = "basic_shooter",
                enemyName = "Basic Shooter",
                tier = EnemyTier.Tier1,
                minWaveNumber = 2,
                difficultyValue = 1.5f,
                maxHealth = 35f,
                moveSpeed = 2.5f,
                movementType = EnemyMovementType.Stop,
                attackType = EnemyAttackType.SingleShot,
                attackDamage = 10f,
                attackRange = 6f,
                attackInterval = 2f,
                enemyColor = Color.red,
                experienceDrop = 8
            },

            new EnemyTemplate
            {
                enemyId = "soldier",
                enemyName = "Soldier",
                tier = EnemyTier.Tier2,
                minWaveNumber = 5,
                difficultyValue = 2.5f,
                maxHealth = 80f,
                moveSpeed = 3f,
                movementType = EnemyMovementType.DirectChase,
                attackType = EnemyAttackType.SingleShot,
                attackDamage = 15f,
                attackRange = 7f,
                attackInterval = 1.8f,
                enemyColor = new Color(1f, 0.3f, 0.3f),
                experienceDrop = 15
            },

            new EnemyTemplate
            {
                enemyId = "heavy_tank",
                enemyName = "Heavy Tank",
                tier = EnemyTier.Tier3,
                minWaveNumber = 10,
                difficultyValue = 5f,
                maxHealth = 200f,
                moveSpeed = 1.5f,
                movementType = EnemyMovementType.DirectChase,
                attackType = EnemyAttackType.Explosive,
                attackDamage = 25f,
                attackRange = 5f,
                attackInterval = 3f,
                enemyColor = new Color(0.2f, 0.2f, 0.2f),
                experienceDrop = 35
            }
        };
    }

    public static void ValidateEnemyConfig(EnemyConfig config)
    {
        if (config == null)
        {
            Debug.LogError("EnemyConfig is null!");
            return;
        }

        var issues = new System.Collections.Generic.List<string>();

        if (string.IsNullOrEmpty(config.enemyId))
            issues.Add("Enemy ID is required");

        if (string.IsNullOrEmpty(config.enemyName))
            issues.Add("Enemy name is required");

        if (config.maxHealth <= 0)
            issues.Add("Max health must be greater than 0");

        if (config.moveSpeed < 0)
            issues.Add("Move speed cannot be negative");

        if (config.difficultyValue <= 0)
            issues.Add("Difficulty value must be greater than 0");

        if (config.minWaveNumber < 1)
            issues.Add("Min wave number must be at least 1");

        if (config.attackType != EnemyAttackType.None)
        {
            if (config.attackDamage <= 0)
                issues.Add("Attack damage must be greater than 0 for attacking enemies");

            if (config.attackRange <= 0)
                issues.Add("Attack range must be greater than 0 for attacking enemies");

            if (config.attackInterval <= 0)
                issues.Add("Attack interval must be greater than 0 for attacking enemies");

            if (config.attackRange > config.detectionRange)
                issues.Add("Attack range should not exceed detection range");
        }

        if (config.attackType == EnemyAttackType.BurstFire)
        {
            if (config.burstCount <= 0)
                issues.Add("Burst count must be greater than 0 for burst fire enemies");

            if (config.burstInterval <= 0)
                issues.Add("Burst interval must be greater than 0 for burst fire enemies");

            if (config.burstCooldown <= 0)
                issues.Add("Burst cooldown must be greater than 0 for burst fire enemies");
        }

        if (config.attackType == EnemyAttackType.Spray)
        {
            if (config.projectileCount <= 0)
                issues.Add("Projectile count must be greater than 0 for spray enemies");

            if (config.spreadAngle < 0)
                issues.Add("Spread angle cannot be negative");
        }

        if (config.hasShield && config.shieldHealth <= 0)
            issues.Add("Shield health must be greater than 0 if shield is enabled");

        if (config.regeneratesHealth && config.healthRegenRate <= 0)
            issues.Add("Health regen rate must be greater than 0 if regeneration is enabled");

        if (config.canSplit && config.splitCount <= 0)
            issues.Add("Split count must be greater than 0 if splitting is enabled");

        if (issues.Count == 0)
        {
            Debug.Log($"? EnemyConfig '{config.enemyName}' validation passed!");
        }
        else
        {
            Debug.LogError($"EnemyConfig '{config.enemyName}' has {issues.Count} validation issues:");
            foreach (var issue in issues)
            {
                Debug.LogError($"- {issue}");
            }
        }
    }

    public static string GetEnemyConfigSummary(EnemyConfig config)
    {
        if (config == null) return "NULL CONFIG";

        var summary = $"=== {config.enemyName} ({config.enemyId}) ===\n";
        summary += $"Tier: {config.tier} | Wave: {config.minWaveNumber}+ | Difficulty: {config.difficultyValue:F1}\n";
        summary += $"Health: {config.maxHealth} | Speed: {config.moveSpeed} | XP: {config.experienceDrop}\n";
        summary += $"Movement: {config.movementType} | Attack: {config.attackType}\n";

        if (config.attackType != EnemyAttackType.None)
        {
            summary += $"Attack Damage: {config.attackDamage} | Range: {config.attackRange} | Interval: {config.attackInterval:F1}s\n";
        }

        if (config.hasShield)
        {
            summary += $"Shield: {config.shieldHealth} HP\n";
        }

        if (config.armor > 0)
        {
            summary += $"Armor: {config.armor}\n";
        }

        if (config.regeneratesHealth)
        {
            summary += $"Regeneration: {config.healthRegenRate} HP/s\n";
        }

        return summary;
    }

    [System.Obsolete("Use individual EnemyConfig assets instead")]
    public static void CreateBasicEnemyConfigs()
    {
        Debug.LogWarning("This method is deprecated. Please create individual EnemyConfig assets manually using the CreateAssetMenu.");
    }
}