using UnityEngine;

public class GameplayUIController : BaseUIController
{
    private const string TIMER_TEXT = "TimerText";
    private const string SCORE_TEXT = "ScoreText";
    private const string LEVEL_TEXT = "LevelText";
    private const string EXP_TEXT = "ExperienceText";
    private const string HEALTH_TEXT = "HealthText";
    private const string PAUSE_BUTTON = "PauseButton";
    private const string WAVE_TEXT = "WaveText";

    private float currentTimer;
    private int currentScore;
    private int currentLevel = 1;
    private int currentExperience = 0;
    private float currentHealth = 100f;
    private float maxHealth = 100f;
    private int currentWave = 1;
    private WaveData currentWaveData;

    private ScoreSystem scoreSystem;
    private SpawnSystem spawnSystem;
    private PlayerHealth playerHealth;

    private bool isInitialized = false;

    private float playerSearchTimer = 0f;
    private const float PLAYER_SEARCH_INTERVAL = 0.5f;

    public GameplayUIController() : base("GameUI") { }

    protected override void OnShow()
    {
        base.OnShow();

        if (!isInitialized)
        {
            InitializeUIController();
            isInitialized = true;
        }

        SubscribeToEvents();
        RefreshAllDisplays();
    }

    private void InitializeUIController()
    {
        ServiceLocator.TryGet<ScoreSystem>(out scoreSystem);
        ServiceLocator.TryGet<SpawnSystem>(out spawnSystem);

        FindPlayerHealth();
        playerSearchTimer = 0f;
    }

    private void FindPlayerHealth()
    {
        if (playerHealth != null)
            return;

        Debug.Log("GameplayUIController: Searching for PlayerHealth...");

        if (ServiceLocator.TryGet<GameObject>(out var player) && player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log($"GameplayUIController: Found PlayerHealth via ServiceLocator on {player.name}");
                return;
            }
            else
            {
                Debug.LogWarning($"GameplayUIController: Player object found ({player.name}) but no PlayerHealth component!");
            }
        }
        else
        {
            Debug.LogWarning("GameplayUIController: No player object in ServiceLocator");
        }

        var allPlayers = UnityEngine.Object.FindObjectsOfType<PlayerHealth>();
        if (allPlayers.Length > 0)
        {
            playerHealth = allPlayers[0];
            Debug.Log($"GameplayUIController: Found PlayerHealth via FindObjectsOfType on {playerHealth.gameObject.name}");

            if (!ServiceLocator.IsRegistered<GameObject>())
            {
                ServiceLocator.Register<GameObject>(playerHealth.gameObject);
                Debug.Log("GameplayUIController: Registered player in ServiceLocator");
            }
            return;
        }

