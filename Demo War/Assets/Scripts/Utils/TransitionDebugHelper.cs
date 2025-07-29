using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Помощник для отладки переходов между состояниями и сценами
/// </summary>
public class TransitionDebugHelper : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool logStateChanges = true;
    [SerializeField] private bool logButtonClicks = true;

    void Start()
    {
        // Автоматическая проверка при старте
        Invoke(nameof(CheckGameState), 2f);
    }

    [ContextMenu("Check Game State")]
    public void CheckGameState()
    {
        Debug.Log("=== GAME STATE DEBUG ===");

        // 1. Проверяем текущую сцену
        CheckCurrentScene();

        // 2. Проверяем GameStateMachine
        CheckStateMachine();

        // 3. Проверяем UI систему
        CheckUISystem();

        // 4. Проверяем кнопки
        CheckButtonCallbacks();
 
    }

    private void CheckCurrentScene()
    {
        var currentScene = SceneManager.GetActiveScene();
        Debug.Log($"?? Current Scene: {currentScene.name}");
        Debug.Log($"   - Scene Index: {currentScene.buildIndex}");
        Debug.Log($"   - Scene Path: {currentScene.path}");
        Debug.Log($"   - Is Loaded: {currentScene.isLoaded}");

        // Проверяем все загруженные сцены
        int sceneCount = SceneManager.sceneCount;
        Debug.Log($"?? Total loaded scenes: {sceneCount}");
        for (int i = 0; i < sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            Debug.Log($"   - Scene {i}: {scene.name} (active: {scene == currentScene})");
        }
    }

    private void CheckStateMachine()
    {
        Debug.Log("?? Checking GameStateMachine...");

        if (ServiceLocator.TryGet<GameStateMachine>(out var stateMachine))
        {
            Debug.Log("? GameStateMachine found in ServiceLocator");

            // Пытаемся получить информацию о текущем состоянии через рефлексию
            var stateMachineType = stateMachine.GetType();
            var currentStateField = stateMachineType.GetField("currentState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (currentStateField != null)
            {
                var currentState = currentStateField.GetValue(stateMachine);
                if (currentState != null)
                {
                    Debug.Log($"? Current State: {currentState.GetType().Name}");
                }
                else
                {
                    Debug.LogError("? Current State is NULL!");
                }
            }
            else
            {
                Debug.LogWarning("?? Cannot access currentState field");
            }
        }
        else
        {
            Debug.LogError("? GameStateMachine NOT FOUND in ServiceLocator!");
        }
    }

    private void CheckUISystem()
    {
        Debug.Log("??? Checking UI System...");

        if (ServiceLocator.TryGet<UISystem>(out var uiSystem))
        {
            Debug.Log("? UISystem found");

            // Проверяем активные UI
            Debug.Log($"   - MainMenu active: {uiSystem.IsUIActive("MainMenu")}");
            Debug.Log($"   - GameplayUI active: {uiSystem.IsUIActive("GameplayUI")}");
        }
        else
        {
            Debug.LogError("? UISystem NOT FOUND!");
        }
    }

    private void CheckButtonCallbacks()
    {
        Debug.Log("?? Checking Button Callbacks...");

        var buttons = FindObjectsOfType<UnityEngine.UI.Button>();
        foreach (var button in buttons)
        {
            Debug.Log($"Button: {button.name}");
            Debug.Log($"   - Listener Count: {button.onClick.GetPersistentEventCount()}");
            Debug.Log($"   - Interactable: {button.interactable}");
            Debug.Log($"   - Active: {button.gameObject.activeInHierarchy}");

            // Проверяем если это StartButton
            if (button.name == "StartButton")
            {
                Debug.Log("?? Found StartButton - checking callbacks...");
                // Можем попробовать симулировать клик
                // button.onClick.Invoke();
            }
        }
    }
 

    [ContextMenu("Simulate Start Button Click")]
    public void SimulateStartButtonClick()
    {
        Debug.Log("?? Simulating Start Button Click...");

        var startButton = GameObject.Find("StartButton")?.GetComponent<UnityEngine.UI.Button>();
        if (startButton != null)
        {
            Debug.Log("? StartButton found, invoking click...");
            startButton.onClick.Invoke();
        }
        else
        {
            Debug.LogError("? StartButton not found!");

            // Ищем все кнопки
            var buttons = FindObjectsOfType<UnityEngine.UI.Button>();
            Debug.Log($"Available buttons: {string.Join(", ", System.Array.ConvertAll(buttons, b => b.name))}");
        }
    }

    [ContextMenu("Force Transition to Gameplay")]
    public void ForceTransitionToGameplay()
    {
        Debug.Log("?? Forcing transition to GameplayState...");

        if (ServiceLocator.TryGet<GameStateMachine>(out var stateMachine))
        {
            try
            {
                stateMachine.ChangeState(new GameplayState());
                Debug.Log("? Transition initiated");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"? Transition failed: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("? Cannot transition - GameStateMachine not found!");
        }
    }

    [ContextMenu("Check MainMenuController")]
    public void CheckMainMenuController()
    {
        Debug.Log("?? Checking MainMenuController...");

        if (ServiceLocator.TryGet<UISystem>(out var uiSystem))
        {
            var mainMenuController = uiSystem.GetUIController<MainMenuUIController>("MainMenu");
            if (mainMenuController != null)
            {
                Debug.Log("? MainMenuUIController found");
                Debug.Log($"   - Is Visible: {mainMenuController.IsVisible()}");
            }
            else
            {
                Debug.LogError("? MainMenuUIController not found!");
            }
        }
    }

    [ContextMenu("Log Button Click Chain")]
    public void LogButtonClickChain()
    {
        Debug.Log("?? Button Click Chain Analysis:");
        Debug.Log("1. Button Click ? MainMenuUIController.HandleButtonClick");
        Debug.Log("2. HandleButtonClick ? StartGame()");
        Debug.Log("3. StartGame() ? GameStateMachine.ChangeState(GameplayState)");
        Debug.Log("4. GameplayState.Enter() ? Setup UI, Scene, etc.");
        Debug.Log("");
        Debug.Log("Let's check each step...");

        CheckMainMenuController();
        CheckStateMachine();
    }

    // Метод для мониторинга изменений состояния
    private GameState lastKnownState = null;

    void Update()
    {
        if (!logStateChanges) return;

        // Мониторим изменения состояния
        if (ServiceLocator.TryGet<GameStateMachine>(out var stateMachine))
        {
            // Через рефлексию получаем текущее состояние
            var stateMachineType = stateMachine.GetType();
            var currentStateField = stateMachineType.GetField("currentState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (currentStateField != null)
            {
                var currentState = currentStateField.GetValue(stateMachine) as GameState;

                if (currentState != lastKnownState)
                {
                    var oldStateName = lastKnownState?.GetType().Name ?? "NULL";
                    var newStateName = currentState?.GetType().Name ?? "NULL";
                    Debug.Log($"?? State Change: {oldStateName} ? {newStateName}");
                    lastKnownState = currentState;
                }
            }
        }
    }
}