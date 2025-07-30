using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayState : PausableGameState
{
    private float gameTimer;
    private bool isRoundActive;
    private GameObject playerInstance;
    private GameplayUIController gameplayUIController;
    private const string GAMEPLAY_UI_ID = "GameUI";
    private bool isExiting = false;
    private bool playerCreated = false;
    private bool forceReset = false;

    public GameplayState(bool forceReset = false)
    {
        this.forceReset = forceReset;
    }

    public static GameplayState CreateRestart()
    {
        return new GameplayState(forceReset: true);
    }

    public override IEnumerator Enter()
    {
        Debug.Log($"GameplayState Enter - wasInitialized: {wasInitialized}, isPaused: {isPaused}, forceReset: {forceReset}");

        if (forceReset)
        {
            Debug.Log("Force resetting GameplayState for restart");
            wasInitialized = false;
            isPaused = false;

            if (playerInstance != null)
            {
                Object.Destroy(playerInstance);
                playerInstance = null;
            }
        }

        if (wasInitialized && !forceReset)
        {
            Debug.Log("Resuming existing gameplay - no recreation");
            Resume();

            var uiSystem = ServiceLocator.Get<UISystem>();
            if (uiSystem != null && gameplayUIController != null)
            {
                if (!uiSystem.IsUIActive(GAMEPLAY_UI_ID))
                {
                    uiSystem.ShowUI(GAMEPLAY_UI_ID);
                }
            }
            yield break;
        }

        Debug.Log("First time initialization of gameplay state");
        isExiting = false;
        playerCreated = false;
        gameTimer = 0f;
        isRoundActive = true;

        yield return SetupGameplayUI();
        if (isExiting) yield break;

        yield return LoadGameplayScene();
        if (isExiting) yield break;

        yield return CreatePlayer();
        if (isExiting) yield break;

        if (playerInstance == null)
        {
            Debug.LogError("Player creation failed completely!");
            yield break;
        }

        playerCreated = true;
        wasInitialized = true;
        ActivateGameplaySystems();
        EnableGameplayInput();

        Debug.Log("GameplayState fully initialized");
    }

    protected override void OnPause()
    {
        Debug.Log("GameplayState paused - stopping systems but keeping objects");

        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StopSpawning();
        }

        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.DisableAllInput();
        }

        Time.timeScale = 0f;
    }

    protected override void OnResume()
    {
        Debug.Log("GameplayState resumed - restarting systems with existing objects");

        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StartSpawning();
        }

        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.EnableGameplayInput();
        }

        Time.timeScale = 1f;
    }

    private IEnumerator SetupGameplayUI()
    {
        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem == null)
        {
            Debug.LogError("UISystem not found!");
            yield break;
        }

        if (uiSystem.IsUIActive("MainMenu"))
        {
            uiSystem.HideUI("MainMenu");
        }

        if (gameplayUIController == null)
        {
            gameplayUIController = new GameplayUIController();
            uiSystem.RegisterUIController(GAMEPLAY_UI_ID, gameplayUIController);
        }

        yield return new WaitForSeconds(0.2f);

        if (!uiSystem.IsUIActive(GAMEPLAY_UI_ID))
        {
            uiSystem.ShowUI(GAMEPLAY_UI_ID);
        }

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator LoadGameplayScene()
    {
        var addressableManager = ServiceLocator.Get<AddressableManager>();
        if (addressableManager == null)
        {
            Debug.LogError("AddressableManager not found!");
            yield break;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

        var sceneHandle = addressableManager.LoadSceneAsync("GameplayScene");
        if (sceneHandle.IsValid())
        {
            yield return new WaitUntil(() => sceneHandle.IsValid() && sceneHandle.IsDone);
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");
    }

    private IEnumerator CreatePlayer()
    {
        if (playerInstance != null)
        {
            Debug.Log("Player already exists, skipping creation");
            yield break;
        }

        Debug.Log("Creating player for the first time");

        Vector3 spawnPosition = GetPlayerSpawnPosition();
        var addressableManager = ServiceLocator.Get<AddressableManager>();

        if (addressableManager != null)
        {
            var playerTask = addressableManager.InstantiateAsync("PlayerPrefab", spawnPosition);
            yield return new WaitUntil(() => playerTask.IsCompleted);

            if (playerTask.Result != null)
            {
                playerInstance = playerTask.Result;
                Debug.Log($"Player created: {playerInstance.name}");

                SetupPlayerComponents();
                RegisterPlayer();
                yield break;
            }
        }

        Debug.LogError("Failed to create player");
    }

    private Vector3 GetPlayerSpawnPosition()
    {
        var camera = Camera.main;
        if (camera != null)
        {
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, camera.nearClipPlane);
            Vector3 worldCenter = camera.ScreenToWorldPoint(screenCenter);
            return new Vector3(worldCenter.x, worldCenter.y, 0f);
        }
        return Vector3.zero;
    }

    private void SetupPlayerComponents()
    {
        if (playerInstance == null) return;

        var rb3d = playerInstance.GetComponent<Rigidbody>();
        if (rb3d != null)
        {
            Object.DestroyImmediate(rb3d);
        }

        var rb2d = playerInstance.GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = playerInstance.AddComponent<Rigidbody2D>();
        }

        rb2d.gravityScale = 0f;
        rb2d.linearDamping = 5f;
        rb2d.freezeRotation = true;

        var playerCombat = playerInstance.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            CoroutineRunner.StartRoutine(SafeInitializeComponent(playerCombat, "PlayerCombat"));
        }

        var playerMovement = playerInstance.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            CoroutineRunner.StartRoutine(SafeInitializeComponent(playerMovement, "PlayerMovement"));
        }

        var playerHealth = playerInstance.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDied += OnPlayerDied;
            playerHealth.ResetHealth();
        }

        var collider = playerInstance.GetComponent<Collider2D>();
        if (collider == null)
        {
            var circleCollider = playerInstance.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
            circleCollider.radius = 0.5f;
        }

        try
        {
            playerInstance.tag = "Player";
        }
        catch
        {
            Debug.LogWarning("Could not set Player tag");
        }

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            playerInstance.layer = playerLayer;
        }
    }

    private IEnumerator SafeInitializeComponent(IInitializable component, string componentName)
    {
        if (component == null) yield break;

        yield return component.Initialize();
        Debug.Log($"Component {componentName} initialized");
    }

    private void RegisterPlayer()
    {
        if (playerInstance != null)
        {
            ServiceLocator.Unregister<GameObject>();
            ServiceLocator.Register<GameObject>(playerInstance);
            ServiceLocator.RegisterWeak<GameObject>(playerInstance);
            Debug.Log($"Player registered: {playerInstance.name}");
        }
    }

    private void ActivateGameplaySystems()
    {
        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StartSpawning();
            Debug.Log("SpawnSystem activated");
        }
    }

    private void EnableGameplayInput()
    {
        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.EnableGameplayInput();
            Debug.Log("Gameplay input enabled");
        }
    }

    private void OnPlayerDied()
    {
        if (isExiting || isPaused) return;

        Debug.Log("Player died event triggered");
        CoroutineRunner.StartRoutine(EndGameplayAfterDelay());
    }

    private IEnumerator EndGameplayAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        int finalScore = 0;
        if (ServiceLocator.TryGet<ScoreSystem>(out var scoreSystem))
        {
            finalScore = scoreSystem.GetCurrentScore();
        }

        var stateMachine = ServiceLocator.Get<GameStateMachine>();
        if (stateMachine != null)
        {
            stateMachine.ChangeState(new GameOverState(finalScore));
        }
    }

    protected override void OnUpdate()
    {
        if (!isRoundActive || isExiting || !playerCreated) return;

        if (playerInstance == null)
        {
            Debug.LogError("Player instance is null during gameplay!");
            return;
        }

        gameTimer += Time.deltaTime;

        if (gameplayUIController != null)
        {
            gameplayUIController.UpdateTimer(gameTimer);
        }
    }

    public override IEnumerator Exit()
    {
        Debug.Log("GameplayState Exit called");

        isExiting = true;
        isRoundActive = false;

        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StopSpawning();
        }

        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem != null)
        {
            if (uiSystem.IsUIActive(GAMEPLAY_UI_ID))
            {
                uiSystem.HideUI(GAMEPLAY_UI_ID);
            }
            uiSystem.UnregisterUIController(GAMEPLAY_UI_ID);
        }

        if (playerInstance != null)
        {
            var playerHealth = playerInstance.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnPlayerDied -= OnPlayerDied;
            }

            var playerCombat = playerInstance.GetComponent<PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.Cleanup();
            }

            var playerMovement = playerInstance.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.Cleanup();
            }

            var addressableManager = ServiceLocator.Get<AddressableManager>();
            if (addressableManager != null)
            {
                addressableManager.ReleaseAsset(playerInstance);
            }
            else
            {
                Object.Destroy(playerInstance);
            }
            playerInstance = null;
        }

        ServiceLocator.Unregister<GameObject>();
        gameplayUIController = null;
        wasInitialized = false;
        playerCreated = false;

        yield return null;
    }

    public float GetGameTimer() => gameTimer;
    public bool IsRoundActive() => isRoundActive && !isPaused;
    public GameObject GetPlayerInstance() => playerInstance;
}