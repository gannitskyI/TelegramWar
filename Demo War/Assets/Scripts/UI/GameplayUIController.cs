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
    private bool isFirstShow = true;

    private ScoreSystem cachedScoreSystem;
    private SpawnSystem cachedSpawnSystem;
    private PlayerHealth cachedPlayerHealth;
    private bool systemsCached = false;
    private bool healthEventSubscribed = false;

    private readonly struct UIUpdateBatch
    {
        public readonly bool updateTimer;
        public readonly bool updateScore;
        public readonly bool updateLevel;
        public readonly bool updateExp;
        public readonly bool updateHealth;
        public readonly bool updateWave;

        public readonly float timerValue;
        public readonly int scoreValue;
        public readonly int levelValue;
        public readonly int expValue;
        public readonly int expToNext;
        public readonly float healthValue;
        public readonly float maxHealthValue;
        public readonly int waveValue;

        public UIUpdateBatch(bool updateTimer = false, bool updateScore = false, bool updateLevel = false,
                           bool updateExp = false, bool updateHealth = false, bool updateWave = false,
                           float timerValue = 0f, int scoreValue = 0, int levelValue = 0,
                           int expValue = 0, int expToNext = 0, float healthValue = 0f, float maxHealthValue = 100f, int waveValue = 0)
        {
            this.updateTimer = updateTimer;
            this.updateScore = updateScore;
            this.updateLevel = updateLevel;
            this.updateExp = updateExp;
            this.updateHealth = updateHealth;
            this.updateWave = updateWave;
            this.timerValue = timerValue;
            this.scoreValue = scoreValue;
            this.levelValue = levelValue;
            this.expValue = expValue;
            this.expToNext = expToNext;
            this.healthValue = healthValue;
            this.maxHealthValue = maxHealthValue;
            this.waveValue = waveValue;
        }
    }

    public GameplayUIController() : base("GameUI")
    {
    }

    protected override void OnShow()
    {
        base.OnShow();

        CacheSystems();

        if (isFirstShow)
        {
            InitializeWithDefaults();
            isFirstShow = false;
        }
        else
        {
            RestoreCurrentValues();
        }

        SubscribeToHealthEvents();
    }

    private void SubscribeToHealthEvents()
    {
        if (cachedPlayerHealth != null && !healthEventSubscribed)
        {
            cachedPlayerHealth.OnHealthChanged += OnPlayerHealthChanged;
            healthEventSubscribed = true;

            currentHealth = cachedPlayerHealth.GetCurrentHealth();
            maxHealth = cachedPlayerHealth.GetMaxHealth();
            UpdateHealthDisplay();

            Debug.Log($"[GAMEPLAY UI] Subscribed to health events. Current: {currentHealth}/{maxHealth}");
        }
        else if (cachedPlayerHealth == null)
        {
            currentHealth = 100f;
            maxHealth = 100f;
            UpdateHealthDisplay();
            Debug.Log("[GAMEPLAY UI] Player not found yet, using default health values");
        }
    }

    private void UnsubscribeFromHealthEvents()
    {
        if (cachedPlayerHealth != null && healthEventSubscribed)
        {
            cachedPlayerHealth.OnHealthChanged -= OnPlayerHealthChanged;
            healthEventSubscribed = false;
            Debug.Log("[GAMEPLAY UI] Unsubscribed from health events");
        }
    }

    private void OnPlayerHealthChanged(float newHealth, float newMaxHealth)
    {
        Debug.Log($"[GAMEPLAY UI] Health changed event: {currentHealth}/{maxHealth} -> {newHealth}/{newMaxHealth}");
        currentHealth = newHealth;
        maxHealth = newMaxHealth;
        UpdateHealthDisplay();
    }

    private void UpdateHealthDisplay()
    {
        string healthText = $"Health: {currentHealth:F0}/{maxHealth:F0}";
        SetText(HEALTH_TEXT, healthText);

        float healthPercentage = currentHealth / maxHealth;
        Color healthColor = healthPercentage < 0.3f ? Color.red :
                           (healthPercentage < 0.6f ? Color.yellow : Color.white);
        SetTextColor(HEALTH_TEXT, healthColor);

        Debug.Log($"[GAMEPLAY UI] Health display updated: {healthText} (Color: {healthColor})");
    }

    private void InitializeWithDefaults()
    {
        Debug.Log("[GAMEPLAY UI] Initializing with defaults");
        UpdateTimer(0f);
        UpdateScore(0);
        UpdateLevel(1);
        UpdateExperience(0, 100);
        UpdateWave(1);

        currentHealth = 100f;
        maxHealth = 100f;
        UpdateHealthDisplay();

        if (cachedPlayerHealth != null)
        {
            Debug.Log($"[GAMEPLAY UI] PlayerHealth found during initialization: {cachedPlayerHealth.GetCurrentHealth()}/{cachedPlayerHealth.GetMaxHealth()}");
        }
        else
        {
            Debug.Log("[GAMEPLAY UI] PlayerHealth not found during initialization - using defaults");
        }
    }

    private void RestoreCurrentValues()
    {
        Debug.Log("[GAMEPLAY UI] Restoring current values");
        SetText(TIMER_TEXT, FormatTime(currentTimer));
        SetText(SCORE_TEXT, $"Score: {currentScore}");
        SetText(LEVEL_TEXT, $"Level: {currentLevel}");
        SetText(EXP_TEXT, $"EXP: {currentExperience}");
        UpdateHealthDisplay();

        if (currentWaveData != null)
        {
            RestoreWaveInfo();
        }
        else
        {
            SetText(WAVE_TEXT, $"Wave: {currentWave}");
        }

        Debug.Log($"GameplayUI restored - Wave: {currentWave}, Score: {currentScore}, Level: {currentLevel}");
    }

    private void RestoreWaveInfo()
    {
        var waveInfoText = $"Wave {currentWaveData.waveNumber}\nEnemies: {currentWaveData.enemyCount}\nDifficulty: {currentWaveData.difficultyPoints:F0}";
        SetText(WAVE_TEXT, waveInfoText);
    }

    private void CacheSystems()
    {
        if (cachedScoreSystem == null)
        {
            ServiceLocator.TryGet<ScoreSystem>(out cachedScoreSystem);
        }

        if (cachedSpawnSystem == null)
        {
            ServiceLocator.TryGet<SpawnSystem>(out cachedSpawnSystem);
        }

        if (cachedPlayerHealth == null)
        {
            if (ServiceLocator.TryGet<GameObject>(out var player) && player != null)
            {
                cachedPlayerHealth = player.GetComponent<PlayerHealth>();
                if (cachedPlayerHealth != null)
                {
                    Debug.Log($"[GAMEPLAY UI] Found PlayerHealth component: {cachedPlayerHealth.GetCurrentHealth()}/{cachedPlayerHealth.GetMaxHealth()}");
                }
            }
        }

        systemsCached = true;
    }

    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (!systemsCached || cachedPlayerHealth == null)
        {
            CacheSystems();

            if (cachedPlayerHealth != null && !healthEventSubscribed)
            {
                SubscribeToHealthEvents();
            }
        }

        if (systemsCached)
        {
            var batch = BuildUpdateBatch();
            ApplyUIBatch(batch);
        }
    }

    private UIUpdateBatch BuildUpdateBatch()
    {
        bool updateScore = false, updateLevel = false, updateExp = false, updateWave = false, updateHealth = false;
        int newScore = currentScore, newLevel = currentLevel, newExp = currentExperience, newExpToNext = 100, newWave = currentWave;
        float newHealth = currentHealth, newMaxHealth = maxHealth;

        if (cachedScoreSystem != null)
        {
            newScore = cachedScoreSystem.GetCurrentScore();
            newLevel = cachedScoreSystem.GetCurrentLevel();
            newExp = cachedScoreSystem.GetCurrentExperience();
            newExpToNext = cachedScoreSystem.GetExperienceToNextLevel();

            updateScore = newScore != currentScore;
            updateLevel = newLevel != currentLevel;
            updateExp = newExp != currentExperience;
        }

        if (cachedSpawnSystem != null)
        {
            newWave = cachedSpawnSystem.GetCurrentWave();
            updateWave = newWave != currentWave;
        }

        if (cachedPlayerHealth != null && !healthEventSubscribed)
        {
            newHealth = cachedPlayerHealth.GetCurrentHealth();
            newMaxHealth = cachedPlayerHealth.GetMaxHealth();
            updateHealth = Mathf.Abs(newHealth - currentHealth) > 0.1f || Mathf.Abs(newMaxHealth - maxHealth) > 0.1f;
        }

        return new UIUpdateBatch(
            updateScore: updateScore,
            updateLevel: updateLevel,
            updateExp: updateExp,
            updateWave: updateWave,
            updateHealth: updateHealth,
            scoreValue: newScore,
            levelValue: newLevel,
            expValue: newExp,
            expToNext: newExpToNext,
            waveValue: newWave,
            healthValue: newHealth,
            maxHealthValue: newMaxHealth
        );
    }

    private void ApplyUIBatch(UIUpdateBatch batch)
    {
        if (batch.updateScore)
        {
            currentScore = batch.scoreValue;
            SetText(SCORE_TEXT, $"Score: {currentScore}");
        }

        if (batch.updateLevel)
        {
            currentLevel = batch.levelValue;
            SetText(LEVEL_TEXT, $"Level: {currentLevel}");
        }

        if (batch.updateExp)
        {
            currentExperience = batch.expValue;
            SetText(EXP_TEXT, $"EXP: {currentExperience}/{batch.expToNext}");

            float progress = (float)currentExperience / batch.expToNext;
            SetTextColor(EXP_TEXT, progress > 0.8f ? Color.yellow : Color.white);
        }

        if (batch.updateWave)
        {
            currentWave = batch.waveValue;
            SetText(WAVE_TEXT, $"Wave: {currentWave}");
        }

        if (batch.updateHealth)
        {
            currentHealth = batch.healthValue;
            maxHealth = batch.maxHealthValue;
            UpdateHealthDisplay();
        }
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
            Debug.Log($"[GAMEPLAY UI] Health manually updated to: {health}");
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

    protected override void HandleButtonClick(string buttonName)
    {
        if (buttonName == PAUSE_BUTTON)
        {
            PauseGame();
        }
    }

    private void PauseGame()
    {
        Time.timeScale = Time.timeScale > 0 ? 0 : 1;
        SetButtonText(PAUSE_BUTTON, Time.timeScale == 0 ? "RESUME" : "PAUSE");
    }

    private void SetButtonText(string buttonId, string text)
    {
        if (buttons.TryGetValue(buttonId, out var button))
        {
            var tmpButtonText = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpButtonText != null)
            {
                tmpButtonText.text = text;
                return;
            }

            var buttonText = button.GetComponentInChildren<UnityEngine.UI.Text>();
            if (buttonText != null)
            {
                buttonText.text = text;
            }
        }
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

    public void ShowDamageIndicator(Vector3 worldPosition, float damage)
    {
        SetTextColor(HEALTH_TEXT, Color.red);
        CoroutineRunner.StartRoutine(ResetHealthColor());
    }

    private System.Collections.IEnumerator ResetHealthColor()
    {
        yield return new WaitForSeconds(0.5f);
        float healthPercentage = currentHealth / maxHealth;
        Color healthColor = healthPercentage < 0.3f ? Color.red :
                           (healthPercentage < 0.6f ? Color.yellow : Color.white);
        SetTextColor(HEALTH_TEXT, healthColor);
    }

    protected override void OnHide()
    {
        base.OnHide();
        UnsubscribeFromHealthEvents();
    }

    protected override void OnCleanup()
    {
        UnsubscribeFromHealthEvents();
        base.OnCleanup();
        cachedScoreSystem = null;
        cachedSpawnSystem = null;
        cachedPlayerHealth = null;
        systemsCached = false;
        isFirstShow = true;
    }

    public float GetCurrentTimer() => currentTimer;
    public int GetCurrentScore() => currentScore;
    public int GetCurrentLevel() => currentLevel;
    public int GetCurrentExperience() => currentExperience;
    public float GetCurrentHealth() => currentHealth;
    public int GetCurrentWave() => currentWave;
}