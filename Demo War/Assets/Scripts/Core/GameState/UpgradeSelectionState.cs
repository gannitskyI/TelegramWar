using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeSelectionState : GameState
{
    private const string UPGRADE_UI_ID = "UpgradeSelection";
    private const string GAMEPLAY_UI_ID = "GameUI";
    private UpgradeSelectionUIController upgradeUIController;
    private List<Upgrade> currentUpgradeOptions;
    private GameplayState gameplayState;

    public UpgradeSelectionState(GameplayState currentGameplayState)
    {
        gameplayState = currentGameplayState;
    }

    public override IEnumerator Enter()
    {
        Debug.Log("Entering Upgrade Selection State - PAUSE ONLY");

        if (gameplayState != null)
        {
            gameplayState.Pause();
        }

        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem == null)
        {
            Debug.LogError("UISystem not found! Cannot show upgrade selection.");
            yield break;
        }

        if (uiSystem.IsUIActive(GAMEPLAY_UI_ID))
        {
            uiSystem.HideUI(GAMEPLAY_UI_ID);
        }

        upgradeUIController = new UpgradeSelectionUIController();
        uiSystem.RegisterUIController(UPGRADE_UI_ID, upgradeUIController);

        GenerateUpgradeOptions();
        uiSystem.ShowUI(UPGRADE_UI_ID);

        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.DisableAllInput();
        }

        Debug.Log("Upgrade Selection State entered - game is paused");
        yield return null;
    }

    private void GenerateUpgradeOptions()
    {
        if (ServiceLocator.TryGet<UpgradeSystem>(out var upgradeSystem))
        {
            currentUpgradeOptions = upgradeSystem.GenerateUpgradeOptions(3);
            upgradeUIController?.SetUpgradeOptions(currentUpgradeOptions);
        }
        else
        {
            Debug.LogError("UpgradeSystem not found! Creating fallback upgrades.");
            CreateFallbackUpgrades();
        }
    }

    private void CreateFallbackUpgrades()
    {
        currentUpgradeOptions = new List<Upgrade>
        {
            new Upgrade("damage_boost", "Damage Boost", "+20% damage", UpgradeType.Damage, 0.2f),
            new Upgrade("attack_speed", "Attack Speed", "+25% attack speed", UpgradeType.AttackSpeed, 0.25f),
            new Upgrade("health_boost", "Health Boost", "+30% max health", UpgradeType.Health, 0.3f)
        };

        upgradeUIController?.SetUpgradeOptions(currentUpgradeOptions);
    }

    public void SelectUpgrade(int upgradeIndex)
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
        CoroutineRunner.StartRoutine(ReturnToGameplayAfterDelay(0.3f));
    }

    private IEnumerator ReturnToGameplayAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        var stateMachine = ServiceLocator.Get<GameStateMachine>();
        if (stateMachine != null && gameplayState != null)
        {
            stateMachine.ChangeState(gameplayState);
        }
    }

    public override IEnumerator Exit()
    {
        Debug.Log("Exiting Upgrade Selection State - RESUME GAME");

        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem != null)
        {
            uiSystem.HideUI(UPGRADE_UI_ID);
            uiSystem.UnregisterUIController(UPGRADE_UI_ID);

            if (uiSystem.GetUIController<GameplayUIController>(GAMEPLAY_UI_ID) != null)
            {
                uiSystem.ShowUI(GAMEPLAY_UI_ID);
            }
        }

        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.EnableGameplayInput();
        }

        if (gameplayState != null)
        {
            gameplayState.Resume();
        }

        upgradeUIController = null;
        currentUpgradeOptions = null;

        Debug.Log("Game resumed from upgrade selection");
        yield return null;
    }

    public override void Update()
    {
    }
}