using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeSelectionState : GameState
{
    private const string UPGRADE_UI_ID = "UpgradeSelection";
    private const string GAMEPLAY_UI_ID = "GameUI";
    private UpgradeSelectionUIController upgradeUIController;
    private List<UpgradeConfig> currentUpgradeOptions; // Изменено с Upgrade на UpgradeConfig
    private GameplayState gameplayState;

    public UpgradeSelectionState(GameplayState currentGameplayState)
    {
        gameplayState = currentGameplayState;
    }

    public override IEnumerator Enter()
    {
        Debug.Log("Вход в состояние выбора апгрейда - ТОЛЬКО ПАУЗА");
        if (gameplayState != null)
        {
            gameplayState.Pause();
        }

        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem == null)
        {
            Debug.LogError("UISystem не найден! Невозможно отобразить выбор апгрейда.");
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

        Debug.Log("Состояние выбора апгрейда активировано - игра приостановлена");
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
            Debug.LogError("UpgradeSystem не найден! Создание резервных апгрейдов.");
           
        }
    }
     
     

    public void SelectUpgrade(int upgradeIndex)
    {
        if (currentUpgradeOptions == null || upgradeIndex < 0 || upgradeIndex >= currentUpgradeOptions.Count)
        {
            Debug.LogError($"Недопустимый индекс апгрейда: {upgradeIndex}");
            return;
        }

        var selectedUpgrade = currentUpgradeOptions[upgradeIndex];
        Debug.Log($"Выбран апгрейд: {selectedUpgrade.DisplayName}");

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
        Debug.Log("Выход из состояния выбора апгрейда - ВОЗОБНОВЛЕНИЕ ИГРЫ");
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
        Debug.Log("Игра возобновлена из состояния выбора апгрейда");
        yield return null;
    }

    public override void Update()
    {
    }
}