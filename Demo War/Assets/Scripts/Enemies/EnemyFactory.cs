using System.Threading.Tasks;
using UnityEngine;

public class EnemyFactory
{
    private AddressableManager addressableManager;
    private EnemyPool enemyPool;
    private bool isWarmedUp = false;

    public EnemyFactory(AddressableManager addressableManager)
    {
        this.addressableManager = addressableManager;
        this.enemyPool = new EnemyPool();
        enemyPool.SetFactory(this);
    }

    public async Task EnsureWarmedUp()
    {
        if (isWarmedUp) return;
        await WarmupPoolsAsync();
        isWarmedUp = true;
    }

    public async Task<GameObject> CreateEnemy(string enemyType, Vector3 position)
    {
        if (string.IsNullOrEmpty(enemyType))
        {
            Debug.LogError("Enemy type is null or empty!");
            return null;
        }

        await EnsureWarmedUp();

        var pooledEnemy = enemyPool.Get(enemyType);
        if (pooledEnemy != null)
        {
            pooledEnemy.transform.position = position;
            pooledEnemy.SetActive(true);

            var enemyComponent = pooledEnemy.GetComponent<EnemyBehaviour>();
            if (enemyComponent != null)
            {
                var config = CreateEnemyConfig(enemyType);
                enemyComponent.Initialize(config, () => ReturnToPool(pooledEnemy, enemyType));
                return pooledEnemy;
            }
            else
            {
                Debug.LogError($"EnemyBehaviour component not found on pooled enemy: {enemyType}");
                ReturnToPool(pooledEnemy, enemyType);
                return null;
            }
        }

        return await CreateNewEnemy(enemyType, position);
    }

    private async Task<GameObject> CreateNewEnemy(string enemyType, Vector3 position)
    {
        var prefabKey = $"Enemy_{enemyType}";
        var enemy = await addressableManager.InstantiateAsync(prefabKey, position);

        if (enemy == null)
        {
            Debug.LogWarning($"Failed to load enemy prefab: {prefabKey}, creating fallback enemy");
            return CreateFallbackEnemy(enemyType, position);
        }

        var enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();
        if (enemyBehaviour == null)
        {
            enemyBehaviour = enemy.AddComponent<EnemyBehaviour>();
        }

        var enemyConfig = CreateEnemyConfig(enemyType);
        enemyBehaviour.Initialize(enemyConfig, () => ReturnToPool(enemy, enemyType));

        return enemy;
    }

    private GameObject CreateFallbackEnemy(string enemyType, Vector3 position)
    {
        var enemyGO = new GameObject($"Fallback_{enemyType}");
        enemyGO.transform.position = position;

        var enemyBehaviour = enemyGO.AddComponent<EnemyBehaviour>();
        var config = CreateEnemyConfig(enemyType);
        enemyBehaviour.Initialize(config, () => ReturnToPool(enemyGO, enemyType));

        enemyGO.tag = "Enemy";

        Debug.Log($"Created fallback enemy: {enemyType}");
        return enemyGO;
    }

    public async Task<GameObject> CreateEnemyForPool(string enemyType)
    {
        var prefabKey = $"Enemy_{enemyType}";
        var enemy = await addressableManager.InstantiateAsync(prefabKey, Vector3.zero);

        if (enemy == null)
        {
            enemy = CreateFallbackEnemy(enemyType, Vector3.zero);
        }

        var enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();
        if (enemyBehaviour == null)
        {
            enemyBehaviour = enemy.AddComponent<EnemyBehaviour>();
        }

        var config = CreateEnemyConfig(enemyType);
        enemyBehaviour.InitializeForPool(config);
        enemy.SetActive(false);
        return enemy;
    }

