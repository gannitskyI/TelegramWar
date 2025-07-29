using UnityEngine;

[CreateAssetMenu(fileName = "SystemsConfiguration", menuName = "Game/Systems Configuration")]
public class SystemsConfiguration : ScriptableObject
{
    [Header("Game Settings")]
    public float roundDuration = 30f;
    public int startingLives = 3;

    [Header("Spawn Settings")]
    public float initialSpawnDelay = 2f;
    public float spawnInterval = 1f;
    public int enemiesPerWave = 5;
    public float difficultyIncreaseRate = 0.1f;

    [Header("Player Settings")]
    public float playerMoveSpeed = 5f;
    public float playerHealth = 100f;
    public float playerDamage = 10f;

    [Header("Performance Settings")]
    public int maxEnemiesOnScreen = 50;
    public int poolInitialSize = 20;
}