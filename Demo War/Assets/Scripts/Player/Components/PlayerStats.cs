using UnityEngine;
using System;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Game/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Base Combat Stats")]
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseAttackSpeed = 2f;
    [SerializeField] private float baseAttackRange = 5f;
    [SerializeField] private float baseBulletSpeed = 15f;

    [Header("Base Movement Stats")]
    [SerializeField] private float baseMoveSpeed = 5f;

    [Header("Base Health Stats")]
    [SerializeField] public float baseMaxHealth = 100f;
    [SerializeField] private float baseHealthRegen = 0f;

    [Header("Runtime Multipliers")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float attackSpeedMultiplier = 1f;
    [SerializeField] private float attackRangeMultiplier = 1f;
    [SerializeField] private float moveSpeedMultiplier = 1f;
    [SerializeField] private float maxHealthMultiplier = 1f;
    [SerializeField] private float healthRegenBonus = 0f;

    public event Action<PlayerStats> OnStatsChanged;

    public float BaseDamage => baseDamage;
    public float BaseAttackSpeed => baseAttackSpeed;
    public float BaseAttackRange => baseAttackRange;
    public float BaseBulletSpeed => baseBulletSpeed;
    public float BaseMoveSpeed => baseMoveSpeed;
    public float BaseMaxHealth => baseMaxHealth;
    public float BaseHealthRegen => baseHealthRegen;

    public float FinalDamage => baseDamage * damageMultiplier;
    public float FinalAttackSpeed => baseAttackSpeed * attackSpeedMultiplier;
    public float FinalAttackRange => baseAttackRange * attackRangeMultiplier;
    public float FinalBulletSpeed => baseBulletSpeed;
    public float FinalMoveSpeed => baseMoveSpeed * moveSpeedMultiplier;
    public float FinalMaxHealth => baseMaxHealth * maxHealthMultiplier;
    public float FinalHealthRegen => baseHealthRegen + healthRegenBonus;
    public float AttackInterval => 1f / FinalAttackSpeed;

    public void SetDamageMultiplier(float multiplier) { damageMultiplier = multiplier; OnStatsChanged?.Invoke(this); }
    public void SetAttackSpeedMultiplier(float multiplier) { attackSpeedMultiplier = multiplier; OnStatsChanged?.Invoke(this); }
    public void SetAttackRangeMultiplier(float multiplier) { attackRangeMultiplier = multiplier; OnStatsChanged?.Invoke(this); }
    public void SetMoveSpeedMultiplier(float multiplier) { moveSpeedMultiplier = multiplier; OnStatsChanged?.Invoke(this); }
    public void SetMaxHealthMultiplier(float multiplier) { maxHealthMultiplier = multiplier; OnStatsChanged?.Invoke(this); }
    public void SetHealthRegenBonus(float bonus) { healthRegenBonus = bonus; OnStatsChanged?.Invoke(this); }

    public void AddDamageMultiplier(float additionalMultiplier) { damageMultiplier += additionalMultiplier; OnStatsChanged?.Invoke(this); }
    public void AddAttackSpeedMultiplier(float additionalMultiplier) { attackSpeedMultiplier += additionalMultiplier; OnStatsChanged?.Invoke(this); }
    public void AddAttackRangeMultiplier(float additionalMultiplier) { attackRangeMultiplier += additionalMultiplier; OnStatsChanged?.Invoke(this); }
    public void AddMoveSpeedMultiplier(float additionalMultiplier) { moveSpeedMultiplier += additionalMultiplier; OnStatsChanged?.Invoke(this); }
    public void AddMaxHealthMultiplier(float additionalMultiplier) { maxHealthMultiplier += additionalMultiplier; OnStatsChanged?.Invoke(this); }
    public void AddHealthRegenBonus(float additionalBonus) { healthRegenBonus += additionalBonus; OnStatsChanged?.Invoke(this); }

    public void ResetToBase()
    {
        damageMultiplier = 1f;
        attackSpeedMultiplier = 1f;
        attackRangeMultiplier = 1f;
        moveSpeedMultiplier = 1f;
        maxHealthMultiplier = 1f;
        healthRegenBonus = 0f;
        OnStatsChanged?.Invoke(this);
    }
}
