using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeSelectionState : GameState
{
    private const string UPGRADE_UI_ID = "UpgradeSelection";
    private const string GAMEPLAY_UI_ID = "GameUI";
    private UpgradeSelectionUIController upgradeUIController;
    private List<Upgrade> currentUpgradeOptions;

    public override IEnumerator Enter()
    {
        Debug.Log("Entering Upgrade Selection State");

        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem == null)
        {
            Debug.LogError("UISystem not found! Cannot show upgrade selection.");
            yield break;
        }

        if (uiSystem.IsUIActive(GAMEPLAY_UI_ID))
        {
            uiSystem.HideUI(GAMEPLAY_UI_ID);
            Debug.Log("Gameplay UI hidden");
        }
        else
        {
            Debug.Log("Gameplay UI was not active, skipping hide");
        }

        upgradeUIController = new UpgradeSelectionUIController();
        uiSystem.RegisterUIController(UPGRADE_UI_ID, upgradeUIController);

        GenerateUpgradeOptions();

        uiSystem.ShowUI(UPGRADE_UI_ID);

        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.DisableAllInput();
            Debug.Log("Game input disabled in upgrade selection");
        }

        Time.timeScale = 0f;

        Debug.Log("Upgrade Selection State entered successfully");
        yield return null;
    }

    private void GenerateUpgradeOptions()
    {
        if (ServiceLocator.TryGet<UpgradeSystem>(out var upgradeSystem))
        {
            currentUpgradeOptions = upgradeSystem.GenerateUpgradeOptions(3);

            if (upgradeUIController != null)
            {
                upgradeUIController.SetUpgradeOptions(currentUpgradeOptions);
            }
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

        if (upgradeUIController != null)
        {
            upgradeUIController.SetUpgradeOptions(currentUpgradeOptions);
        }
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
        else
        {
            ApplyUpgradeDirect(selectedUpgrade);
        }

        WebGLHelper.TriggerHapticFeedback("medium");

        CoroutineRunner.StartRoutine(ReturnToGameplayAfterDelay(0.3f));
    }

    private void ApplyUpgradeDirect(Upgrade upgrade)
    {
        if (ServiceLocator.TryGet<GameObject>(out var player) && player != null)
        {
            var combat = player.GetComponent<PlayerCombat>();
            if (combat != null)
            {
                switch (upgrade.type)
                {
                    case UpgradeType.Damage:
                        combat.UpgradeDamage(1f + upgrade.value);
                        break;
                    case UpgradeType.AttackSpeed:
                        combat.UpgradeAttackSpeed(1f + upgrade.value);
                        break;
                    case UpgradeType.AttackRange:
                        combat.UpgradeRange(1f + upgrade.value);
                        break;
                }
            }
        }

        Debug.Log($"Applied upgrade direct: {upgrade.name}");
    }

    private IEnumerator ReturnToGameplayAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        var stateMachine = ServiceLocator.Get<GameStateMachine>();
        stateMachine?.ChangeState(new GameplayState());
    }

    public override IEnumerator Exit()
    {
        Debug.Log("Exiting Upgrade Selection State");

        Time.timeScale = 1f;

        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.EnableGameplayInput();
            Debug.Log("Game input enabled");
        }

        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem != null)
        {
            uiSystem.HideUI(UPGRADE_UI_ID);
            uiSystem.UnregisterUIController(UPGRADE_UI_ID);

            if (uiSystem.GetUIController<GameplayUIController>(GAMEPLAY_UI_ID) != null)
            {
                uiSystem.ShowUI(GAMEPLAY_UI_ID);
                Debug.Log("Gameplay UI restored");
            }
        }

        upgradeUIController = null;
        currentUpgradeOptions = null;

        Debug.Log("Upgrade Selection State exited successfully");
        yield return null;
    }

    public override void Update()
    {
    }
}