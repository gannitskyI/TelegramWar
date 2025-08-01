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
    private List<Upgrade> currentUpgradeOptions;
    private UpgradeSelectionUIController upgradeUIController;
    private const string UPGRADE_UI_ID = "UpgradeSelection";
    private const string GAMEPLAY_UI_ID = "GameUI";

    public bool IsPaused => isPaused;

    public void ShowUpgradeSelection(List<Upgrade> upgradeOptions)
    {
        if (isPaused)
        {
            Debug.LogWarning("Game is already paused for upgrade selection");
            return;
        }

        Debug.Log("Pausing game for upgrade selection - NO STATE CHANGE");

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

        Debug.Log("Game systems paused - objects remain intact");
    }

    private void ShowUpgradeUI()
    {
        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem == null) return;

        if (uiSystem.IsUIActive(GAMEPLAY_UI_ID))
        {
            uiSystem.HideUI(GAMEPLAY_UI_ID);
        }

        upgradeUIController = new UpgradeSelectionUIController();
        upgradeUIController.OnUpgradeSelected += OnUpgradeSelected;

        uiSystem.RegisterUIController(UPGRADE_UI_ID, upgradeUIController);
        upgradeUIController.SetUpgradeOptions(currentUpgradeOptions);
        uiSystem.ShowUI(UPGRADE_UI_ID);

        Debug.Log("Upgrade UI shown - gameplay UI hidden but not destroyed");
    }

    private void OnUpgradeSelected(int upgradeIndex)
    {
        if (currentUpgradeOptions == null || upgradeIndex < 0 || upgradeIndex >= currentUpgradeOptions.Count)
        {
            Debug.LogError($"Invalid upgrade index: {upgradeIndex}");
            return;
        }

        var selectedUpgrade = currentUpgradeOptions[upgradeIndex];
        Debug.Log($"Selected upgrade: {selectedUpgrade.name}");

        if (ServiceLocator.TryGet<UpgradeSystem>(out var upgradeSystem))
        {
            upgradeSystem.SelectUpgrade(selectedUpgrade);
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

        Debug.Log("Resuming game - NO STATE CHANGE");

        HideUpgradeUI();
        UnpauseGame();

        isPaused = false;
        currentUpgradeOptions = null;

        Debug.Log("Game resumed - all objects preserved");
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
        Debug.Log("Upgrade UI hidden - gameplay UI restored");
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

        Debug.Log("Game systems resumed");
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

        Debug.Log($"Reactivated {allEnemies.Length} enemies and {allProjectiles.Length} projectiles");
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