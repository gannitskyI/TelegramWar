using UnityEngine;

public class GameOverUIController : BaseUIController
{
    private const string TITLE_TEXT = "TitleText";
    private const string SCORE_TEXT = "ScoreText";
    private const string RESTART_BUTTON = "RestartButton";
    private const string MENU_BUTTON = "MenuButton";
    private const string EXIT_BUTTON = "ExitButton";

    private int finalScore;

    public GameOverUIController(int score) : base("GameOverUI")
    {
        finalScore = score;
    }

    protected override void OnShow()
    {
        base.OnShow();

        SetText(TITLE_TEXT, "GAME OVER");
        SetText(SCORE_TEXT, $"Final Score: {finalScore}");

        SetButtonInteractable(RESTART_BUTTON, true);
        SetButtonInteractable(MENU_BUTTON, true);
        SetButtonInteractable(EXIT_BUTTON, true);
    }

    protected override void ValidateRequiredComponents()
    {
        base.ValidateRequiredComponents();

        string[] requiredButtons = { RESTART_BUTTON, MENU_BUTTON, EXIT_BUTTON };
        string[] requiredTexts = { TITLE_TEXT, SCORE_TEXT };

        foreach (var buttonName in requiredButtons)
        {
            if (!buttons.ContainsKey(buttonName))
            {
                Debug.LogError($"Required button missing: {buttonName}");
            }
        }

        foreach (var textName in requiredTexts)
        {
            if (!texts.ContainsKey(textName) && !tmpTexts.ContainsKey(textName))
            {
                Debug.LogError($"Required text component missing: {textName}");
            }
        }
    }

    protected override void CreateFallbackComponents()
    {
        CreateFallbackBackground();
        CreateFallbackTitle();
        CreateFallbackScoreText();
        CreateFallbackButtons();
    }

    private void CreateFallbackBackground()
    {
        var backgroundGO = new GameObject("Background");
        backgroundGO.transform.SetParent(uiGameObject.transform, false);

        var image = backgroundGO.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0f, 0f, 0f, 0.9f);

        var rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void CreateFallbackTitle()
    {
        var titleGO = new GameObject(TITLE_TEXT);
        titleGO.transform.SetParent(uiGameObject.transform, false);

        var titleText = titleGO.AddComponent<UnityEngine.UI.Text>();
        titleText.text = "GAME OVER";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 64;
        titleText.color = Color.red;
        titleText.alignment = TextAnchor.MiddleCenter;

        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.7f);
        titleRect.anchorMax = new Vector2(0.9f, 0.85f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        texts[TITLE_TEXT] = titleText;
    }

    private void CreateFallbackScoreText()
    {
        var scoreGO = new GameObject(SCORE_TEXT);
        scoreGO.transform.SetParent(uiGameObject.transform, false);

        var scoreText = scoreGO.AddComponent<UnityEngine.UI.Text>();
        scoreText.text = $"Final Score: {finalScore}";
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = 36;
        scoreText.color = Color.white;
        scoreText.alignment = TextAnchor.MiddleCenter;

        var scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0.1f, 0.55f);
        scoreRect.anchorMax = new Vector2(0.9f, 0.65f);
        scoreRect.offsetMin = Vector2.zero;
        scoreRect.offsetMax = Vector2.zero;

