using System.Threading.Tasks;
using UnityEngine;

public class EnemyFactory
{
    private AddressableManager addressableManager;
    private EnemyPool enemyPool;
    private bool isWarmedUp = false;

    public EnemyFactory(AddressableManager addressableManager)
    {
        this.addressableManager = addressableManager;
        this.enemyPool = new EnemyPool();

        // Устанавливаем ссылку на фабрику в пуле
        enemyPool.SetFactory(this);

        // НЕ запускаем прогрев автоматически при создании фабрики
      
    }

    /// <summary>
    /// Прогрев пулов по требованию (вызывается при первом спавне)
    /// </summary>
    public async Task EnsureWarmedUp()
    {
        if (isWarmedUp) return;
 
        await WarmupPoolsAsync();
        isWarmedUp = true;
    }

    /// <summary>
    /// Создать врага определенного типа
    /// </summary>
    public async Task<GameObject> CreateEnemy(string enemyType, Vector3 position)
    {
        if (string.IsNullOrEmpty(enemyType))
        {
            Debug.LogError("Enemy type is null or empty!");
            return null;
        }

        // Убеждаемся что пулы прогреты
        await EnsureWarmedUp();

        // Сначала проверяем пул
        var pooledEnemy = enemyPool.Get(enemyType);
        if (pooledEnemy != null)
        {
            pooledEnemy.transform.position = position;
            pooledEnemy.SetActive(true);

            // Переинициализируем врага
            var enemyComponent = pooledEnemy.GetComponent<EnemyBehaviour>();
            if (enemyComponent != null)
            {
                var config = await LoadEnemyConfig(enemyType);
                if (config != null)
                {
                    enemyComponent.Initialize(config, () => ReturnToPool(pooledEnemy, enemyType));
                    return pooledEnemy;
                }
                else
                {
                    Debug.LogError($"Failed to load config for enemy type: {enemyType}");
                    ReturnToPool(pooledEnemy, enemyType);
                    return null;
                }
            }
            else
            {
                Debug.LogError($"EnemyBehaviour component not found on pooled enemy: {enemyType}");
                ReturnToPool(pooledEnemy, enemyType);
                return null;
            }
        }

        // Если в пуле нет, создаем новый
        return await CreateNewEnemy(enemyType, position);
    }

    /// <summary>
    /// Создать нового врага (не из пула)
    /// </summary>
    private async Task<GameObject> CreateNewEnemy(string enemyType, Vector3 position)
    {
        var prefabKey = $"Enemy_{enemyType}";
        var enemy = await addressableManager.InstantiateAsync(prefabKey, position);

        if (enemy == null)
        {
            Debug.LogWarning($"Failed to load enemy prefab: {prefabKey}, creating fallback enemy");
            return CreateFallbackEnemy(enemyType, position);
        }

        // Настраиваем врага
        var enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();
        if (enemyBehaviour == null)
        {
            enemyBehaviour = enemy.AddComponent<EnemyBehaviour>();
        }

        var enemyConfig = await LoadEnemyConfig(enemyType);
        if (enemyConfig != null)
        {
            enemyBehaviour.Initialize(enemyConfig, () => ReturnToPool(enemy, enemyType));
        }
        else
        {
            Debug.LogWarning($"Failed to load config for enemy type: {enemyType}, using default config");
            var defaultConfig = CreateDefaultEnemyConfig(enemyType);
            enemyBehaviour.Initialize(defaultConfig, () => ReturnToPool(enemy, enemyType));
        }

        return enemy;
    }

    /// <summary>
    /// Создать fallback врага если префаб не загрузился
    /// </summary>
    private GameObject CreateFallbackEnemy(string enemyType, Vector3 position)
    {
        var enemyGO = new GameObject($"Fallback_{enemyType}");
        enemyGO.transform.position = position;

        // Визуал
        var renderer = enemyGO.AddComponent<SpriteRenderer>();

        // Создаем простой спрайт врага
        var texture = new Texture2D(32, 32);
        var colors = new Color[32 * 32];
        Color enemyColor = GetEnemyColor(enemyType);

        for (int i = 0; i < colors.Length; i++)
        {
            float x = (i % 32) - 16f;
            float y = (i / 32) - 16f;
            float distance = Mathf.Sqrt(x * x + y * y);
            colors[i] = distance < 14f ? enemyColor : Color.clear;
        }
        texture.SetPixels(colors);
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        renderer.sprite = sprite;

        // Физика
        var rb2d = enemyGO.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;
        rb2d.linearDamping = 1f;

        // Коллайдер
        var collider = enemyGO.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.4f;

        // Компонент поведения
        var enemyBehaviour = enemyGO.AddComponent<EnemyBehaviour>();
        var defaultConfig = CreateDefaultEnemyConfig(enemyType);
        enemyBehaviour.Initialize(defaultConfig, () => ReturnToPool(enemyGO, enemyType));

        // Тег
        enemyGO.tag = "Enemy";

        Debug.Log($"Created fallback enemy: {enemyType}");
        return enemyGO;
    }

    private Color GetEnemyColor(string enemyType)
    {
        switch (enemyType)
        {
            case "weak": return Color.gray;
            case "normal": return Color.red;
            case "strong": return Color.magenta;
            case "fast": return Color.yellow;
            case "tank": return Color.black;
            default: return Color.red;
        }
    }