    private EnemyConfig CreateEnemyConfig(string enemyType)
    {
        var config = ScriptableObject.CreateInstance<EnemyConfig>();

        switch (enemyType.ToLower())
        {
            
            case "strong":
                ConfigureStrongEnemy(config);
                break;
            case "fast":
                ConfigureFastEnemy(config);
                break;
            case "tank":
                ConfigureTankEnemy(config);
                break;
            case "sniper":
                ConfigureSniperEnemy(config);
                break;
            case "burst":
                ConfigureBurstEnemy(config);
                break;
            case "bomber":
                ConfigureBomberEnemy(config);
                break;
            case "assassin":
                ConfigureAssassinEnemy(config);
                break;
            case "guardian":
                ConfigureGuardianEnemy(config);
                break;
            default:
                ConfigureNormalEnemy(config);
                break;
        }

        return config;
    }

    private void ConfigureWeakEnemy(EnemyConfig config)
    {
        config.enemyName = "Weak Runner";
        config.maxHealth = 30f;
        config.moveSpeed = 4f;
        config.movementType = EnemyMovementType.DirectChase;
        config.attackType = EnemyAttackType.None;
        config.collisionDamage = 8f;
        config.explosionDamage = 15f;
        config.experienceDrop = 5;
        config.enemyColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        config.scale = 0.8f;
        config.detectionRange = 8f;
    }

    private void ConfigureNormalEnemy(EnemyConfig config)
    {
        config.enemyName = "Standard Soldier";
        config.maxHealth = 60f;
        config.moveSpeed = 3f;
        config.movementType = EnemyMovementType.DirectChase;
        config.attackType = EnemyAttackType.SingleShot;
        config.attackRange = 6f;
        config.attackDamage = 12f;
        config.attackInterval = 2f;
        config.projectileSpeed = 8f;
        config.collisionDamage = 10f;
        config.explosionDamage = 20f;
        config.experienceDrop = 10;
        config.enemyColor = Color.red;
        config.detectionRange = 10f;
        config.optimalDistance = 4f;
    }

    private void ConfigureStrongEnemy(EnemyConfig config)
    {
        config.enemyName = "Heavy Trooper";
        config.maxHealth = 120f;
        config.moveSpeed = 2f;
        config.movementType = EnemyMovementType.DirectChase;
        config.attackType = EnemyAttackType.SingleShot;
        config.attackRange = 7f;
        config.attackDamage = 18f;
        config.attackInterval = 1.8f;
        config.projectileSpeed = 9f;
        config.collisionDamage = 15f;
        config.explosionDamage = 30f;
        config.experienceDrop = 18;
        config.enemyColor = new Color(0.8f, 0.2f, 0.8f, 1f);
        config.armor = 10f;
        config.scale = 1.2f;
        config.detectionRange = 12f;
        config.optimalDistance = 5f;
    }

    private void ConfigureFastEnemy(EnemyConfig config)
    {
        config.enemyName = "Speed Demon";
        config.maxHealth = 40f;
        config.moveSpeed = 6f;
        config.movementType = EnemyMovementType.ZigZag;
        config.attackType = EnemyAttackType.SingleShot;
        config.attackRange = 5f;
        config.attackDamage = 10f;
        config.attackInterval = 1.2f;
        config.projectileSpeed = 12f;
        config.movementAmplitude = 2f;
        config.movementFrequency = 3f;
        config.collisionDamage = 12f;
        config.explosionDamage = 18f;
        config.experienceDrop = 15;
        config.enemyColor = Color.yellow;
        config.scale = 0.9f;
        config.detectionRange = 9f;
        config.canAttackWhileMoving = true;
    }

    private void ConfigureTankEnemy(EnemyConfig config)
    {
        config.enemyName = "Armored Tank";
        config.maxHealth = 300f;
        config.moveSpeed = 1.2f;
        config.movementType = EnemyMovementType.DirectChase;
        config.attackType = EnemyAttackType.Explosive;
        config.attackRange = 8f;
        config.attackDamage = 35f;
        config.attackInterval = 3f;
        config.projectileSpeed = 6f;
        config.collisionDamage = 25f;
        config.explosionDamage = 50f;
        config.experienceDrop = 40;
        config.enemyColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        config.armor = 25f;
        config.hasShield = true;
        config.shieldHealth = 100f;
        config.scale = 1.5f;
        config.detectionRange = 15f;
        config.optimalDistance = 6f;
        config.stunDuration = 0.5f;
    }

