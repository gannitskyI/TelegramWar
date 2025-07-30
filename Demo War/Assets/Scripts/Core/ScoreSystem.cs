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
        Debug.Log("Triggering upgrade selection - using pause manager (NO STATE CHANGES)");

        if (ServiceLocator.TryGet<UpgradeSystem>(out var upgradeSystem))
        {
            var upgradeOptions = upgradeSystem.GenerateUpgradeOptions(3);
            GamePauseManager.Instance.ShowUpgradeSelection(upgradeOptions);
        }
        else
        {
            Debug.LogError("UpgradeSystem not found! Cannot generate upgrade options.");
            var fallbackUpgrades = CreateFallbackUpgrades();
            GamePauseManager.Instance.ShowUpgradeSelection(fallbackUpgrades);
        }
    }

    private System.Collections.Generic.List<Upgrade> CreateFallbackUpgrades()
    {
        return new System.Collections.Generic.List<Upgrade>
        {
            new Upgrade("damage_boost", "Damage Boost", "+20% damage", UpgradeType.Damage, 0.2f),
            new Upgrade("attack_speed", "Attack Speed", "+25% attack speed", UpgradeType.AttackSpeed, 0.25f),
            new Upgrade("health_boost", "Health Boost", "+30% max health", UpgradeType.Health, 0.3f)
        };
    }

    private void NotifyUIScoreChanged()
    {
        if (ServiceLocator.TryGet<UISystem>(out var uiSystem))
        {
            var gameplayUI = uiSystem.GetUIController<GameplayUIController>("GameUI");
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
            var gameplayUI = uiSystem.GetUIController<GameplayUIController>("GameUI");
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

    public void ResetForRestart()
    {
        currentScore = 0;
        currentExperience = 0;
        currentLevel = 1;
        experienceToNextLevel = 100;
        OnLevelUp = null;

        Debug.Log("ScoreSystem completely reset for restart");
    }

    public void Cleanup()
    {
        OnLevelUp = null;
        Debug.Log("ScoreSystem events cleaned up");
    }
}