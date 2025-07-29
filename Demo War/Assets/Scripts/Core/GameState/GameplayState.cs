using System.Collections;
using UnityEngine;

public class GameplayState : GameState
{
    private float gameTimer;
    private bool isRoundActive;
    private GameObject playerInstance;
    private GameplayUIController gameplayUIController;
    private const string GAMEPLAY_UI_ID = "GameUI"; // Изменено с "GameplayUI" на "GameUI"
    private bool isExiting = false;

    public override IEnumerator Enter()
    {
        isExiting = false;

        var config = ServiceLocator.Get<SystemsConfiguration>();
        if (config == null)
        {
            Debug.LogError("SystemsConfiguration not found!");
            gameTimer = 30f;
        }
        else
        {
            gameTimer = config.roundDuration;
        }

        isRoundActive = true;

        yield return SetupGameplayUI();
        if (isExiting) yield break;

        yield return LoadGameplayScene();
        if (isExiting) yield break;

        yield return CreatePlayer();
        if (isExiting) yield break;

        ActivateGameplaySystems();
        EnableGameplayInput();
    }

    private IEnumerator SetupGameplayUI()
    {
        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem == null)
        {
            Debug.LogError("UISystem not found! Cannot setup gameplay UI.");
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
            Debug.LogWarning("AddressableManager not found. Skipping scene load.");
            yield break;
        }

