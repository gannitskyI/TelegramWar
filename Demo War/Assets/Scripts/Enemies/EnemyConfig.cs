using UnityEngine;

public enum EnemyAttackType
{
    None,           // Только взрыв при столкновении
    SingleShot,     // Одиночные выстрелы
    BurstFire,      // Очереди
    Spray,          // Веерная стрельба
    Homing,         // Самонаводящиеся снаряды
    Laser,          // Лазерная атака
    Orbital,        // Орбитальные снаряды
    Explosive       // Взрывчатые снаряды
}

public enum EnemyMovementType
{
    DirectChase,    // Прямое преследование
    CircularOrbit,  // Круговое движение вокруг игрока
    ZigZag,         // Зигзагообразное движение
    Stop,           // Останавливается на дистанции
    Teleport,       // Телепортация
    Wave,           // Волнообразное движение
    Spiral,         // Спиральное движение
    Bounce          // Отскоки от стен
}

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/Enemy Configuration")]
public class EnemyConfig : ScriptableObject
{
    [Header("Basic Stats")]
    public string enemyName = "Enemy";
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

    [ContextMenu("Validate Configuration")]
    private void ValidateConfig()
    {
        if (maxHealth <= 0f)
            Debug.LogWarning($"[{enemyName}] Health should be greater than 0");

        if (attackType != EnemyAttackType.None && attackDamage <= 0f)
            Debug.LogWarning($"[{enemyName}] Attack damage should be greater than 0 for attacking enemies");

        if (attackRange > detectionRange)
            Debug.LogWarning($"[{enemyName}] Attack range should not exceed detection range");

        if (optimalDistance > attackRange && attackType != EnemyAttackType.None)
            Debug.LogWarning($"[{enemyName}] Optimal distance should be within attack range for attacking enemies");
    }

    [ContextMenu("Create Preset - Weak Runner")]
    private void CreateWeakRunner()
    {
        enemyName = "Weak Runner";
        maxHealth = 30f;
        moveSpeed = 4f;
        movementType = EnemyMovementType.DirectChase;
        attackType = EnemyAttackType.None;
        collisionDamage = 8f;
        explosionDamage = 15f;
        experienceDrop = 5;
        enemyColor = Color.gray;
    }

    [ContextMenu("Create Preset - Gunner")]
    private void CreateGunner()
    {
        enemyName = "Gunner";
        maxHealth = 50f;
        moveSpeed = 2f;
        movementType = EnemyMovementType.Stop;
        attackType = EnemyAttackType.SingleShot;
        attackRange = 8f;
        attackDamage = 12f;
        attackInterval = 1.5f;
        optimalDistance = 6f;
        experienceDrop = 10;
        enemyColor = Color.red;
    }

    [ContextMenu("Create Preset - Burst Soldier")]
    private void CreateBurstSoldier()
    {
        enemyName = "Burst Soldier";
        maxHealth = 75f;
        moveSpeed = 2.5f;
        movementType = EnemyMovementType.CircularOrbit;
        attackType = EnemyAttackType.BurstFire;
        burstCount = 3;
        burstInterval = 0.3f;
        burstCooldown = 4f;
        attackRange = 10f;
        experienceDrop = 15;
        enemyColor = Color.magenta;
    }

    [ContextMenu("Create Preset - Tank")]
    private void CreateTank()
    {
        enemyName = "Tank";
        maxHealth = 200f;
        moveSpeed = 1f;
        movementType = EnemyMovementType.DirectChase;
        attackType = EnemyAttackType.Explosive;
        attackRange = 6f;
        attackDamage = 25f;
        armor = 20f;
        hasShield = true;
        shieldHealth = 50f;
        experienceDrop = 30;
        enemyColor = Color.black;
    }

    [ContextMenu("Create Preset - Sniper")]
    private void CreateSniper()
    {
        enemyName = "Sniper";
        maxHealth = 40f;
        moveSpeed = 1.5f;
        movementType = EnemyMovementType.Stop;
        attackType = EnemyAttackType.Homing;
        attackRange = 15f;
        attackDamage = 20f;
        attackInterval = 3f;
        optimalDistance = 12f;
        fleeWhenLowHealth = true;
        retreatThreshold = 0.5f;
        experienceDrop = 20;
        enemyColor = Color.cyan;
    }
}