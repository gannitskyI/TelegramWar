using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour, IInitializable
{
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private InputReader inputReader;

    private Vector3 targetPosition;
    private Vector3 velocity;
    private bool isMoving;
    private Rigidbody2D rb2d;
    private bool isInitialized = false;
    private PlayerStats playerStats;
    private float currentMoveSpeed;
    private Camera mainCamera;

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
        }
        if (inputReader == null)
        {
            yield return TryGetInputReader();
            if (inputReader == null)
            {
                isInitialized = false;
                yield break;
            }
        }
        inputReader.MoveEvent += HandleMoveInput;
        inputReader.MoveCancelEvent += HandleMoveCancel;
        targetPosition = transform.position;
        mainCamera = Camera.main;
        isInitialized = true;
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
    }

    private void HandleMoveInput(Vector2 worldPosition)
    {
        if (!isInitialized) return;
        targetPosition = ClampToScreen(new Vector3(worldPosition.x, worldPosition.y, transform.position.z));
        isMoving = true;
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
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return position;
        }
        Vector3 bottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 topRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.nearClipPlane));
        float margin = 0.5f;
        float clampedX = Mathf.Clamp(position.x, bottomLeft.x + margin, topRight.x - margin);
        float clampedY = Mathf.Clamp(position.y, bottomLeft.y + margin, topRight.y - margin);
        return new Vector3(clampedX, clampedY, position.z);
    }

    void FixedUpdate()
    {
        if (!isInitialized || rb2d == null || !isMoving) return;
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance > 0.1f)
        {
            Vector3 newPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime,
                currentMoveSpeed,
                Time.fixedDeltaTime
            );
            rb2d.MovePosition(newPosition);
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
