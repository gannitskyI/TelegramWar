using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour, IInitializable
{
    [Header("Movement Settings")]
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private InputReader inputReader;

    private Vector3 targetPosition;
    private Vector3 velocity;
    private bool isMoving;
    private Rigidbody2D rb2d;
    private bool isInitialized = false;
    private PlayerStats playerStats;
    private float currentMoveSpeed;

    public int InitializationOrder => 10;

    private void OnDestroy()
    {
        Cleanup();
    }

    public IEnumerator Initialize()
    {
        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
        }

        rb2d.gravityScale = 0f;
        rb2d.linearDamping = 8f;
        rb2d.freezeRotation = true;

        if (ServiceLocator.TryGet<PlayerStats>(out playerStats))
        {
            currentMoveSpeed = playerStats.FinalMoveSpeed;
            playerStats.OnStatsChanged += OnStatsChanged;
        }
        else
        {
            currentMoveSpeed = 5f;
            Debug.LogWarning("PlayerMovement: PlayerStats not found, using default speed");
        }

        if (inputReader == null)
        {
            yield return TryGetInputReader();

            if (inputReader == null)
            {
                Debug.LogError("PlayerMovement: Failed to get InputReader!");
                isInitialized = false;
                yield break;
            }
        }

        inputReader.MoveEvent += HandleMoveInput;
        inputReader.MoveCancelEvent += HandleMoveCancel;

        targetPosition = transform.position;
        isInitialized = true;

        Debug.Log($"PlayerMovement initialized with speed: {currentMoveSpeed:F1}");
        yield return null;
    }

    private void OnStatsChanged(PlayerStats stats)
    {
        currentMoveSpeed = stats.FinalMoveSpeed;
    }

    private IEnumerator TryGetInputReader()
    {
        int attempts = 0;
        while (attempts < 10 && inputReader == null)
        {
            if (ServiceLocator.TryGet<InputReader>(out var locatorInputReader))
            {
                inputReader = locatorInputReader;
                break;
            }

            attempts++;
            yield return new WaitForSeconds(0.1f);
        }

        if (inputReader == null)
        {
            Debug.LogError("PlayerMovement: Could not find InputReader after 10 attempts!");
        }
    }

    private void HandleMoveInput(Vector2 worldPosition)
    {
        if (!isInitialized)
        {
            return;
        }

        targetPosition = ClampToScreen(new Vector3(worldPosition.x, worldPosition.y, transform.position.z));
        isMoving = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLHelper.TriggerHapticFeedback("light");
#endif
    }

    private void HandleMoveCancel()
    {
        if (!isInitialized) return;

        isMoving = false;
        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
        }
    }

    private Vector3 ClampToScreen(Vector3 position)
    {
        var camera = Camera.main;
        if (camera == null)
        {
            return position;
        }

        Vector3 bottomLeft = camera.ScreenToWorldPoint(new Vector3(0, 0, camera.nearClipPlane));
        Vector3 topRight = camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, camera.nearClipPlane));

        float margin = 0.5f;

        float clampedX = Mathf.Clamp(position.x, bottomLeft.x + margin, topRight.x - margin);
        float clampedY = Mathf.Clamp(position.y, bottomLeft.y + margin, topRight.y - margin);

        return new Vector3(clampedX, clampedY, position.z);
    }

    void Update()
    {
        if (!isInitialized || rb2d == null) return;

        if (isMoving)
        {
            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance < 0.1f)
            {
                isMoving = false;
                rb2d.linearVelocity = Vector2.zero;
                return;
            }

            Vector3 newPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime,
                currentMoveSpeed
            );

            rb2d.MovePosition(newPosition);
        }
    }

    void FixedUpdate()
    {
        if (!isInitialized || rb2d == null || !isMoving) return;

        Vector2 direction = (targetPosition - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, targetPosition);

        if (distance > 0.1f)
        {
            float currentSpeed = Mathf.Min(currentMoveSpeed, distance * 10f);
            rb2d.linearVelocity = direction * currentSpeed;
        }
        else
        {
            isMoving = false;
            rb2d.linearVelocity = Vector2.zero;
        }
    }

    public void Cleanup()
    {
        isInitialized = false;
        isMoving = false;

        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= OnStatsChanged;
        }

        if (inputReader != null)
        {
            inputReader.MoveEvent -= HandleMoveInput;
            inputReader.MoveCancelEvent -= HandleMoveCancel;
        }

        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
        }
    }

    public bool IsMoving() => isMoving;
    public Vector3 GetTargetPosition() => targetPosition;
    public float GetMoveSpeed() => currentMoveSpeed;
    public bool IsInitialized() => isInitialized;

    public void SetPosition(Vector3 position)
    {
        Vector3 clampedPosition = ClampToScreen(position);
        transform.position = clampedPosition;
        targetPosition = clampedPosition;

        if (rb2d != null)
        {
            rb2d.position = clampedPosition;
            rb2d.linearVelocity = Vector2.zero;
        }

        isMoving = false;
    }

    public void SetMoveSpeed(float newSpeed)
    {
        currentMoveSpeed = Mathf.Max(0.1f, newSpeed);
    }
}