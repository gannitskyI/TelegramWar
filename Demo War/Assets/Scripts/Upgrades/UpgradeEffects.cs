using UnityEngine;

public interface IUpgradeEffect
{
    UpgradeType TargetType { get; }
    void Apply(UpgradeConfig config, float deltaValue, int currentLevel);
    void Remove(UpgradeConfig config, float totalValue);
    void Reset();
}

public abstract class BaseUpgradeEffect : IUpgradeEffect
{
    protected readonly GameObject playerObject;

    public abstract UpgradeType TargetType { get; }

    protected BaseUpgradeEffect(GameObject player)
    {
        playerObject = player ?? throw new System.ArgumentNullException(nameof(player));
    }

    public abstract void Apply(UpgradeConfig config, float deltaValue, int currentLevel);
    public abstract void Remove(UpgradeConfig config, float totalValue);
    public abstract void Reset();
}

public class DamageUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.Damage;

    private PlayerCombat playerCombat;
    private float originalDamage;
    private float currentMultiplier = 1f;

    public DamageUpgradeEffect(GameObject player) : base(player)
    {
        playerCombat = player.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            originalDamage = playerCombat.GetBulletDamage();
        }
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        if (playerCombat == null) return;

        currentMultiplier += deltaValue;
        var newDamage = originalDamage * currentMultiplier;

        playerCombat.SetBulletDamage(newDamage);
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        if (playerCombat == null) return;

        currentMultiplier -= totalValue;
        var newDamage = originalDamage * currentMultiplier;

        playerCombat.SetBulletDamage(newDamage);
    }

    public override void Reset()
    {
        if (playerCombat == null) return;

        currentMultiplier = 1f;
        playerCombat.SetBulletDamage(originalDamage);
    }
}

public class AttackSpeedUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.AttackSpeed;

    private PlayerCombat playerCombat;
    private float originalInterval;
    private float currentMultiplier = 1f;

    public AttackSpeedUpgradeEffect(GameObject player) : base(player)
    {
        playerCombat = player.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            originalInterval = playerCombat.GetAttackInterval();
        }
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        if (playerCombat == null) return;

        currentMultiplier += deltaValue;
        var newInterval = originalInterval / currentMultiplier;

        playerCombat.SetAttackInterval(newInterval);
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        if (playerCombat == null) return;

        currentMultiplier -= totalValue;
        var newInterval = originalInterval / currentMultiplier;

        playerCombat.SetAttackInterval(newInterval);
    }

    public override void Reset()
    {
        if (playerCombat == null) return;

        currentMultiplier = 1f;
        playerCombat.SetAttackInterval(originalInterval);
    }
}

public class AttackRangeUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.AttackRange;

    private PlayerCombat playerCombat;
    private float originalRange;
    private float currentMultiplier = 1f;

    public AttackRangeUpgradeEffect(GameObject player) : base(player)
    {
        playerCombat = player.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            originalRange = playerCombat.GetAttackRange();
        }
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        if (playerCombat == null) return;

        currentMultiplier += deltaValue;
        var newRange = originalRange * currentMultiplier;

        playerCombat.SetAttackRange(newRange);
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        if (playerCombat == null) return;

        currentMultiplier -= totalValue;
        var newRange = originalRange * currentMultiplier;

        playerCombat.SetAttackRange(newRange);
    }

    public override void Reset()
    {
        if (playerCombat == null) return;

        currentMultiplier = 1f;
        playerCombat.SetAttackRange(originalRange);
    }
}

public class MoveSpeedUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.MoveSpeed;

    private PlayerMovement playerMovement;
    private float originalSpeed;
    private float currentMultiplier = 1f;

    public MoveSpeedUpgradeEffect(GameObject player) : base(player)
    {
        playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            originalSpeed = playerMovement.GetMoveSpeed();
        }
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        if (playerMovement == null) return;

        currentMultiplier += deltaValue;
        var newSpeed = originalSpeed * currentMultiplier;

        playerMovement.SetMoveSpeed(newSpeed);
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        if (playerMovement == null) return;

        currentMultiplier -= totalValue;
        var newSpeed = originalSpeed * currentMultiplier;

        playerMovement.SetMoveSpeed(newSpeed);
    }

    public override void Reset()
    {
        if (playerMovement == null) return;

        currentMultiplier = 1f;
        playerMovement.SetMoveSpeed(originalSpeed);
    }
}