        var playerByTag = GameObject.FindWithTag("Player");
        if (playerByTag != null)
        {
            playerHealth = playerByTag.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log($"GameplayUIController: Found PlayerHealth via Player tag on {playerByTag.name}");

                if (!ServiceLocator.IsRegistered<GameObject>())
                {
                    ServiceLocator.Register<GameObject>(playerByTag);
                    Debug.Log("GameplayUIController: Registered player in ServiceLocator");
                }
                return;
            }
            else
            {
                Debug.LogWarning($"GameplayUIController: Player tag found ({playerByTag.name}) but no PlayerHealth component!");
            }
        }

        Debug.LogWarning("GameplayUIController: PlayerHealth not found by any method!");
    }

    private void SubscribeToEvents()
    {
        UnsubscribeFromEvents();

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnPlayerHealthChanged;
            isSubscribedToHealthEvents = true;
            Debug.Log("GameplayUIController: Subscribed to health events");

            currentHealth = playerHealth.GetCurrentHealth();
            maxHealth = playerHealth.GetMaxHealth();
            UpdateHealthDisplay();
        }

        if (scoreSystem != null)
        {
            scoreSystem.OnLevelUp += OnLevelChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (playerHealth != null && isSubscribedToHealthEvents)
        {
            playerHealth.OnHealthChanged -= OnPlayerHealthChanged;
            isSubscribedToHealthEvents = false;
        }

        if (scoreSystem != null)
        {
            scoreSystem.OnLevelUp -= OnLevelChanged;
        }
    }

    private void OnPlayerHealthChanged(float newHealth, float newMaxHealth)
    {
        currentHealth = newHealth;
        maxHealth = newMaxHealth;
        UpdateHealthDisplay();
        Debug.Log($"Health changed: {newHealth}/{newMaxHealth}");
    }

    private void OnLevelChanged()
    {
        if (scoreSystem != null)
        {
            currentLevel = scoreSystem.GetCurrentLevel();
            SetText(LEVEL_TEXT, $"Level: {currentLevel}");
            ShowLevelUpNotification();
        }
    }

    private void RefreshAllDisplays()
    {
        UpdateTimer(currentTimer);

        if (scoreSystem != null)
        {
            currentScore = scoreSystem.GetCurrentScore();
            currentLevel = scoreSystem.GetCurrentLevel();
            currentExperience = scoreSystem.GetCurrentExperience();
            var expToNext = scoreSystem.GetExperienceToNextLevel();

            SetText(SCORE_TEXT, $"Score: {currentScore}");
            SetText(LEVEL_TEXT, $"Level: {currentLevel}");
            SetText(EXP_TEXT, $"EXP: {currentExperience}/{expToNext}");
        }

        RefreshHealthDisplay();

        if (spawnSystem != null)
        {
            currentWave = spawnSystem.GetCurrentWave();
            SetText(WAVE_TEXT, $"Wave: {currentWave}");
        }
    }

    private void RefreshHealthDisplay()
    {
        if (playerHealth == null)
        {
            FindPlayerHealth();
        }

        if (playerHealth != null)
        {
            currentHealth = playerHealth.GetCurrentHealth();
            maxHealth = playerHealth.GetMaxHealth();
            Debug.Log($"Refreshing health display: {currentHealth}/{maxHealth}");

            if (!isSubscribedToHealthEvents)
            {
                SubscribeToEvents();
            }
        }
        else
        {
            currentHealth = 100f;
            maxHealth = 100f;
            Debug.LogWarning("PlayerHealth still not found, using default values");
        }

        UpdateHealthDisplay();
    }

    private bool isSubscribedToHealthEvents = false;

    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        playerSearchTimer += deltaTime;
        if (playerHealth == null && playerSearchTimer >= PLAYER_SEARCH_INTERVAL)
        {
            FindPlayerHealth();
            if (playerHealth != null)
            {
                SubscribeToEvents();
            }
            playerSearchTimer = 0f;
        }

        if (scoreSystem != null)
        {
            var newScore = scoreSystem.GetCurrentScore();
            var newExp = scoreSystem.GetCurrentExperience();

            if (newScore != currentScore)
            {
                currentScore = newScore;
                SetText(SCORE_TEXT, $"Score: {currentScore}");
            }

            if (newExp != currentExperience)
            {
                currentExperience = newExp;
                var expToNext = scoreSystem.GetExperienceToNextLevel();
                SetText(EXP_TEXT, $"EXP: {currentExperience}/{expToNext}");

                float progress = (float)currentExperience / expToNext;
                SetTextColor(EXP_TEXT, progress > 0.8f ? Color.yellow : Color.white);
            }
        }

        if (spawnSystem != null)
        {
            var newWave = spawnSystem.GetCurrentWave();
            if (newWave != currentWave)
            {
                currentWave = newWave;
                SetText(WAVE_TEXT, $"Wave: {currentWave}");
            }
        }

        if (playerHealth != null)
        {
            var newHealth = playerHealth.GetCurrentHealth();
            var newMaxHealth = playerHealth.GetMaxHealth();

            if (Mathf.Abs(newHealth - currentHealth) > 0.1f || Mathf.Abs(newMaxHealth - maxHealth) > 0.1f)
            {
                currentHealth = newHealth;
                maxHealth = newMaxHealth;
                UpdateHealthDisplay();
            }
        }
        else
        {
            SetText(HEALTH_TEXT, "Health: Searching...");
        }
    }

    private void UpdateHealthDisplay()
    {
        string healthText = $"Health: {currentHealth:F0}/{maxHealth:F0}";
        SetText(HEALTH_TEXT, healthText);

        float healthPercentage = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        Color healthColor = healthPercentage < 0.3f ? Color.red :
                           (healthPercentage < 0.6f ? Color.yellow : Color.white);
        SetTextColor(HEALTH_TEXT, healthColor);
    }

    public void UpdateTimer(float timeElapsed)
    {
        currentTimer = timeElapsed;
        string timeText = FormatTime(timeElapsed);
        SetText(TIMER_TEXT, timeText);
    }

    private string FormatTime(float timeElapsed)
    {
        int minutes = Mathf.FloorToInt(timeElapsed / 60);
        int seconds = Mathf.FloorToInt(timeElapsed % 60);
        return $"{minutes:00}:{seconds:00}";
    }

    public void UpdateScore(int score)
    {
        if (currentScore != score)
        {
            currentScore = score;
            SetText(SCORE_TEXT, $"Score: {score}");
        }
    }

    public void UpdateLevel(int level)
    {
        if (currentLevel != level)
        {
            currentLevel = level;
            SetText(LEVEL_TEXT, $"Level: {level}");
        }
    }

    public void UpdateExperience(int experience, int experienceToNext)
    {
        if (currentExperience != experience)
        {
            currentExperience = experience;
            SetText(EXP_TEXT, $"EXP: {experience}/{experienceToNext}");

            float progress = (float)experience / experienceToNext;
            SetTextColor(EXP_TEXT, progress > 0.8f ? Color.yellow : Color.white);
        }
    }

    public void UpdateHealth(float health)
    {
        if (Mathf.Abs(currentHealth - health) > 0.1f)
        {
            currentHealth = health;
            UpdateHealthDisplay();
        }
    }

    public void UpdateWave(int wave)
    {
        if (currentWave != wave)
        {
            currentWave = wave;
            SetText(WAVE_TEXT, $"Wave: {wave}");
        }
    }

    public void UpdateWaveInfo(WaveData waveData)
    {
        if (waveData == null) return;

        currentWave = waveData.waveNumber;
        currentWaveData = waveData;
        var waveInfoText = $"Wave {waveData.waveNumber}\nEnemies: {waveData.enemyCount}\nDifficulty: {waveData.difficultyPoints:F0}";
        SetText(WAVE_TEXT, waveInfoText);
    }

    public void ShowLevelUpNotification()
    {
        SetTextColor(LEVEL_TEXT, Color.gold);
        SetTextColor(EXP_TEXT, Color.gold);
        CoroutineRunner.StartRoutine(ResetLevelUpColors());
    }

    private System.Collections.IEnumerator ResetLevelUpColors()
    {
        yield return new WaitForSeconds(1f);
        SetTextColor(LEVEL_TEXT, Color.white);
        SetTextColor(EXP_TEXT, Color.white);
    }

    protected override void OnHide()
    {
        base.OnHide();
        UnsubscribeFromEvents();
    }

    protected override void OnCleanup()
    {
        UnsubscribeFromEvents();
        base.OnCleanup();

        scoreSystem = null;
        spawnSystem = null;
        playerHealth = null;
        isInitialized = false;
        currentWaveData = null;
    }

    public float GetCurrentTimer() => currentTimer;
    public int GetCurrentScore() => currentScore;
    public int GetCurrentLevel() => currentLevel;
    public int GetCurrentExperience() => currentExperience;
    public float GetCurrentHealth() => currentHealth;
    public int GetCurrentWave() => currentWave;
}