    private void ConfigureSniperEnemy(EnemyConfig config)
    {
        config.enemyName = "Sniper";
        config.maxHealth = 50f;
        config.moveSpeed = 1.5f;
        config.movementType = EnemyMovementType.Stop;
        config.attackType = EnemyAttackType.Homing;
        config.attackRange = 15f;
        config.attackDamage = 25f;
        config.attackInterval = 4f;
        config.projectileSpeed = 10f;
        config.projectileLifetime = 8f;
        config.collisionDamage = 8f;
        config.explosionDamage = 15f;
        config.experienceDrop = 25;
        config.enemyColor = Color.cyan;
        config.detectionRange = 18f;
        config.optimalDistance = 12f;
        config.fleeWhenLowHealth = true;
        config.retreatThreshold = 0.4f;
        config.attackWarningDuration = 1f;
        config.attackWarningColor = Color.red;
    }

    private void ConfigureBurstEnemy(EnemyConfig config)
    {
        config.enemyName = "Burst Gunner";
        config.maxHealth = 80f;
        config.moveSpeed = 2.5f;
        config.movementType = EnemyMovementType.CircularOrbit;
        config.attackType = EnemyAttackType.BurstFire;
        config.attackRange = 9f;
        config.attackDamage = 14f;
        config.burstCount = 4;
        config.burstInterval = 0.25f;
        config.burstCooldown = 5f;
        config.projectileSpeed = 11f;
        config.collisionDamage = 12f;
        config.explosionDamage = 22f;
        config.experienceDrop = 20;
        config.enemyColor = new Color(1f, 0.5f, 0f, 1f);
        config.detectionRange = 12f;
        config.optimalDistance = 7f;
        config.movementAmplitude = 3f;
        config.movementFrequency = 1.5f;
    }

    private void ConfigureBomberEnemy(EnemyConfig config)
    {
        config.enemyName = "Bomber";
        config.maxHealth = 70f;
        config.moveSpeed = 2f;
        config.movementType = EnemyMovementType.Wave;
        config.attackType = EnemyAttackType.Explosive;
        config.attackRange = 6f;
        config.attackDamage = 30f;
        config.attackInterval = 2.5f;
        config.projectileSpeed = 7f;
        config.collisionDamage = 20f;
        config.explosionDamage = 40f;
        config.experienceDrop = 22;
        config.enemyColor = new Color(1f, 0.3f, 0.3f, 1f);
        config.detectionRange = 10f;
        config.optimalDistance = 4f;
        config.movementAmplitude = 1.5f;
        config.movementFrequency = 2f;
        config.attackWarningDuration = 0.8f;
        config.attackWarningColor = Color.red;
    }

    private void ConfigureAssassinEnemy(EnemyConfig config)
    {
        config.enemyName = "Shadow Assassin";
        config.maxHealth = 35f;
        config.moveSpeed = 5f;
        config.movementType = EnemyMovementType.Teleport;
        config.attackType = EnemyAttackType.SingleShot;
        config.attackRange = 4f;
        config.attackDamage = 22f;
        config.attackInterval = 1f;
        config.projectileSpeed = 15f;
        config.collisionDamage = 18f;
        config.explosionDamage = 25f;
        config.experienceDrop = 28;
        config.enemyColor = new Color(0.5f, 0f, 0.8f, 0.8f);
        config.scale = 0.7f;
        config.detectionRange = 8f;
        config.optimalDistance = 3f;
        config.fleeWhenLowHealth = true;
        config.retreatThreshold = 0.6f;
        config.canAttackWhileMoving = true;
    }