        var sceneHandle = addressableManager.LoadSceneAsync("GameplayScene");
        if (sceneHandle.IsValid())
        {
            yield return new WaitUntil(() => sceneHandle.IsValid() && sceneHandle.IsDone);
        }
        else
        {
            Debug.LogWarning("Failed to start scene loading. Continuing without scene change.");
        }
    }

    private IEnumerator CreatePlayer()
    {
        var addressableManager = ServiceLocator.Get<AddressableManager>();
        Vector3 spawnPosition = GetPlayerSpawnPosition();

        if (addressableManager == null)
        {
            Debug.LogWarning("AddressableManager not found! Creating fallback player.");
            CreateFallbackPlayer(spawnPosition);
            yield break;
        }

        CreateFallbackPlayer(spawnPosition);

        if (playerInstance != null)
        {
            SetupPlayerComponents();
            RegisterPlayer();
            Debug.Log("✓ Player created and setup completed");
        }
        else
        {
            Debug.LogError("✗ Failed to create player!");
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

    private void CreateFallbackPlayer(Vector3 spawnPosition)
    {
        var fallbackPlayer = new GameObject("FallbackPlayer");
        fallbackPlayer.transform.position = spawnPosition;

        var renderer = fallbackPlayer.AddComponent<SpriteRenderer>();

        var texture = new Texture2D(64, 64);
        var colors = new Color[64 * 64];
        for (int i = 0; i < colors.Length; i++)
        {
            float x = (i % 64) - 32f;
            float y = (i / 64) - 32f;
            float distance = Mathf.Sqrt(x * x + y * y);
            colors[i] = distance < 30f ? Color.blue : Color.clear;
        }
        texture.SetPixels(colors);
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        renderer.sprite = sprite;
        renderer.sortingOrder = 10;

        var rb2d = fallbackPlayer.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;
        rb2d.linearDamping = 5f;
        rb2d.freezeRotation = true;
        rb2d.mass = 1f;

        var triggerCollider = fallbackPlayer.AddComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = 0.6f;

        var physicsCollider = fallbackPlayer.AddComponent<CircleCollider2D>();
        physicsCollider.isTrigger = false;
        physicsCollider.radius = 0.4f;

        var playerHealth = fallbackPlayer.AddComponent<PlayerHealth>();
        var playerCombat = fallbackPlayer.AddComponent<PlayerCombat>();
        var playerMovement = fallbackPlayer.AddComponent<PlayerMovement>();

        try
        {
            fallbackPlayer.tag = "Player";
        }
        catch (UnityException)
        {
            Debug.LogWarning("Player tag not found, using Untagged");
        }

        Object.DontDestroyOnLoad(fallbackPlayer);

        var destroyWatcher = fallbackPlayer.AddComponent<PlayerDestroyWatcher>();

        playerInstance = fallbackPlayer;
    }

    private void SetupPlayerComponents()
    {
        if (playerInstance == null)
        {
            Debug.LogError("SetupPlayerComponents called with null playerInstance!");
            return;
        }

        Object.DontDestroyOnLoad(playerInstance);

        if (playerInstance.transform.position.y > 100f || playerInstance.transform.position.y < -100f)
        {
            Vector3 correctedPosition = GetPlayerSpawnPosition();
            playerInstance.transform.position = correctedPosition;
        }

        var rb3d = playerInstance.GetComponent<Rigidbody>();
        if (rb3d != null)
        {
            Debug.LogWarning("Removing 3D Rigidbody from player");
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

        if (playerInstance.GetComponent<PlayerDestroyWatcher>() == null)
        {
            playerInstance.AddComponent<PlayerDestroyWatcher>();
        }

        var playerCombat = playerInstance.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            CoroutineRunner.StartRoutine(SafeInitializeComponent(playerCombat, "PlayerCombat"));
        }
        else
        {
            Debug.LogWarning("PlayerCombat component not found!");
        }

        var playerMovement = playerInstance.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            CoroutineRunner.StartRoutine(SafeInitializeComponent(playerMovement, "PlayerMovement"));
        }
        else
        {
            Debug.LogWarning("PlayerMovement component not found!");
        }

        var playerHealth = playerInstance.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDied += OnPlayerDied;
        }
        else
        {
            Debug.LogWarning("PlayerHealth component not found!");
        }
    }

    private IEnumerator SafeInitializeComponent(IInitializable component, string componentName)
    {
        if (component == null)
        {
            Debug.LogError($"{componentName} is null, cannot initialize");
            yield break;
        }

        yield return component.Initialize();
    }

    private void RegisterPlayer()
    {
        if (playerInstance != null)
        {
            ServiceLocator.Register<GameObject>(playerInstance);
        }
    }

    private void ActivateGameplaySystems()
    {
        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StartSpawning();
        }
        else
        {
            Debug.LogWarning("SpawnSystem not found");
        }
    }

    private void EnableGameplayInput()
    {
        Debug.Log("Enabling gameplay input...");

        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            inputReader.EnableGameplayInput();
            Debug.Log("✓ Gameplay input enabled");
        }
        else
        {
            Debug.LogWarning("InputReader not found");
        }
    }

    private void OnPlayerDied()
    {
        if (isExiting) return;

        Debug.Log("Player died! Ending gameplay.");

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
            Debug.Log($"Transitioning to GameOver with score: {finalScore}");
            stateMachine.ChangeState(new GameOverState(finalScore));
        }
        else
        {
            Debug.LogError("GameStateMachine not found! Cannot transition to GameOver.");
        }
    }

    public override void Update()
    {
        if (!isRoundActive || isExiting) return;

        try
        {
            if (playerInstance == null || playerInstance.gameObject == null)
            {
                Debug.LogError("Player instance is null or destroyed during gameplay!");
                AttemptPlayerRecovery();
                return;
            }

            if (playerInstance.name == null)
            {
                Debug.LogError("Player object is destroyed but reference still exists!");
                playerInstance = null;
                AttemptPlayerRecovery();
                return;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception checking player instance: {e.Message}");
            playerInstance = null;
            AttemptPlayerRecovery();
            return;
        }

        if (gameplayUIController != null)
        {
            gameplayUIController.UpdateTimer(Time.time);
        }
    }

    private void AttemptPlayerRecovery()
    {
        Debug.LogWarning("🔄 Attempting player recovery...");

        var foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer != null)
        {
            Debug.Log($"✓ Found player by tag: {foundPlayer.name} (ID: {foundPlayer.GetInstanceID()})");
            playerInstance = foundPlayer;
            RegisterPlayer();
            return;
        }

        var playerHealthComponents = Object.FindObjectsOfType<PlayerHealth>();
        if (playerHealthComponents.Length > 0)
        {
            playerInstance = playerHealthComponents[0].gameObject;
            Debug.Log($"✓ Found player by PlayerHealth component: {playerInstance.name}");
            RegisterPlayer();
            return;
        }

        var allObjects = Object.FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("Player") || obj.name.Contains("Fallback"))
            {
                playerInstance = obj;
                Debug.Log($"✓ Found player by name search: {playerInstance.name}");
                RegisterPlayer();
                return;
            }
        }

        Debug.LogError("❌ Player recovery failed - ending gameplay");
        OnPlayerDied();
    }

    public override IEnumerator Exit()
    {
        Debug.Log("=== EXITING GAMEPLAY STATE ===");
        isExiting = true;

        if (ServiceLocator.TryGet<SpawnSystem>(out var spawnSystem))
        {
            spawnSystem.StopSpawning();
            Debug.Log("Spawn system stopped");
        }

        var uiSystem = ServiceLocator.Get<UISystem>();
        if (uiSystem != null)
        {
            if (uiSystem.IsUIActive(GAMEPLAY_UI_ID))
            {
                uiSystem.HideUI(GAMEPLAY_UI_ID);
            }
            uiSystem.UnregisterUIController(GAMEPLAY_UI_ID);
            Debug.Log("Gameplay UI cleaned up");
        }

        if (playerInstance != null)
        {
            Debug.Log($"Cleaning up player: {playerInstance.name} (ID: {playerInstance.GetInstanceID()})");

            var playerHealth = playerInstance.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnPlayerDied -= OnPlayerDied;
                Debug.Log("Unsubscribed from PlayerHealth events");
            }

            var playerCombat = playerInstance.GetComponent<PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.Cleanup();
                Debug.Log("PlayerCombat cleaned up");
            }

            var playerMovement = playerInstance.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.Cleanup();
                Debug.Log("PlayerMovement cleaned up");
            }

            bool shouldDestroyPlayer = ShouldDestroyPlayerOnExit();

            if (shouldDestroyPlayer)
            {
                Debug.Log("Destroying player - full game exit");
                Object.Destroy(playerInstance);
                playerInstance = null;
            }
            else
            {
                Debug.Log("Keeping player alive - temporary state change");
                playerInstance.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("playerInstance was already null during Exit");
        }

        gameplayUIController = null;
        isRoundActive = false;

        Debug.Log("=== GAMEPLAY STATE EXITED ===");
        yield return null;
    }

    private bool ShouldDestroyPlayerOnExit()
    {
        return true;
    }
}