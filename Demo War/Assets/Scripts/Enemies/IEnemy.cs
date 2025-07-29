public interface IEnemy
{
    void Initialize(EnemyConfig config, System.Action onReturnToPool = null);
    void ResetState();
}