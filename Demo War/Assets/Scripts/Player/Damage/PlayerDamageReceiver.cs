using UnityEngine;
  

public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("PlayerDamageReceiver: No PlayerHealth component found!");
        }
    }

    public void TakeDamage(float damage, IDamageSource source)
    {
        if (playerHealth == null || !IsAlive()) return;

        if (source.GetTeam() == DamageTeam.Player)
        {
            Debug.LogWarning($"[DAMAGE SYSTEM] Player damage blocked from friendly source: {source.GetSourceName()}");
            return;
        }

        Debug.Log($"[DAMAGE SYSTEM] Player taking {damage} damage from {source.GetSourceName()} (Team: {source.GetTeam()})");
        playerHealth.TakeDamage(damage, source);
    }

    public bool IsAlive()
    {
        return playerHealth != null && !playerHealth.IsDead();
    }

    public DamageTeam GetTeam()
    {
        return DamageTeam.Player;
    }
}

 