        texts[SCORE_TEXT] = scoreText;
    }

    private void CreateFallbackButtons()
    {
        CreateGameOverButton(RESTART_BUTTON, "Restart Game", 0.4f, new Color(0.2f, 0.7f, 0.2f, 0.8f));
        CreateGameOverButton(MENU_BUTTON, "Main Menu", 0.25f, new Color(0.2f, 0.2f, 0.7f, 0.8f));
        CreateGameOverButton(EXIT_BUTTON, "Exit Game", 0.1f, new Color(0.7f, 0.2f, 0.2f, 0.8f));
    }

    private void CreateGameOverButton(string buttonName, string buttonText, float yPosition, Color buttonColor)
    {
        var buttonGO = new GameObject(buttonName);
        buttonGO.transform.SetParent(uiGameObject.transform, false);

        var button = buttonGO.AddComponent<UnityEngine.UI.Button>();
        var buttonImage = buttonGO.AddComponent<UnityEngine.UI.Image>();
        buttonImage.color = buttonColor;

        var buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.25f, yPosition - 0.05f);
        buttonRect.anchorMax = new Vector2(0.75f, yPosition + 0.05f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        var textGO = new GameObject($"{buttonName}Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        var text = textGO.AddComponent<UnityEngine.UI.Text>();
        text.text = buttonText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        buttons[buttonName] = button;
    }

    protected override void HandleButtonClick(string buttonName)
    {
        DisableAllButtons();

        switch (buttonName)
        {
            case RESTART_BUTTON:
                RestartGame();
                break;

            case MENU_BUTTON:
                ReturnToMainMenu();
                break;

            case EXIT_BUTTON:
                ExitGame();
                break;

            default:
                Debug.LogWarning($"Unhandled button click: {buttonName}");
                break;
        }
    }

    private void DisableAllButtons()
    {
        SetButtonInteractable(RESTART_BUTTON, false);
        SetButtonInteractable(MENU_BUTTON, false);
        SetButtonInteractable(EXIT_BUTTON, false);
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;

        ResetAllSystems();

        // Используем GameBootstrapper для полного рестарта
        GameBootstrapper.RequestGameRestart();

        // После рестарта систем переходим к новому геймплею
        CoroutineRunner.StartRoutine(StartNewGameAfterRestart());
    }

    private System.Collections.IEnumerator StartNewGameAfterRestart()
    {
        // Ждем завершения рестарта систем
        yield return new WaitForSeconds(1.0f);

        var stateMachine = ServiceLocator.Get<GameStateMachine>();
        if (stateMachine != null)
        {
            stateMachine.ChangeState(GameplayState.CreateRestart());
        }
        else
        {
            Debug.LogError("GameStateMachine not found after restart!");
        }
    }

    private void ResetAllSystems()
    {
        Debug.Log("Starting system reset for restart...");

        if (ServiceLocator.TryGet<ScoreSystem>(out var scoreSystem))
        {
            scoreSystem.ResetForRestart();
            Debug.Log("ScoreSystem reset for restart");
        }

        if (ServiceLocator.TryGet<UpgradeSystem>(out var upgradeSystem))
        {
            upgradeSystem.ResetUpgrades();
            Debug.Log("UpgradeSystem reset for restart");
        }

        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.Cleanup();
            Debug.Log("SpawnSystem reset for restart");
        }

        ExperienceParticle.ClearAllPools();
        GamePauseManager.Instance.Cleanup();

        var allEnemies = Object.FindObjectsOfType<EnemyBehaviour>();
        foreach (var enemy in allEnemies)
        {
            if (enemy != null)
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        var allProjectiles = Object.FindObjectsOfType<EnemyProjectile>();
        foreach (var projectile in allProjectiles)
        {
            if (projectile != null)
            {
                Object.Destroy(projectile.gameObject);
            }
        }

        var allExperienceParticles = Object.FindObjectsOfType<ExperienceParticle>();
        foreach (var particle in allExperienceParticles)
        {
            if (particle != null)
            {
                Object.Destroy(particle.gameObject);
            }
        }

        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem != null)
        {
            if (uiSystem.IsUIActive("GameUI"))
            {
                uiSystem.HideUI("GameUI");
            }
            uiSystem.UnregisterUIController("GameUI");
        }

        if (ServiceLocator.TryGet<GameObject>(out var oldPlayer))
        {
            Object.Destroy(oldPlayer);
            ServiceLocator.Unregister<GameObject>();
        }

        Debug.Log("All systems reset for game restart - scene cleared");
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

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

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        AnimateTitle();
    }

    private void AnimateTitle()
    {
        if (texts.TryGetValue(TITLE_TEXT, out var titleText))
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 2f) * 0.1f;
            titleText.transform.localScale = Vector3.one * pulse;
        }
    }

    public void SetFinalScore(int score)
    {
        finalScore = score;
        SetText(SCORE_TEXT, $"Final Score: {finalScore}");
    }

    protected override void OnCleanup()
    {
        base.OnCleanup();
        Time.timeScale = 1f;
    }
}