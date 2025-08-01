using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour, IInitializable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private InputReader inputReader;

    private Vector3 targetPosition;
    private Vector3 velocity;
    private bool isMoving;
    private Rigidbody2D rb2d;
    private bool isInitialized = false;

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

        Debug.Log($"PlayerMovement initialized at position: {transform.position}");
        yield return null;
    }

    private IEnumerator TryGetInputReader()
    {
        int attempts = 0;
        while (attempts < 10 && inputReader == null)
        {
            if (ServiceLocator.TryGet<InputReader>(out var locatorInputReader))
            {
                inputReader = locatorInputReader;
                Debug.Log("PlayerMovement: Got InputReader from ServiceLocator");
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
            Debug.LogWarning("PlayerMovement: Received input but not initialized yet");
            return;
        }

        targetPosition = ClampToScreen(new Vector3(worldPosition.x, worldPosition.y, transform.position.z));
        isMoving = true;

        Debug.Log($"PlayerMovement: Move target set to: {targetPosition}");

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
        Debug.Log("PlayerMovement: Movement canceled");
    }

    private Vector3 ClampToScreen(Vector3 position)
    {
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogWarning("PlayerMovement: No main camera found for screen clamping");
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
                moveSpeed
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
            float currentSpeed = Mathf.Min(moveSpeed, distance * 10f);
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

        if (inputReader != null)
        {
            inputReader.MoveEvent -= HandleMoveInput;
            inputReader.MoveCancelEvent -= HandleMoveCancel;
            Debug.Log("PlayerMovement: Input events unsubscribed");
        }

        if (rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;  
        }
    }

    public bool IsMoving() => isMoving;
    public Vector3 GetTargetPosition() => targetPosition;
    public float GetMoveSpeed() => moveSpeed;
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
        Debug.Log($"PlayerMovement: Position set to: {clampedPosition}");
    }

    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0.1f, newSpeed);
    }
}