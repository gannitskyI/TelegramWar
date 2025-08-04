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
    protected PlayerStats playerStats;

    public abstract UpgradeType TargetType { get; }

    protected BaseUpgradeEffect(GameObject player)
    {
        playerObject = player ?? throw new System.ArgumentNullException(nameof(player));

        if (ServiceLocator.TryGet<PlayerStats>(out var stats))
        {
            playerStats = stats;
        }
        else
        {
            Debug.LogWarning($"{GetType().Name}: PlayerStats not found in ServiceLocator");
        }
    }

    public abstract void Apply(UpgradeConfig config, float deltaValue, int currentLevel);
    public abstract void Remove(UpgradeConfig config, float totalValue);
    public abstract void Reset();
}

public class DamageUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.Damage;

    private float accumulatedBonus = 0f;

    public DamageUpgradeEffect(GameObject player) : base(player) { }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        if (playerStats == null) return;

        accumulatedBonus += deltaValue;
        playerStats.SetDamageMultiplier(1f + accumulatedBonus);

        Debug.Log($"Damage upgraded: +{deltaValue:F2} (total bonus: {accumulatedBonus:F2}, final damage: {playerStats.FinalDamage:F1})");
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        if (playerStats == null) return;

        accumulatedBonus -= totalValue;
        accumulatedBonus = Mathf.Max(0f, accumulatedBonus);
        playerStats.SetDamageMultiplier(1f + accumulatedBonus);
    }

    public override void Reset()
    {
        if (playerStats == null) return;

        accumulatedBonus = 0f;
        playerStats.SetDamageMultiplier(1f);
    }
}

public class AttackSpeedUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.AttackSpeed;

    private float accumulatedBonus = 0f;

    public AttackSpeedUpgradeEffect(GameObject player) : base(player) { }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        if (playerStats == null) return;

        accumulatedBonus += deltaValue;
        playerStats.SetAttackSpeedMultiplier(1f + accumulatedBonus);

        Debug.Log($"Attack speed upgraded: +{deltaValue:F2} (total bonus: {accumulatedBonus:F2}, final speed: {playerStats.FinalAttackSpeed:F1})");
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        if (playerStats == null) return;

        accumulatedBonus -= totalValue;
        accumulatedBonus = Mathf.Max(0f, accumulatedBonus);
        playerStats.SetAttackSpeedMultiplier(1f + accumulatedBonus);
    }

    public override void Reset()
    {
        if (playerStats == null) return;

        accumulatedBonus = 0f;
        playerStats.SetAttackSpeedMultiplier(1f);
    }
}

public class AttackRangeUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.AttackRange;

    private float accumulatedBonus = 0f;

    public AttackRangeUpgradeEffect(GameObject player) : base(player) { }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        if (playerStats == null) return;

        accumulatedBonus += deltaValue;
        playerStats.SetAttackRangeMultiplier(1f + accumulatedBonus);

        Debug.Log($"Attack range upgraded: +{deltaValue:F2} (total bonus: {accumulatedBonus:F2}, final range: {playerStats.FinalAttackRange:F1})");
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        if (playerStats == null) return;

        accumulatedBonus -= totalValue;
        accumulatedBonus = Mathf.Max(0f, accumulatedBonus);
        playerStats.SetAttackRangeMultiplier(1f + accumulatedBonus);
    }

    public override void Reset()
    {
        if (playerStats == null) return;

        accumulatedBonus = 0f;
        playerStats.SetAttackRangeMultiplier(1f);
    }
}

public class MoveSpeedUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.MoveSpeed;

    private PlayerMovement playerMovement;
    private float accumulatedBonus = 0f;

    public MoveSpeedUpgradeEffect(GameObject player) : base(player)
    {
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        if (playerStats == null || playerMovement == null) return;

        accumulatedBonus += deltaValue;
        playerStats.SetMoveSpeedMultiplier(1f + accumulatedBonus);
        playerMovement.SetMoveSpeed(playerStats.FinalMoveSpeed);

        Debug.Log($"Move speed upgraded: +{deltaValue:F2} (total bonus: {accumulatedBonus:F2}, final speed: {playerStats.FinalMoveSpeed:F1})");
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        if (playerStats == null || playerMovement == null) return;

        accumulatedBonus -= totalValue;
        accumulatedBonus = Mathf.Max(0f, accumulatedBonus);
        playerStats.SetMoveSpeedMultiplier(1f + accumulatedBonus);
        playerMovement.SetMoveSpeed(playerStats.FinalMoveSpeed);
    }

    public override void Reset()
    {
        if (playerStats == null || playerMovement == null) return;

        accumulatedBonus = 0f;
        playerStats.SetMoveSpeedMultiplier(1f);
        playerMovement.SetMoveSpeed(playerStats.FinalMoveSpeed);
    }
}

