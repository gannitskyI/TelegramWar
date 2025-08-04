using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    private float currentHealth;
    private float maxHealth;
    private bool isDead;

    [SerializeField] private PlayerStats playerStats;

    public event Action<float, float> OnHealthChanged;
    public event Action OnPlayerDied;

    private List<DamageRecord> damageHistory = new List<DamageRecord>();
    private float damageTrackingDuration = 10f;
    private float totalDamageTaken = 0f;

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
        if (playerStats == null)
        {
            ServiceLocator.TryGet<PlayerStats>(out playerStats);
        }

        if (playerStats == null)
        {
            Debug.LogError("PlayerStats не найден! Ќазначь ScriptableObject через инспектор или через ServiceLocator.");
            enabled = false;
            return;
        }

        maxHealth = playerStats.BaseMaxHealth;
        currentHealth = maxHealth;
        isDead = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        playerStats.OnStatsChanged += OnStatsChanged;
    }

    private void OnStatsChanged(PlayerStats stats)
    {
        maxHealth = stats.FinalMaxHealth;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage, IDamageSource source)
    {
        if (isDead || damage < 0) return;
        if (source.GetTeam() == DamageTeam.Player) return;
        float actualDamage = Mathf.Min(damage, currentHealth);
        currentHealth = Mathf.Max(0, currentHealth - actualDamage);
        totalDamageTaken += actualDamage;
        damageHistory.Add(new DamageRecord(actualDamage, source));
        damageHistory.RemoveAll(r => Time.time - r.timestamp > damageTrackingDuration);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            OnPlayerDied?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (amount < 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        maxHealth = playerStats.BaseMaxHealth;
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
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= OnStatsChanged;
        }
        this.CleanupEvents();
    }
}
