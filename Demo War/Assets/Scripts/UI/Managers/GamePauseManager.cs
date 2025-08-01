using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePauseManager
{
    private static GamePauseManager instance;
    public static GamePauseManager Instance
    {
        get
        {
            if (instance == null)
                instance = new GamePauseManager();
            return instance;
        }
    }

    private bool isPaused = false;
    private List<UpgradeConfig> currentUpgradeOptions;
    private UpgradeSelectionUIController upgradeUIController;
    private const string UPGRADE_UI_ID = "UpgradeSelection";
    private const string GAMEPLAY_UI_ID = "GameUI";

    public bool IsPaused => isPaused;

    public void ShowUpgradeSelection(List<UpgradeConfig> upgradeOptions)
    {
        if (isPaused)
        {
            Debug.LogWarning("Game already paused for upgrade selection");
            return;
        }

        if (upgradeOptions == null || upgradeOptions.Count == 0)
        {
            Debug.LogError("Cannot show upgrade selection: no upgrade options provided!");
            return;
        }

        Debug.Log($"Showing upgrade selection with {upgradeOptions.Count} options");
        isPaused = true;
        currentUpgradeOptions = upgradeOptions;

        PauseGame();
        ShowUpgradeUI();
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;

        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StopSpawning();
        }

        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.DisableAllInput();
        }
    }

    private void ShowUpgradeUI()
    {
        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem == null)
        {
            Debug.LogError("UISystem not found! Cannot show upgrade UI.");
            ResumeGame();
            return;
        }

        if (uiSystem.IsUIActive(GAMEPLAY_UI_ID))
        {
            uiSystem.HideUI(GAMEPLAY_UI_ID);
        }

        upgradeUIController = new UpgradeSelectionUIController();
        upgradeUIController.OnUpgradeSelected += OnUpgradeSelected;

        uiSystem.RegisterUIController(UPGRADE_UI_ID, upgradeUIController);

        upgradeUIController.SetUpgradeOptions(currentUpgradeOptions);
        uiSystem.ShowUI(UPGRADE_UI_ID);

        Debug.Log("Upgrade UI displayed successfully");
    }

    private void OnUpgradeSelected(int upgradeIndex)
    {
        if (currentUpgradeOptions == null || upgradeIndex < 0 || upgradeIndex >= currentUpgradeOptions.Count)
        {
            Debug.LogError($"Invalid upgrade selection: index {upgradeIndex} for {currentUpgradeOptions?.Count ?? 0} options");
            ResumeGame();
            return;
        }

        var selectedUpgrade = currentUpgradeOptions[upgradeIndex];
        Debug.Log($"Upgrade selected: {selectedUpgrade.DisplayName}");

        if (ServiceLocator.TryGet<UpgradeSystem>(out var upgradeSystem))
        {
            bool success = upgradeSystem.SelectUpgrade(selectedUpgrade);
            if (!success)
            {
                Debug.LogError("Failed to apply selected upgrade!");
            }
        }
        else
        {
            Debug.LogError("UpgradeSystem not found! Cannot apply upgrade.");
        }

        WebGLHelper.TriggerHapticFeedback("medium");
        CoroutineRunner.StartRoutine(ResumeGameAfterDelay(0.3f));
    }

    private IEnumerator ResumeGameAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ResumeGame();
    }

    public void ResumeGame()
    {
        if (!isPaused)
        {
            Debug.LogWarning("Game is not paused");
            return;
        }

        Debug.Log("Resuming game from upgrade selection");

        HideUpgradeUI();
        UnpauseGame();

        isPaused = false;
        currentUpgradeOptions = null;
    }

    private void HideUpgradeUI()
    {
        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem == null) return;

        if (upgradeUIController != null)
        {
            upgradeUIController.OnUpgradeSelected -= OnUpgradeSelected;
        }

        uiSystem.HideUI(UPGRADE_UI_ID);
        uiSystem.UnregisterUIController(UPGRADE_UI_ID);

        if (uiSystem.GetUIController<GameplayUIController>(GAMEPLAY_UI_ID) != null)
        {
            uiSystem.ShowUI(GAMEPLAY_UI_ID);
        }

        upgradeUIController = null;
    }

    private void UnpauseGame()
    {
        Time.timeScale = 1f;

        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StartSpawning();
        }

        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.EnableGameplayInput();
        }

        ReactivateExistingEnemies();
    }

    private void ReactivateExistingEnemies()
    {
        var allEnemies = Object.FindObjectsOfType<EnemyBehaviour>();
        foreach (var enemy in allEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.enabled = true;
                var enemyRb = enemy.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    enemyRb.WakeUp();
                }
            }
        }

        var allProjectiles = Object.FindObjectsOfType<EnemyProjectile>();
        foreach (var projectile in allProjectiles)
        {
            if (projectile != null && projectile.gameObject.activeInHierarchy)
            {
                projectile.enabled = true;
                var projectileRb = projectile.GetComponent<Rigidbody2D>();
                if (projectileRb != null)
                {
                    projectileRb.WakeUp();
                }
            }
        }
    }

    public void Cleanup()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        instance = null;
    }
}