public class HealthUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.Health;

    private PlayerHealth playerHealth;
    private float accumulatedBonus = 0f;

    public HealthUpgradeEffect(GameObject player) : base(player)
    {
        playerHealth = player.GetComponent<PlayerHealth>();
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        if (playerStats == null || playerHealth == null) return;

        accumulatedBonus += deltaValue;
        playerStats.SetMaxHealthMultiplier(1f + accumulatedBonus);

        float newMaxHealth = playerStats.FinalMaxHealth;
        float healthIncrease = newMaxHealth - playerHealth.GetMaxHealth();

        playerHealth.SetMaxHealth(newMaxHealth);
        if (healthIncrease > 0)
        {
            playerHealth.Heal(healthIncrease);
        }

        Debug.Log($"Max health upgraded: +{deltaValue:F2} (total bonus: {accumulatedBonus:F2}, final health: {newMaxHealth:F1})");
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        if (playerStats == null || playerHealth == null) return;

        accumulatedBonus -= totalValue;
        accumulatedBonus = Mathf.Max(0f, accumulatedBonus);
        playerStats.SetMaxHealthMultiplier(1f + accumulatedBonus);

        playerHealth.SetMaxHealth(playerStats.FinalMaxHealth);
    }

    public override void Reset()
    {
        if (playerStats == null || playerHealth == null) return;

        accumulatedBonus = 0f;
        playerStats.SetMaxHealthMultiplier(1f);
        playerHealth.SetMaxHealth(playerStats.FinalMaxHealth);
    }
}

public class HealthRegenUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.HealthRegen;

    private PlayerHealth playerHealth;
    private float accumulatedBonus = 0f;

    public HealthRegenUpgradeEffect(GameObject player) : base(player)
    {
        playerHealth = player.GetComponent<PlayerHealth>();
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        if (playerStats == null) return;

        accumulatedBonus += deltaValue;
        playerStats.SetHealthRegenBonus(accumulatedBonus);

        if (playerHealth != null && playerStats.FinalHealthRegen > 0f)
        {
            playerHealth.StartHealthRegeneration(playerStats.FinalHealthRegen);
        }

        Debug.Log($"Health regen upgraded: +{deltaValue:F2} (total bonus: {accumulatedBonus:F2}, final regen: {playerStats.FinalHealthRegen:F1})");
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        if (playerStats == null) return;

        accumulatedBonus -= totalValue;
        accumulatedBonus = Mathf.Max(0f, accumulatedBonus);
        playerStats.SetHealthRegenBonus(accumulatedBonus);

        if (playerHealth != null)
        {
            if (playerStats.FinalHealthRegen > 0f)
            {
                playerHealth.StartHealthRegeneration(playerStats.FinalHealthRegen);
            }
            else
            {
                playerHealth.StopHealthRegeneration();
            }
        }
    }

    public override void Reset()
    {
        if (playerStats == null) return;

        accumulatedBonus = 0f;
        playerStats.SetHealthRegenBonus(0f);

        if (playerHealth != null)
        {
            playerHealth.StopHealthRegeneration();
        }
    }
}

public class ExperienceUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.ExperienceMultiplier;

    private float accumulatedBonus = 0f;

    public ExperienceUpgradeEffect(GameObject player) : base(player) { }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        accumulatedBonus += deltaValue;
        UpdateExperienceMultiplier();

        Debug.Log($"Experience multiplier upgraded: +{deltaValue:F2} (total bonus: {accumulatedBonus:F2})");
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        accumulatedBonus -= totalValue;
        accumulatedBonus = Mathf.Max(0f, accumulatedBonus);
        UpdateExperienceMultiplier();
    }

    public override void Reset()
    {
        accumulatedBonus = 0f;
        UpdateExperienceMultiplier();
    }

    private void UpdateExperienceMultiplier()
    {
        if (ServiceLocator.TryGet<ScoreSystem>(out var scoreSystem))
        {
            scoreSystem.SetExperienceMultiplier(1f + accumulatedBonus);
        }
    }
}

public class CriticalChanceUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.CriticalChance;

    private PlayerCombat playerCombat;
    private float accumulatedBonus = 0f;

    public CriticalChanceUpgradeEffect(GameObject player) : base(player)
    {
        playerCombat = player.GetComponent<PlayerCombat>();
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        accumulatedBonus += deltaValue;
        UpdateCriticalChance();

        Debug.Log($"Critical chance upgraded: +{deltaValue:F2} (total: {accumulatedBonus:F2})");
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        accumulatedBonus -= totalValue;
        accumulatedBonus = Mathf.Max(0f, accumulatedBonus);
        UpdateCriticalChance();
    }

    public override void Reset()
    {
        accumulatedBonus = 0f;
        UpdateCriticalChance();
    }

    private void UpdateCriticalChance()
    {
        if (playerCombat != null)
        {
            playerCombat.SetCriticalChance(accumulatedBonus);
        }
    }
}

public class CriticalDamageUpgradeEffect : BaseUpgradeEffect
{
    public override UpgradeType TargetType => UpgradeType.CriticalDamage;

    private PlayerCombat playerCombat;
    private float accumulatedBonus = 0f;
    private const float baseCritMultiplier = 1.5f;

    public CriticalDamageUpgradeEffect(GameObject player) : base(player)
    {
        playerCombat = player.GetComponent<PlayerCombat>();
    }

    public override void Apply(UpgradeConfig config, float deltaValue, int currentLevel)
    {
        accumulatedBonus += deltaValue;
        UpdateCriticalDamage();

        Debug.Log($"Critical damage upgraded: +{deltaValue:F2} (total multiplier: {baseCritMultiplier + accumulatedBonus:F2})");
    }

    public override void Remove(UpgradeConfig config, float totalValue)
    {
        accumulatedBonus -= totalValue;
        accumulatedBonus = Mathf.Max(0f, accumulatedBonus);
        UpdateCriticalDamage();
    }

    public override void Reset()
    {
        accumulatedBonus = 0f;
        UpdateCriticalDamage();
    }

    private void UpdateCriticalDamage()
    {
        if (playerCombat != null)
        {
            playerCombat.SetCriticalDamageMultiplier(baseCritMultiplier + accumulatedBonus);
        }
    }
}