    private void ConfigureGuardianEnemy(EnemyConfig config)
    {
        config.enemyName = "Energy Guardian";
        config.maxHealth = 150f;
        config.moveSpeed = 1.8f;
        config.movementType = EnemyMovementType.Spiral;
        config.attackType = EnemyAttackType.Spray;
        config.attackRange = 10f;
        config.attackDamage = 16f;
        config.attackInterval = 3f;
        config.projectileCount = 5;
        config.spreadAngle = 15f;
        config.projectileSpeed = 9f;
        config.collisionDamage = 15f;
        config.explosionDamage = 30f;
        config.experienceDrop = 35;
        config.enemyColor = new Color(0f, 1f, 0.5f, 1f);
        config.scale = 1.3f;
        config.detectionRange = 14f;
        config.optimalDistance = 8f;
        config.hasShield = true;
        config.shieldHealth = 75f;
        config.regeneratesHealth = true;
        config.healthRegenRate = 3f;
        config.movementAmplitude = 2f;
        config.movementFrequency = 1f;
    }

    private async Task WarmupPoolsAsync()
    {
        string[] enemyTypes = { "weak", "normal", "strong", "fast", "tank", "sniper", "burst", "bomber", "assassin", "guardian" };
        try
        {
            foreach (string enemyType in enemyTypes)
            {
                await enemyPool.WarmupAsync(enemyType, 2);
            }
            Debug.Log("Enemy pools warmed up successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to warmup pools: {e.Message}");
            isWarmedUp = false;
        }
    }

    private void ReturnToPool(GameObject enemy, string enemyType)
    {
        if (enemy != null)
        {
            enemyPool.Return(enemy, enemyType);
        }
    }

    public string GetRandomEnemyType(int waveNumber)
    {
        if (waveNumber <= 0)
        {
            Debug.LogWarning("Wave number should be greater than 0, returning default enemy type");
            return "weak";
        }

        if (waveNumber <= 2)
        {
            int rand = Random.Range(0, 100);
            if (rand < 60) return "weak";
            if (rand < 90) return "normal";
            return "fast";
        }
        else if (waveNumber <= 5)
        {
            int rand = Random.Range(0, 100);
            if (rand < 30) return "weak";
            if (rand < 50) return "normal";
            if (rand < 70) return "fast";
            if (rand < 85) return "strong";
            if (rand < 95) return "sniper";
            return "burst";
        }
        else if (waveNumber <= 10)
        {
            int rand = Random.Range(0, 100);
            if (rand < 15) return "normal";
            if (rand < 30) return "fast";
            if (rand < 45) return "strong";
            if (rand < 60) return "sniper";
            if (rand < 75) return "burst";
            if (rand < 85) return "bomber";
            if (rand < 95) return "tank";
            return "assassin";
        }
        else
        {
            int rand = Random.Range(0, 100);
            if (rand < 10) return "fast";
            if (rand < 25) return "strong";
            if (rand < 35) return "sniper";
            if (rand < 45) return "burst";
            if (rand < 55) return "bomber";
            if (rand < 70) return "tank";
            if (rand < 85) return "assassin";
            return "guardian";
        }
    }

    public EnemyConfig GetEnemyConfig(string enemyType)
    {
        return CreateEnemyConfig(enemyType);
    }

    public void ClearAllPools()
    {
        enemyPool?.ClearAll();
        isWarmedUp = false;
    }

    public string GetDebugInfo()
    {
        var info = enemyPool?.GetPoolInfo() ?? "EnemyPool not initialized";
        info += "\n\nAvailable Enemy Types:\n";
        info += "- weak: Basic runner, no ranged attacks\n";
        info += "- normal: Standard soldier with single shots\n";
        info += "- strong: Heavy trooper with armor\n";
        info += "- fast: Speed demon with zigzag movement\n";
        info += "- tank: Armored tank with explosive rounds\n";
        info += "- sniper: Long-range homing projectiles\n";
        info += "- burst: Burst fire with orbital movement\n";
        info += "- bomber: Explosive projectiles with wave movement\n";
        info += "- assassin: Fast teleporting attacker\n";
        info += "- guardian: Multi-shot spray with shields and regen\n";
        return info;
    }

    public void Cleanup()
    {
        ClearAllPools();
        enemyPool = null;
        addressableManager = null;
        isWarmedUp = false;
    }
}