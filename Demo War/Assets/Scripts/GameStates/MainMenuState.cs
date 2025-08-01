using System.Collections;
using UnityEngine;

public class MainMenuState : GameState
{
    private const string MAIN_MENU_UI_ID = "MainMenu";
    private MainMenuUIController mainMenuController;

    public override IEnumerator Enter()
    { 
        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem == null)
        {
            Debug.LogError("UISystem not found! Cannot show main menu.");
            yield break;
        }

        // ������� � ������������ ���������� �������� ����
        mainMenuController = new MainMenuUIController();
        uiSystem.RegisterUIController(MAIN_MENU_UI_ID, mainMenuController);

        // ���������� ������� ����
        uiSystem.ShowUI(MAIN_MENU_UI_ID);

        // ��������� ������� �����
        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.DisableAllInput();
            
        }

        // ������������� ��� ������� �������
        StopGameplaySystems();
 
        yield return null;
    }

    private void StopGameplaySystems()
    {
        // ������������� ����� ������
        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StopSpawning();
        }

        // ������� ������������ ������
        ClearExistingEnemies();
    }

    private void ClearExistingEnemies()
    {
        // ������� ���� ������ �� ����� � ������� ��
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            Object.Destroy(enemy);
        }

        if (enemies.Length > 0)
        {
            Debug.Log($"Cleared {enemies.Length} enemies from scene");
        }
    }

    public override void Update()
    {
        // � ������� ���� ������ ������ update �� ���������
        // UI ������� ���� ������� �������� �����������
    }

    public override IEnumerator Exit()
    {
      
        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem != null)
        {
            // �������� � ��������� ������� ����
            uiSystem.HideUI(MAIN_MENU_UI_ID);
            uiSystem.UnregisterUIController(MAIN_MENU_UI_ID);
        }

        // ������� ������ �� ����������
        mainMenuController = null;
 
        yield return null;
    }

    /// <summary>
    /// ����� ��� �������� � ������� ���� �� ������ ���������
    /// </summary>
    public static void ReturnToMainMenu()
    {
        Debug.Log("Returning to main menu");

        var stateMachine = ServiceLocator.Get<GameStateMachine>();
        if (stateMachine != null)
        {
            stateMachine.ChangeState(new MainMenuState());
        }
        else
        {
            Debug.LogError("GameStateMachine not found! Cannot return to main menu.");
        }
    }
}