    /// <summary>
    /// Создать врага для пула (без активации)
    /// </summary>
    public async Task<GameObject> CreateEnemyForPool(string enemyType)
    {
        var prefabKey = $"Enemy_{enemyType}";
        var enemy = await addressableManager.InstantiateAsync(prefabKey, Vector3.zero);

        if (enemy == null)
        {
            // Если не удалось загрузить префаб, создаем fallback для пула
            enemy = CreateFallbackEnemy(enemyType, Vector3.zero);
        }

        // Добавляем компонент если его нет
        var enemyBehaviour = enemy.GetComponent<EnemyBehaviour>();
        if (enemyBehaviour == null)
        {
            enemyBehaviour = enemy.AddComponent<EnemyBehaviour>();
        }

        // Загружаем конфигурацию, но не активируем врага
        var config = await LoadEnemyConfig(enemyType);
        if (config == null)
        {
            config = CreateDefaultEnemyConfig(enemyType);
        }

        enemyBehaviour.InitializeForPool(config);
        enemy.SetActive(false); // Деактивируем для пула
        return enemy;
    }

    /// <summary>
    /// Загрузить конфигурацию врага
    /// </summary>
    private async Task<EnemyConfig> LoadEnemyConfig(string enemyType)
    {
        var configKey = $"Enemy_{enemyType}Config";

        try
        {
            return await addressableManager.LoadAssetAsync<EnemyConfig>(configKey);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to load enemy config {configKey}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Создать конфигурацию по умолчанию если не удалось загрузить
    /// </summary>
    private EnemyConfig CreateDefaultEnemyConfig(string enemyType)
    {
        var config = ScriptableObject.CreateInstance<EnemyConfig>();

        switch (enemyType)
        {
            case "weak":
                config.maxHealth = 50f;
                config.moveSpeed = 2f;
                config.damage = 5f;
                config.experienceDrop = 5;
                break;
            case "normal":
                config.maxHealth = 100f;
                config.moveSpeed = 3f;
                config.damage = 10f;
                config.experienceDrop = 10;
                break;
            case "strong":
                config.maxHealth = 200f;
                config.moveSpeed = 2f;
                config.damage = 20f;
                config.experienceDrop = 20;
                break;
            case "fast":
                config.maxHealth = 75f;
                config.moveSpeed = 5f;
                config.damage = 8f;
                config.experienceDrop = 15;
                break;
            case "tank":
                config.maxHealth = 400f;
                config.moveSpeed = 1f;
                config.damage = 30f;
                config.experienceDrop = 50;
                break;
            default:
                config.maxHealth = 100f;
                config.moveSpeed = 3f;
                config.damage = 10f;
                config.experienceDrop = 10;
                break;
        }

        return config;
    }

    /// <summary>
    /// Предзагрузка пулов (теперь вызывается по требованию)
    /// </summary>
    private async Task WarmupPoolsAsync()
    {
        // Основные типы врагов
        string[] enemyTypes = { "weak", "normal", "strong", "fast", "tank" };

        foreach (string enemyType in enemyTypes)
        {
            try
            {
                await enemyPool.WarmupAsync(enemyType, 3); // Уменьшаем количество для быстрого прогрева
                await Task.Delay(50); // Меньшая задержка между типами
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to warmup pool for {enemyType}: {e.Message}");
            }
        }
 
    }

    /// <summary>
    /// Вернуть врага в пул
    /// </summary>
    private void ReturnToPool(GameObject enemy, string enemyType)
    {
        if (enemy != null)
        {
            enemyPool.Return(enemy, enemyType);
        }
    }

    /// <summary>
    /// Получить случайный тип врага в зависимости от волны
    /// </summary>
    public string GetRandomEnemyType(int waveNumber)
    {
        if (waveNumber <= 0)
        {
            Debug.LogWarning("Wave number should be greater than 0, returning default enemy type");
            return "weak";
        }

        if (waveNumber <= 2)
        {
            return Random.Range(0, 100) < 80 ? "weak" : "normal";
        }
        else if (waveNumber <= 5)
        {
            int rand = Random.Range(0, 100);
            if (rand < 40) return "weak";
            if (rand < 70) return "normal";
            if (rand < 90) return "fast";
            return "strong";
        }
        else
        {
            int rand = Random.Range(0, 100);
            if (rand < 20) return "normal";
            if (rand < 40) return "strong";
            if (rand < 60) return "fast";
            if (rand < 80) return "tank";
            return "strong"; // Fallback
        }
    }

    /// <summary>
    /// Очистить все пулы
    /// </summary>
    public void ClearAllPools()
    {
        enemyPool?.ClearAll();
        isWarmedUp = false;
    }

    /// <summary>
    /// Получить информацию о пулах для дебага
    /// </summary>
    public string GetDebugInfo()
    {
        return enemyPool?.GetPoolInfo() ?? "EnemyPool not initialized";
    }

    /// <summary>
    /// Освободить ресурсы
    /// </summary>
    public void Cleanup()
    {
        ClearAllPools();
        enemyPool = null;
        addressableManager = null;
        isWarmedUp = false;
    }
}