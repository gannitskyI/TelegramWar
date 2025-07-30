using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayState : GameState
{
    private float gameTimer;
    private bool isRoundActive;
    private GameObject playerInstance;
    private GameplayUIController gameplayUIController;
    private const string GAMEPLAY_UI_ID = "GameUI";
    private bool isExiting = false;
    private bool playerCreated = false;

    public override IEnumerator Enter()
    {
        isExiting = false;
        playerCreated = false;

        gameTimer = 0f; // �������������� ����� � 0, ������ ���� ������
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
        ActivateGameplaySystems();
        EnableGameplayInput();
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

        gameplayUIController = new GameplayUIController();
        uiSystem.RegisterUIController(GAMEPLAY_UI_ID, gameplayUIController);

        yield return new WaitForSeconds(0.2f);
        uiSystem.ShowUI(GAMEPLAY_UI_ID);
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
        Debug.Log("Starting player creation");

        ClearExistingPlayers();
        yield return null;

        Vector3 spawnPosition = GetPlayerSpawnPosition();
        Debug.Log($"Player spawn position: {spawnPosition}");

        var addressableManager = ServiceLocator.Get<AddressableManager>();

        if (addressableManager != null)
        {
            var playerTask = addressableManager.InstantiateAsync("PlayerPrefab", spawnPosition);
            yield return new WaitUntil(() => playerTask.IsCompleted);

            if (playerTask.Result != null)
            {
                playerInstance = playerTask.Result;
                Debug.Log($"Player created from PlayerPrefab: {playerInstance.name}");

                SetupPlayerComponents();
                RegisterPlayer();
                yield break;
            }
            else
            {
                Debug.LogError("PlayerPrefab failed to instantiate!");
            }
        }
        else
        {
            Debug.LogError("AddressableManager is null!");
        }

        Debug.LogError("Failed to create player - game cannot continue");
    }

    private void ClearExistingPlayers()
    {
        var existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var existingPlayer in existingPlayers)
        {
            Debug.LogWarning($"Destroying existing player: {existingPlayer.name}");
            Object.Destroy(existingPlayer);
        }

        var fallbackPlayers = GameObject.FindObjectsOfType<GameObject>();
        foreach (var obj in fallbackPlayers)
        {
            if (obj.name.Contains("FallbackPlayer"))
            {
                Debug.LogWarning($"Destroying fallback player: {obj.name}");
                Object.Destroy(obj);
            }
        }
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

        Debug.Log($"Setting up components for player: {playerInstance.name}");

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
    }

    private IEnumerator SafeInitializeComponent(IInitializable component, string componentName)
    {
        if (component == null)
        {
            Debug.LogWarning($"Component {componentName} is null");
            yield break;
        }

        yield return component.Initialize();
        Debug.Log($"Component {componentName} initialized successfully");
    }

    private void RegisterPlayer()
    {
        if (playerInstance != null)
        {
            ServiceLocator.Unregister<GameObject>();
            ServiceLocator.Register<GameObject>(playerInstance);
            ServiceLocator.RegisterWeak<GameObject>(playerInstance);
            Debug.Log($"Player registered in ServiceLocator: {playerInstance.name}");
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
        if (isExiting) return;

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

    public override void Update()
    {
        if (!isRoundActive || isExiting || !playerCreated) return;

        if (playerInstance == null)
        {
            Debug.LogError("Player instance is null during gameplay! This should not happen.");
            return;
        }

        gameTimer += Time.deltaTime; // ����� ���� ������

        if (gameplayUIController != null)
        {
            gameplayUIController.UpdateTimer(gameTimer);
        }
    }

    public override IEnumerator Exit()
    {
        isExiting = true;
        isRoundActive = false;
        playerCreated = false;

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

        yield return null;
    }
}