public class HealthUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.Health;

    private PlayerHealth playerHealth;
    private float originalMaxHealth;
    private float currentBonus = 0f;

    public HealthUpgradeEffect(GameObject player) : base(player)
    {
        playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            originalMaxHealth = playerHealth.GetMaxHealth();
        }
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        if (playerHealth == null) return;

        var healthBonus = originalMaxHealth * deltaValue;
        currentBonus += healthBonus;

        playerHealth.SetMaxHealth(originalMaxHealth + currentBonus);
        playerHealth.Heal(healthBonus);
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        if (playerHealth == null) return;

        var healthReduction = originalMaxHealth * totalValue;
        currentBonus -= healthReduction;

        playerHealth.SetMaxHealth(originalMaxHealth + currentBonus);
    }

    public override void Reset()
    {
        if (playerHealth == null) return;

        currentBonus = 0f;
        playerHealth.SetMaxHealth(originalMaxHealth);
    }
}

public class HealthRegenUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.HealthRegen;

    private PlayerHealth playerHealth;
    private float currentRegenRate = 0f;
    private float regenTimer = 0f;

    public HealthRegenUpgradeEffect(GameObject player) : base(player)
    {
        playerHealth = player.GetComponent<PlayerHealth>();
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        currentRegenRate += deltaValue;

        if (currentRegenRate > 0f && !IsRegenerationActive())
        {
            StartRegeneration();
        }
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        currentRegenRate -= totalValue;
        currentRegenRate = Mathf.Max(0f, currentRegenRate);

        if (currentRegenRate <= 0f)
        {
            StopRegeneration();
        }
    }

    public override void Reset()
    {
        currentRegenRate = 0f;
        StopRegeneration();
    }

    private bool IsRegenerationActive()
    {
        return currentRegenRate > 0f;
    }

    private void StartRegeneration()
    {
        if (playerHealth != null)
        {
            playerHealth.StartHealthRegeneration(currentRegenRate);
        }
    }

    private void StopRegeneration()
    {
        if (playerHealth != null)
        {
            playerHealth.StopHealthRegeneration();
        }
    }
}

public class ExperienceUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.ExperienceMultiplier;

    private float currentMultiplier = 1f;

    public ExperienceUpgradeEffect(GameObject player) : base(player)
    {
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        currentMultiplier += deltaValue;
        UpdateExperienceMultiplier();
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        currentMultiplier -= totalValue;
        UpdateExperienceMultiplier();
    }

    public override void Reset()
    {
        currentMultiplier = 1f;
        UpdateExperienceMultiplier();
    }

    private void UpdateExperienceMultiplier()
    {
        if (ServiceLocator.TryGet<ScoreSystem>(out var scoreSystem))
        {
            scoreSystem.SetExperienceMultiplier(currentMultiplier);
        }
    }
}

public class CriticalChanceUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.CriticalChance;

    private PlayerCombat playerCombat;
    private float currentCritChance = 0f;

    public CriticalChanceUpgradeEffect(GameObject player) : base(player)
    {
        playerCombat = player.GetComponent<PlayerCombat>();
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        currentCritChance += deltaValue;
        UpdateCriticalChance();
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        currentCritChance -= totalValue;
        currentCritChance = Mathf.Max(0f, currentCritChance);
        UpdateCriticalChance();
    }

    public override void Reset()
    {
        currentCritChance = 0f;
        UpdateCriticalChance();
    }

    private void UpdateCriticalChance()
    {
        if (playerCombat != null)
        {
            playerCombat.SetCriticalChance(currentCritChance);
        }
    }
}

public class CriticalDamageUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.CriticalDamage;

    private PlayerCombat playerCombat;
    private float currentCritMultiplier = 1.5f;

    public CriticalDamageUpgradeEffect(GameObject player) : base(player)
    {
        playerCombat = player.GetComponent<PlayerCombat>();
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        currentCritMultiplier += deltaValue;
        UpdateCriticalDamage();
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        currentCritMultiplier -= totalValue;
        currentCritMultiplier = Mathf.Max(1f, currentCritMultiplier);
        UpdateCriticalDamage();
    }

    public override void Reset()
    {
        currentCritMultiplier = 1.5f;
        UpdateCriticalDamage();
    }

    private void UpdateCriticalDamage()
    {
        if (playerCombat != null)
        {
            playerCombat.SetCriticalDamageMultiplier(currentCritMultiplier);
        }
    }
}