using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Game/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    public float maxHealth = 100f;
    public float moveSpeed = 5f;
    public float damage = 10f;
    public int experienceDrop = 10; // Добавляем поле для опыта
}