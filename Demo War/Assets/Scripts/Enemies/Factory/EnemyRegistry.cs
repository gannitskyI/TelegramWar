using System.Collections.Generic;
using UnityEngine;

public class EnemyRegistry : MonoBehaviour
{
    private static EnemyRegistry instance;
    public static EnemyRegistry Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[EnemyRegistry]");
                instance = go.AddComponent<EnemyRegistry>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private const float GRID_CELL_SIZE = 8f;
    private readonly Dictionary<Vector2Int, HashSet<EnemyDamageReceiver>> grid = new();
    private readonly HashSet<EnemyDamageReceiver> allEnemies = new();

    public void RegisterEnemy(EnemyDamageReceiver enemy)
    {
        if (enemy == null) return;
        allEnemies.Add(enemy);
        var cell = GetCell(enemy.transform.position);
        if (!grid.TryGetValue(cell, out var set))
        {
            set = new HashSet<EnemyDamageReceiver>();
            grid[cell] = set;
        }
        set.Add(enemy);
    }

    public void UnregisterEnemy(EnemyDamageReceiver enemy)
    {
        if (enemy == null) return;
        allEnemies.Remove(enemy);
        var cell = GetCell(enemy.transform.position);
        if (grid.TryGetValue(cell, out var set))
        {
            set.Remove(enemy);
            if (set.Count == 0)
                grid.Remove(cell);
        }
    }

    public void UpdateEnemyCell(EnemyDamageReceiver enemy, Vector3 prevPos)
    {
        if (enemy == null) return;
        var oldCell = GetCell(prevPos);
        var newCell = GetCell(enemy.transform.position);
        if (oldCell != newCell)
        {
            if (grid.TryGetValue(oldCell, out var oldSet))
            {
                oldSet.Remove(enemy);
                if (oldSet.Count == 0) grid.Remove(oldCell);
            }
            if (!grid.TryGetValue(newCell, out var newSet))
            {
                newSet = new HashSet<EnemyDamageReceiver>();
                grid[newCell] = newSet;
            }
            newSet.Add(enemy);
        }
    }

    public List<EnemyDamageReceiver> GetEnemiesInRange(Vector3 position, float range)
    {
        var result = new List<EnemyDamageReceiver>();
        float rangeSqr = range * range;
        Vector2Int minCell = GetCell(position - Vector3.one * range);
        Vector2Int maxCell = GetCell(position + Vector3.one * range);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var cell = new Vector2Int(x, y);
                if (grid.TryGetValue(cell, out var set))
                {
                    foreach (var enemy in set)
                    {
                        if (enemy != null && enemy.IsAlive())
                        {
                            float distSqr = (enemy.transform.position - position).sqrMagnitude;
                            if (distSqr <= rangeSqr)
                            {
                                result.Add(enemy);
                            }
                        }
                    }
                }
            }
        }
        return result;
    }

    public EnemyDamageReceiver FindClosestEnemy(Vector3 position, float maxRange)
    {
        float minDistSqr = maxRange * maxRange;
        EnemyDamageReceiver closest = null;
        Vector2Int minCell = GetCell(position - Vector3.one * maxRange);
        Vector2Int maxCell = GetCell(position + Vector3.one * maxRange);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                var cell = new Vector2Int(x, y);
                if (grid.TryGetValue(cell, out var set))
                {
                    foreach (var enemy in set)
                    {
                        if (enemy != null && enemy.IsAlive())
                        {
                            float distSqr = (enemy.transform.position - position).sqrMagnitude;
                            if (distSqr < minDistSqr)
                            {
                                minDistSqr = distSqr;
                                closest = enemy;
                            }
                        }
                    }
                }
            }
        }
        return closest;
    }

    private Vector2Int GetCell(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / GRID_CELL_SIZE);
        int y = Mathf.FloorToInt(pos.y / GRID_CELL_SIZE);
        return new Vector2Int(x, y);
    }

    public void Clear()
    {
        allEnemies.Clear();
        grid.Clear();
    }

    // Если враги передвигаются не через Rigidbody2D.position, 
    // обязательно вызывай UpdateEnemyCell(enemy, prevPosition) из EnemyBehaviour!
}
