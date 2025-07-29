using UnityEngine;

public class MainMenuUIController : BaseUIController
{
    private const string START_BUTTON = "StartButton";
    private const string SETTINGS_BUTTON = "SettingsButton";
    private const string EXIT_BUTTON = "ExitButton";
    private const string TITLE_TEXT = "TitleText";

    public MainMenuUIController() : base("MainMenuUI")
    {
    }

    protected override void OnShow()
    {
        base.OnShow();
       
        // Устанавливаем заголовок игры
        SetText(TITLE_TEXT, "Survivors Game");

        // Убеждаемся, что все кнопки активны
        SetButtonInteractable(START_BUTTON, true);
        SetButtonInteractable(SETTINGS_BUTTON, true);
        SetButtonInteractable(EXIT_BUTTON, true);
    }

    protected override void HandleButtonClick(string buttonName)
    {
        switch (buttonName)
        {
            case START_BUTTON:
                StartGame();
                break;

            case SETTINGS_BUTTON:
                OpenSettings();
                break;

            case EXIT_BUTTON:
                ExitGame();
                break;

            default:
                Debug.LogWarning($"Unhandled button click: {buttonName}");
                break;
        }
    }

    private void StartGame()
    { 
        SetButtonInteractable(START_BUTTON, false);

        // Получаем State Machine и переходим к геймплею
        var stateMachine = ServiceLocator.Get<GameStateMachine>();
        if (stateMachine != null)
        { 
            stateMachine.ChangeState(new GameplayState());
        }
        else
        {
            Debug.LogError("GameStateMachine not found! Cannot start game.");
            // Возвращаем кнопку в активное состояние если переход не удался
            SetButtonInteractable(START_BUTTON, true);
        }
    }

    private void OpenSettings()
    {
        Debug.Log("Settings button clicked");

        // Пока что просто логируем, позже можно добавить настройки
        // var uiSystem = ServiceLocator.Get<UISystem>();
        // uiSystem?.ShowUI("SettingsUI");

        Debug.Log("Settings UI not implemented yet");
    }

    private void ExitGame()
    {
        Debug.Log("Exit Game button clicked");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Публичный метод для внешнего управления кнопками
    /// </summary>
    public void SetStartButtonEnabled(bool enabled)
    {
        SetButtonInteractable(START_BUTTON, enabled);
    }

    /// <summary>
    /// Устанавливает текст заголовка
    /// </summary>
    public void SetGameTitle(string title)
    {
        SetText(TITLE_TEXT, title);
    }

    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        // Здесь можно добавить логику анимации или обновления UI
        // Например, мигание кнопки "Start" или анимация заголовка
    }

    protected override void OnHide()
    {
        base.OnHide(); 
    }

    protected override void OnCleanup()
    {
        base.OnCleanup();
        
    }
}