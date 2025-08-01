using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameBootstrapper : MonoBehaviour
{
    [Header("Systems Configuration")]
    [SerializeField] private SystemsConfiguration systemsConfig;

    [Header("Loading UI")]
    [SerializeField] private Canvas loadingCanvas;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text loadingText;

    private GameStateMachine stateMachine;
    private SystemsInitializer systemsInitializer;
    private AddressableManager addressableManager;
    private bool isInitialized = false;

    void Awake()
    {
        // Единственный DontDestroyOnLoad объект
        if (Object.FindObjectsByType<GameBootstrapper>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        CreateLoadingScreen();
        StartCoroutine(InitializeGame());
    }

    private void CreateLoadingScreen()
    {
        // Создаем простой экран загрузки если его нет
        if (loadingCanvas == null)
        {
            var canvasGO = new GameObject("LoadingCanvas");
            canvasGO.transform.SetParent(transform);

            loadingCanvas = canvasGO.AddComponent<Canvas>();
            loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            loadingCanvas.sortingOrder = 1000; // Поверх всего

            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Фон
            var backgroundGO = new GameObject("Background");
            backgroundGO.transform.SetParent(canvasGO.transform, false);
            var backgroundImage = backgroundGO.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            var backgroundRect = backgroundImage.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            // Текст загрузки
            var textGO = new GameObject("LoadingText");
            textGO.transform.SetParent(canvasGO.transform, false);
            loadingText = textGO.AddComponent<Text>();
            loadingText.text = "Initializing Game...";
            loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            loadingText.fontSize = 48;
            loadingText.color = Color.white;
            loadingText.alignment = TextAnchor.MiddleCenter;
            var textRect = loadingText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.6f);
            textRect.anchorMax = new Vector2(1f, 0.7f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Прогресс бар
            var progressGO = new GameObject("ProgressBar");
            progressGO.transform.SetParent(canvasGO.transform, false);
            progressBar = progressGO.AddComponent<Slider>();
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;
            var progressRect = progressBar.GetComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0.2f, 0.4f);
            progressRect.anchorMax = new Vector2(0.8f, 0.5f);
            progressRect.offsetMin = Vector2.zero;
            progressRect.offsetMax = Vector2.zero;

            // Background для слайдера
            var sliderBg = new GameObject("Background");
            sliderBg.transform.SetParent(progressGO.transform, false);
            var sliderBgImage = sliderBg.AddComponent<Image>();
            sliderBgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            var sliderBgRect = sliderBgImage.GetComponent<RectTransform>();
            sliderBgRect.anchorMin = Vector2.zero;
            sliderBgRect.anchorMax = Vector2.one;
            sliderBgRect.offsetMin = Vector2.zero;
            sliderBgRect.offsetMax = Vector2.zero;
            progressBar.targetGraphic = sliderBgImage;

            // Fill для слайдера
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(progressGO.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.3f, 1f);
            var fillRect = fillImage.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            progressBar.fillRect = fillRect;
        }

        loadingCanvas.gameObject.SetActive(true);
        UpdateProgress(0f, "Starting initialization...");
    }

    private void UpdateProgress(float progress, string message)
    {
        if (progressBar != null)
            progressBar.value = progress;

        if (loadingText != null)
            loadingText.text = message;
 
    }

    private IEnumerator InitializeGame()
    {
      
        float totalSteps = 8f;
        float currentStep = 0f;

        // Step 1: Create AddressableManager
        UpdateProgress(++currentStep / totalSteps, "Initializing Addressables...");
        addressableManager = new AddressableManager();
        yield return addressableManager.Initialize();
        yield return new WaitForSeconds(0.1f); // Визуальная задержка

        // Step 2: Preload critical assets
        UpdateProgress(++currentStep / totalSteps, "Loading critical assets...");
        yield return addressableManager.PreloadCriticalAssets();
        yield return new WaitForSeconds(0.1f);

        // Step 3: Create State Machine
        UpdateProgress(++currentStep / totalSteps, "Creating State Machine...");
        stateMachine = new GameStateMachine();
        yield return new WaitForSeconds(0.1f);

        // Step 4: Register core services
        UpdateProgress(++currentStep / totalSteps, "Registering core services...");
        ServiceLocator.Register<AddressableManager>(addressableManager);
        ServiceLocator.Register<GameStateMachine>(stateMachine);

        if (systemsConfig != null)
        {
            ServiceLocator.Register<SystemsConfiguration>(systemsConfig);
        }
        else
        {
            Debug.LogWarning("SystemsConfiguration not assigned! Loading from Resources...");
            var config = Resources.Load<SystemsConfiguration>("SystemsConfiguration");
            if (config != null)
            {
                ServiceLocator.Register<SystemsConfiguration>(config);
                Debug.Log("SystemsConfiguration loaded from Resources");
            }
            else
            {
                Debug.LogError("SystemsConfiguration not found! Creating default config.");
                var defaultConfig = ScriptableObject.CreateInstance<SystemsConfiguration>();
                ServiceLocator.Register<SystemsConfiguration>(defaultConfig);
            }
        }
        yield return new WaitForSeconds(0.1f);

        // Step 5: Initialize all systems
        UpdateProgress(++currentStep / totalSteps, "Initializing game systems...");
        systemsInitializer = new SystemsInitializer();
        yield return systemsInitializer.InitializeAllSystems();
        ServiceLocator.Register<SystemsInitializer>(systemsInitializer);
        yield return new WaitForSeconds(0.2f);

        // Step 6: Warm up pools and cache critical resources
        UpdateProgress(++currentStep / totalSteps, "Preparing game resources...");
        yield return WarmupGameResources();
        yield return new WaitForSeconds(0.2f);

        // Step 7: Setup EventSystem and UI fixes
        UpdateProgress(++currentStep / totalSteps, "Setting up UI system...");
        yield return SetupUISystem();
        yield return new WaitForSeconds(0.1f);

        // Step 8: Complete initialization
        UpdateProgress(++currentStep / totalSteps, "Initialization complete!");
        yield return new WaitForSeconds(0.5f); // Показываем завершение

        isInitialized = true;

        // Скрываем экран загрузки
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(false);
        }
 
        stateMachine.ChangeState(new MainMenuState());
 
    }

    private IEnumerator WarmupGameResources()
    { 
        yield return StartCoroutine(PreloadUIResources());

        yield return null;
    }

    private IEnumerator PreloadUIResources()
    { 
        var mainMenuTask = addressableManager.LoadAssetAsync<GameObject>("MainMenuUI");
        var gameUITask = addressableManager.LoadAssetAsync<GameObject>("GameUI");

        if (mainMenuTask == null || gameUITask == null)
        {
            Debug.LogWarning("Failed to start UI preload tasks");
            yield break;
        }

        // Ждем загрузки но не создаем объекты
        float timer = 0f;
        float timeout = 3f;

        while ((!mainMenuTask.IsCompleted || !gameUITask.IsCompleted) && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (timer >= timeout)
        {
            Debug.LogWarning("UI preload timeout - continuing without preload");
        }
       
    }

    private IEnumerator SetupUISystem()
    {
        // Создаем или находим EventSystem
        var eventSystem = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemGO = new GameObject("EventSystem");
            DontDestroyOnLoad(eventSystemGO);
            eventSystem = eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();

            // Пытаемся добавить InputSystemUIInputModule
            bool inputModuleAdded = TryAddInputSystemModule(eventSystemGO);

            if (!inputModuleAdded)
            {
                // Fallback к StandaloneInputModule
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                
            }
        }

        yield return null;
    }

    private bool TryAddInputSystemModule(GameObject eventSystemGO)
    {
        try
        {
            eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
           
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to add InputSystemUIInputModule: {e.Message}");
            return false;
        }
    }
 
    private void OnDestroy()
    { 
        if (systemsInitializer != null)
        {
            systemsInitializer.Cleanup();
            systemsInitializer = null;
        }

        if (addressableManager != null)
        {
            addressableManager.Cleanup();
            addressableManager = null;
        }

        stateMachine = null;
        ServiceLocator.Clear();

        
    }

    // Публичные методы для проверки состояния
    public bool IsInitialized() => isInitialized;
 
 
    public void ShowLoadingScreen()
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(true);
            UpdateProgress(0.5f, "Debug loading screen");
        }
    }

    [ContextMenu("Hide Loading Screen")]
    public void HideLoadingScreen()
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(false);
        }
    }

    public void RestartGame()
    {
        Debug.Log("GameBootstrapper: Starting game restart...");
        StartCoroutine(RestartGameCoroutine());
    }

    private IEnumerator RestartGameCoroutine()
    {
        // Показываем экран загрузки
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(true);
            UpdateProgress(0f, "Restarting game...");
        }

        // Останавливаем текущие системы
        if (systemsInitializer != null)
        {
            UpdateProgress(0.2f, "Stopping systems...");
            systemsInitializer.Cleanup();
            yield return new WaitForSeconds(0.1f);
        }

        // Очищаем Service Locator (кроме базовых сервисов)
        UpdateProgress(0.4f, "Clearing services...");
        var stateMachine = ServiceLocator.Get<GameStateMachine>();
        var addressableManager = ServiceLocator.Get<AddressableManager>();
        var systemsConfig = ServiceLocator.Get<SystemsConfiguration>();

        ServiceLocator.Clear();

        // Восстанавливаем базовые сервисы
        if (addressableManager != null) ServiceLocator.Register<AddressableManager>(addressableManager);
        if (stateMachine != null) ServiceLocator.Register<GameStateMachine>(stateMachine);
        if (systemsConfig != null) ServiceLocator.Register<SystemsConfiguration>(systemsConfig);

        // Переинициализируем системы
        UpdateProgress(0.6f, "Reinitializing systems...");
        systemsInitializer = new SystemsInitializer();
        yield return systemsInitializer.InitializeAllSystems();
        ServiceLocator.Register<SystemsInitializer>(systemsInitializer);

        UpdateProgress(0.8f, "Preparing restart...");
        yield return new WaitForSeconds(0.2f);

        // Скрываем экран загрузки
        UpdateProgress(1.0f, "Restart complete!");
        yield return new WaitForSeconds(0.5f);

        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(false);
        }

        Debug.Log("GameBootstrapper: Game restart completed");
    }

    public static void RequestGameRestart()
    {
        var bootstrapper = Object.FindObjectOfType<GameBootstrapper>();
        if (bootstrapper != null)
        {
            bootstrapper.RestartGame();
        }
        else
        {
            Debug.LogError("GameBootstrapper not found for restart!");
        }
    }
}