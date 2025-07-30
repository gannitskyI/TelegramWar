public interface IEnemy
{
    void Initialize(EnemyConfig config, System.Action onReturnToPool = null);
    void InitializeForPool(EnemyConfig config);
    void ResetState();
    void TakeDamage(float damageAmount);
    bool IsAlive();
    bool IsInitialized();
    float GetCurrentHealth();
    float GetMaxHealth();
    EnemyConfig GetConfig();
}