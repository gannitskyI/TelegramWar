using System.Collections;
using UnityEngine;

public class ScoreSystem : IInitializable
{
    public int InitializationOrder => 40;

    private int currentScore;
    private int currentExperience;
    private int currentLevel = 1;
    private int experienceToNextLevel = 100;

    public System.Action OnLevelUp;

    public IEnumerator Initialize()
    {
        currentScore = 0;
        currentExperience = 0;
        currentLevel = 1;
        experienceToNextLevel = 100;
        yield return null;
    }

    public void AddScore(int points)
    {
        currentScore += points;
        NotifyUIScoreChanged();
    }

    public void AddExperience(int experience)
    {
        float expMultiplier = 1f;
        if (ServiceLocator.TryGet<UpgradeSystem>(out var upgradeSystem))
        {
            expMultiplier = upgradeSystem.GetUpgradeMultiplier(UpgradeType.ExperienceMultiplier);
        }

        int bonusExperience = Mathf.RoundToInt(experience * expMultiplier);
        currentExperience += bonusExperience;
        AddScore(bonusExperience);
        CheckForLevelUp();
    }

    private void CheckForLevelUp()
    {
        if (currentExperience >= experienceToNextLevel)
        {
            currentLevel++;
            currentExperience -= experienceToNextLevel;
            experienceToNextLevel = Mathf.RoundToInt(experienceToNextLevel * 1.2f);

            NotifyUILevelUp();
            OnLevelUp?.Invoke();
            TriggerUpgradeSelection();
        }
    }

    private void TriggerUpgradeSelection()
    {
        if (!ServiceLocator.TryGet<UpgradeSystem>(out var upgradeSystem))
        {
            Debug.LogError("UpgradeSystem not found! Cannot show upgrade selection.");
            return;
        }

        if (!upgradeSystem.IsDatabaseLoaded())
        {
            Debug.LogError("UpgradeSystem database not loaded! Cannot show upgrade selection.");
            return;
        }

        var upgradeOptions = upgradeSystem.GenerateUpgradeOptions(3);

        if (upgradeOptions == null || upgradeOptions.Count == 0)
        {
            Debug.LogError("No upgrade options generated! Check your UpgradeDatabase configuration.");
            return;
        }

        Debug.Log($"Triggering upgrade selection with {upgradeOptions.Count} options from YOUR database");
        foreach (var option in upgradeOptions)
        {
            Debug.Log($"- Option: {option.DisplayName} (ID: {option.UpgradeId})");
        }

        GamePauseManager.Instance.ShowUpgradeSelection(upgradeOptions);
    }

    private void NotifyUIScoreChanged()
    {
        if (ServiceLocator.TryGet<UISystem>(out var uiSystem))
        {
            var gameplayUI = uiSystem.GetUIController<GameplayUIController>("GameUI");
            gameplayUI?.UpdateScore(currentScore);
        }
    }

    private void NotifyUILevelUp()
    {
        if (ServiceLocator.TryGet<UISystem>(out var uiSystem))
        {
            var gameplayUI = uiSystem.GetUIController<GameplayUIController>("GameUI");
            gameplayUI?.ShowLevelUpNotification();
        }
    }

    public int GetCurrentScore() => currentScore;
    public int GetCurrentExperience() => currentExperience;
    public int GetCurrentLevel() => currentLevel;
    public int GetExperienceToNextLevel() => experienceToNextLevel;
    public float GetLevelProgress() => (float)currentExperience / experienceToNextLevel;

    public void ResetForRestart()
    {
        currentScore = 0;
        currentExperience = 0;
        currentLevel = 1;
        experienceToNextLevel = 100;
        OnLevelUp = null;
    }

    public void Cleanup()
    {
        OnLevelUp = null;
    }
}