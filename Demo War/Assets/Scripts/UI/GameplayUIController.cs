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

        if (ServiceLocator.TryGet<GameObject>(out var player) && player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    private void SubscribeToEvents()
    {
        UnsubscribeFromEvents();

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnPlayerHealthChanged;
        }

        if (scoreSystem != null)
        {
            scoreSystem.OnLevelUp += OnLevelChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnPlayerHealthChanged;
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

        if (playerHealth != null)
        {
            currentHealth = playerHealth.GetCurrentHealth();
            maxHealth = playerHealth.GetMaxHealth();
        }
        UpdateHealthDisplay();

        if (spawnSystem != null)
        {
            currentWave = spawnSystem.GetCurrentWave();
            SetText(WAVE_TEXT, $"Wave: {currentWave}");
        }
    }

    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

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
    }

    private void UpdateHealthDisplay()
    {
        string healthText = $"Health: {currentHealth:F0}/{maxHealth:F0}";
        SetText(HEALTH_TEXT, healthText);

        float healthPercentage = currentHealth / maxHealth;
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