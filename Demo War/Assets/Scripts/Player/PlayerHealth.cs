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
    }

    private void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
        totalDamageTaken = 0f;
        Debug.Log($"[PLAYER HEALTH] Player initialized with {currentHealth}/{maxHealth} health");
        Debug.Log($"[PLAYER HEALTH] Invoking initial OnHealthChanged event");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage, IDamageSource source)
    {
        if (isDead || damage < 0)
        {
            Debug.LogWarning($"[PLAYER HEALTH] Damage rejected: isDead={isDead}, damage={damage}");
            return;
        }

        if (source.GetTeam() == DamageTeam.Player)
        {
            Debug.LogWarning($"[PLAYER HEALTH] Friendly fire blocked from: {source.GetSourceName()}");
            return;
        }

        float actualDamage = Mathf.Min(damage, currentHealth);
        float previousHealth = currentHealth;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        totalDamageTaken += actualDamage;

        var damageRecord = new DamageRecord(actualDamage, source);
        RecordDamage(damageRecord);

        Debug.Log($"[PLAYER HEALTH] Damage taken: {actualDamage:F1} from {source.GetSourceName()}");
        Debug.Log($"[PLAYER HEALTH] Health: {previousHealth:F1} ? {currentHealth:F1}");
        Debug.Log($"[PLAYER HEALTH] Source team: {source.GetTeam()}, Type: {source.GetType().Name}");

        Debug.Log($"[PLAYER HEALTH] Invoking OnHealthChanged event: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            Debug.Log($"[PLAYER HEALTH] Player died. Killed by: {source.GetSourceName()}");
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
        if (amount < 0)
        {
            Debug.LogWarning($"[PLAYER HEALTH] Heal called with negative value: {amount}");
            return;
        }

        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        Debug.Log($"[PLAYER HEALTH] Healing: +{amount:F1} ? {previousHealth:F1} ? {currentHealth:F1}");
        Debug.Log($"[PLAYER HEALTH] Invoking OnHealthChanged event after heal");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        float previousHealth = currentHealth;
        currentHealth = maxHealth;
        isDead = false;
        totalDamageTaken = 0f;
        damageHistory.Clear();

        Debug.Log($"[PLAYER HEALTH] Health reset: {previousHealth:F1} ? {currentHealth:F1}");
        Debug.Log($"[PLAYER HEALTH] Invoking OnHealthChanged event after reset");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
    public bool IsAlive() => !isDead;
    public DamageTeam GetTeam() => DamageTeam.Player;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetTotalDamageTaken() => totalDamageTaken;

    [ContextMenu("Log Damage History")]
    private void DebugLogDamageHistory()
    {
        Debug.Log($"[PLAYER HEALTH] Current health: {currentHealth:F1}/{maxHealth:F1}");
        Debug.Log($"[PLAYER HEALTH] Total damage taken: {totalDamageTaken:F1}");
        Debug.Log($"[PLAYER HEALTH] Damage events: {damageHistory.Count}");

        foreach (var record in damageHistory)
        {
            float timeAgo = Time.time - record.timestamp;
            Debug.Log($"[PLAYER HEALTH] {timeAgo:F1}s ago: {record.damage:F1} from {record.sourceName} (Team: {record.sourceTeam})");
        }
    }

    [ContextMenu("Test Damage (10)")]
    private void DebugTestDamage()
    {
        var testSource = new TestDamageSource("Debug Test", DamageTeam.Enemy, 10f, transform.position);
        TakeDamage(10f, testSource);
    }
}

public class TestDamageSource : IDamageSource
{
    private string sourceName;
    private DamageTeam team;
    private float damage;
    private Vector3 position;

    public TestDamageSource(string name, DamageTeam sourceTeam, float sourceDamage, Vector3 sourcePosition)
    {
        sourceName = name;
        team = sourceTeam;
        damage = sourceDamage;
        position = sourcePosition;
    }

    public float GetDamage() => damage;
    public DamageTeam GetTeam() => team;
    public string GetSourceName() => sourceName;
    public GameObject GetSourceObject() => null;
    public Vector3 GetSourcePosition() => position;
}