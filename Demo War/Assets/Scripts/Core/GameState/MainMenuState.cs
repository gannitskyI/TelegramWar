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

        // Создаем и регистрируем контроллер главного меню
        mainMenuController = new MainMenuUIController();
        uiSystem.RegisterUIController(MAIN_MENU_UI_ID, mainMenuController);

        // Показываем главное меню
        uiSystem.ShowUI(MAIN_MENU_UI_ID);

        // Отключаем игровой инпут
        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.DisableAllInput();
            
        }

        // Останавливаем все игровые системы
        StopGameplaySystems();
 
        yield return null;
    }

    private void StopGameplaySystems()
    {
        // Останавливаем спавн врагов
        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StopSpawning();
        }

        // Очищаем существующих врагов
        ClearExistingEnemies();
    }

    private void ClearExistingEnemies()
    {
        // Находим всех врагов на сцене и удаляем их
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
        // В главном меню особой логики update не требуется
        // UI система сама обновит активные контроллеры
    }

    public override IEnumerator Exit()
    {
      
        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem != null)
        {
            // Скрываем и отключаем главное меню
            uiSystem.HideUI(MAIN_MENU_UI_ID);
            uiSystem.UnregisterUIController(MAIN_MENU_UI_ID);
        }

        // Очищаем ссылку на контроллер
        mainMenuController = null;
 
        yield return null;
    }

    /// <summary>
    /// Метод для возврата в главное меню из других состояний
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