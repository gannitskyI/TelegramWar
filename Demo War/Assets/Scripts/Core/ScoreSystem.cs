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
        Debug.Log($"Score: {currentScore}");

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

        Debug.Log($"Experience: +{bonusExperience} (total: {currentExperience})");

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

            Debug.Log($"Level up! New level: {currentLevel}");
            Debug.Log($"Experience to next level: {experienceToNextLevel}");

            NotifyUILevelUp();

            OnLevelUp?.Invoke();

            TriggerUpgradeSelection();
        }
    }

    private void TriggerUpgradeSelection()
    {
        Debug.Log("Triggering upgrade selection");

        var stateMachine = ServiceLocator.Get<GameStateMachine>();
        if (stateMachine != null)
        {
            stateMachine.ChangeState(new UpgradeSelectionState());
        }
        else
        {
            Debug.LogError("GameStateMachine not found! Cannot trigger upgrade selection.");
        }
    }

    private void NotifyUIScoreChanged()
    {
        if (ServiceLocator.TryGet<UISystem>(out var uiSystem))
        {
            var gameplayUI = uiSystem.GetUIController<GameplayUIController>("GameUI"); // Изменено с "GameplayUI" на "GameUI"
            if (gameplayUI != null)
            {
                gameplayUI.UpdateScore(currentScore);
            }
        }
    }

    private void NotifyUILevelUp()
    {
        if (ServiceLocator.TryGet<UISystem>(out var uiSystem))
        {
            var gameplayUI = uiSystem.GetUIController<GameplayUIController>("GameUI"); // Изменено с "GameplayUI" на "GameUI"
            if (gameplayUI != null)
            {
                gameplayUI.ShowLevelUpNotification();
            }
        }
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    public int GetCurrentExperience()
    {
        return currentExperience;
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public int GetExperienceToNextLevel()
    {
        return experienceToNextLevel;
    }

    public float GetLevelProgress()
    {
        return (float)currentExperience / experienceToNextLevel;
    }

    public void Cleanup()
    {
        currentScore = 0;
        currentExperience = 0;
        currentLevel = 1;
        experienceToNextLevel = 100;
        OnLevelUp = null;
    }
}