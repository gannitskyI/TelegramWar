using UnityEngine;

public static class PlayerCombatExtensions
{
    public static void SetBulletDamage(this PlayerCombat combat, float damage)
    {
        var field = typeof(PlayerCombat).GetField("bulletDamage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(combat, damage);
    }

    public static void SetAttackInterval(this PlayerCombat combat, float interval)
    {
        var field = typeof(PlayerCombat).GetField("attackInterval",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(combat, interval);
    }

    public static void SetAttackRange(this PlayerCombat combat, float range)
    {
        var field = typeof(PlayerCombat).GetField("attackRange",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(combat, range);
    }

    public static void SetCriticalChance(this PlayerCombat combat, float chance)
    {
    }

    public static void SetCriticalDamageMultiplier(this PlayerCombat combat, float multiplier)
    {
    }
}

public static class PlayerHealthExtensions
{
    public static void SetMaxHealth(this PlayerHealth health, float maxHealth)
    {
        var maxHealthField = typeof(PlayerHealth).GetField("maxHealth",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        maxHealthField?.SetValue(health, maxHealth);
    }

    public static void StartHealthRegeneration(this PlayerHealth health, float regenRate)
    {
    }

    public static void StopHealthRegeneration(this PlayerHealth health)
    {
    }
}

public static class ScoreSystemExtensions
{
    public static void SetExperienceMultiplier(this ScoreSystem scoreSystem, float multiplier)
    {
    }
}