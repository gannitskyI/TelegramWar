using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    private bool isDead;

    public event Action<float, float> OnHealthChanged;
    public event Action OnPlayerDied;

    private struct DamageRecord
    {
        public float damage;
        public string sourceName;
        public string sourceType;
        public DamageTeam sourceTeam;
        public Vector3 sourcePosition;
        public float timestamp;

        public DamageRecord(float damage, IDamageSource source)
        {
            this.damage = damage;
            this.sourceName = source.GetSourceName();
            this.sourceType = source.GetType().Name;
            this.sourceTeam = source.GetTeam();
            this.sourcePosition = source.GetSourcePosition();
            this.timestamp = Time.time;
        }
    }

    private List<DamageRecord> damageHistory = new List<DamageRecord>();
    private float damageTrackingDuration = 10f;
    private float totalDamageTaken = 0f;

    private void Awake()
    {
        var existingDamageReceiver = GetComponent<PlayerDamageReceiver>();
        if (existingDamageReceiver != null)
        {
            DestroyImmediate(existingDamageReceiver);
        }
        this.RegisterEventCleanup(() =>
        {
            OnHealthChanged = null;
            OnPlayerDied = null;
        });
    }

    private void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
        totalDamageTaken = 0f;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage, IDamageSource source)
    {
        if (isDead || damage < 0) return;
        if (source.GetTeam() == DamageTeam.Player) return;

        float actualDamage = Mathf.Min(damage, currentHealth);
        currentHealth = Mathf.Max(0, currentHealth - damage);
        totalDamageTaken += actualDamage;
        RecordDamage(new DamageRecord(actualDamage, source));
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            OnPlayerDied?.Invoke();
        }
    }

    private void RecordDamage(DamageRecord record)
    {
        damageHistory.Add(record);
        damageHistory.RemoveAll(r => Time.time - r.timestamp > damageTrackingDuration);
    }

    public void Heal(float amount)
    {
        if (amount < 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        totalDamageTaken = 0f;
        damageHistory.Clear();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
    public bool IsAlive() => !isDead;
    public DamageTeam GetTeam() => DamageTeam.Player;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetTotalDamageTaken() => totalDamageTaken;

    private void OnDestroy()
    {
        this.CleanupEvents();
    }
}
