using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
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

    private void Start()
    {
        currentHealth = maxHealth;
        isDead = false;
        totalDamageTaken = 0f;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"[PLAYER HEALTH] Player initialized with {currentHealth}/{maxHealth} health");
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
            Debug.LogError($"[PLAYER HEALTH] *** FRIENDLY FIRE DETECTED *** Source: {source.GetSourceName()}");
            Debug.LogError($"[PLAYER HEALTH] This should be blocked by the damage system!");
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

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            Debug.LogError($"[PLAYER HEALTH] *** PLAYER DEATH *** Killed by: {source.GetSourceName()}");
            LogDeathAnalysis();
            OnPlayerDied?.Invoke();
        }
    }

    private void RecordDamage(DamageRecord record)
    {
        damageHistory.Add(record);
        damageHistory.RemoveAll(r => Time.time - r.timestamp > damageTrackingDuration);
    }

    private void LogDeathAnalysis()
    {
        Debug.LogError("========== PLAYER DEATH ANALYSIS ==========");
        Debug.LogError($"[DEATH] Final health: {currentHealth:F1}/{maxHealth:F1}");
        Debug.LogError($"[DEATH] Total damage taken: {totalDamageTaken:F1}");
        Debug.LogError($"[DEATH] Time of death: {Time.time:F2}s");
        Debug.LogError($"[DEATH] Death position: {transform.position}");

        if (damageHistory.Count > 0)
        {
            var lastDamage = damageHistory[damageHistory.Count - 1];
            Debug.LogError($"[DEATH] Final blow: {lastDamage.damage:F1} from {lastDamage.sourceName} ({lastDamage.sourceType})");
        }

        Debug.LogError("--- Recent Damage History ---");
        var damageByType = new Dictionary<string, float>();

        for (int i = damageHistory.Count - 1; i >= 0; i--)
        {
            var record = damageHistory[i];
            float timeAgo = Time.time - record.timestamp;

            string key = $"{record.sourceType} ({record.sourceTeam})";
            if (damageByType.ContainsKey(key))
                damageByType[key] += record.damage;
            else
                damageByType[key] = record.damage;

            Debug.LogError($"[DEATH] {timeAgo:F1}s ago: {record.damage:F1} from {record.sourceName} at {record.sourcePosition}");
        }

        Debug.LogError("--- Damage Summary ---");
        foreach (var kvp in damageByType)
        {
            float percentage = (kvp.Value / totalDamageTaken) * 100f;
            Debug.LogError($"[DEATH] {kvp.Key}: {kvp.Value:F1} damage ({percentage:F1}%)");
        }

        LogNearbyDamageSources();
        Debug.LogError("==========================================");
    }

    private void LogNearbyDamageSources()
    {
        Debug.LogError("--- Nearby Damage Sources ---");

        var nearbyObjects = Physics2D.OverlapCircleAll(transform.position, 5f);
        foreach (var obj in nearbyObjects)
        {
            if (obj.gameObject == gameObject) continue;

            var damageSource = obj.GetComponent<IDamageSource>();
            if (damageSource != null)
            {
                float distance = Vector2.Distance(transform.position, obj.transform.position);
                Debug.LogError($"[DEATH] Damage Source: {damageSource.GetSourceName()} ({damageSource.GetType().Name}) - Team: {damageSource.GetTeam()}, Damage: {damageSource.GetDamage():F1}, Distance: {distance:F2}");
            }

            var damageable = obj.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float distance = Vector2.Distance(transform.position, obj.transform.position);
                Debug.LogError($"[DEATH] Damageable: {obj.name} - Team: {damageable.GetTeam()}, Alive: {damageable.IsAlive()}, Distance: {distance:F2}");
            }
        }
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
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetTotalDamageTaken() => totalDamageTaken;

    private void OnDestroy()
    {
        Debug.LogError($"[PLAYER HEALTH] *** PLAYER OBJECT DESTROYED ***");
        Debug.LogError($"[PLAYER HEALTH] Final state - Health: {currentHealth:F1}/{maxHealth:F1}, Dead: {isDead}");
        Debug.LogError($"[PLAYER HEALTH] Total damage taken: {totalDamageTaken:F1}");
        Debug.LogError($"[PLAYER HEALTH] Destruction position: {transform.position}");

        if (damageHistory.Count > 0)
        {
            Debug.LogError("--- Last 5 Damage Events ---");
            for (int i = damageHistory.Count - 1; i >= Mathf.Max(0, damageHistory.Count - 5); i--)
            {
                var record = damageHistory[i];
                float timeAgo = Time.time - record.timestamp;
                Debug.LogError($"[PLAYER HEALTH] {timeAgo:F1}s ago: {record.damage:F1} from {record.sourceName} (Team: {record.sourceTeam})");
            }
        }
        else
        {
            Debug.LogError("[PLAYER HEALTH] No damage was recorded - player destroyed without taking damage!");
        }

        LogNearbyDamageSources();
    }

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