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
    private Rigidbody2D rb2d; // Используем 2D физику

    public int InitializationOrder => 10;

    public IEnumerator Initialize()
    {
        // Получаем или создаем Rigidbody2D
        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
        }

        // Настраиваем физику
        rb2d.gravityScale = 0f; // Отключаем гравитацию
        rb2d.linearDamping = 5f; // Демпфирование
        rb2d.freezeRotation = true; // Запрещаем вращение

        // Получаем InputReader из ServiceLocator если не назначен
        if (inputReader == null)
        {
            if (ServiceLocator.TryGet<InputReader>(out var locatorInputReader))
            {
                inputReader = locatorInputReader;
                Debug.Log("InputReader obtained from ServiceLocator");
            }
            else
            {
                Debug.LogError("InputReader not found in ServiceLocator!");
                yield break;
            }
        }

        // Подписываемся на события инпута
        inputReader.MoveEvent += HandleMoveInput;
        inputReader.MoveCancelEvent += HandleMoveCancel;

        targetPosition = transform.position;

        Debug.Log($"PlayerMovement initialized at position: {transform.position}");
        yield return null;
    }

    private void HandleMoveInput(Vector2 worldPosition)
    {
        // Ограничиваем движение в пределах экрана
        Vector3 clampedPosition = ClampToScreen(new Vector3(worldPosition.x, worldPosition.y, transform.position.z));

        targetPosition = clampedPosition;
        isMoving = true;

        Debug.Log($"Move input: {worldPosition} -> clamped: {clampedPosition}");

        // Haptic feedback для мобильных
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLHelper.TriggerHapticFeedback("light");
#endif
    }

    private void HandleMoveCancel()
    {
        isMoving = false;
        Debug.Log("Move cancelled");
    }

    private Vector3 ClampToScreen(Vector3 position)
    {
        var camera = Camera.main;
        if (camera == null) return position;

        // Получаем границы экрана в мировых координатах
        Vector3 bottomLeft = camera.ScreenToWorldPoint(new Vector3(0, 0, camera.nearClipPlane));
        Vector3 topRight = camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, camera.nearClipPlane));

        // Добавляем отступ чтобы игрок не выходил за границы
        float margin = 0.5f;

        float clampedX = Mathf.Clamp(position.x, bottomLeft.x + margin, topRight.x - margin);
        float clampedY = Mathf.Clamp(position.y, bottomLeft.y + margin, topRight.y - margin);

        return new Vector3(clampedX, clampedY, position.z);
    }

    void Update()
    {
        if (isMoving && rb2d != null)
        {
            // Используем SmoothDamp для плавного движения
            Vector3 newPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime,
                moveSpeed
            );

            // Применяем новую позицию через Rigidbody2D
            rb2d.MovePosition(newPosition);

            // Останавливаемся если достигли цели
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isMoving = false;
                rb2d.linearVelocity = Vector2.zero; // Останавливаем движение
            }
        }
    }

    void FixedUpdate()
    {
        // Альтернативный способ движения через FixedUpdate для более стабильной физики
        if (isMoving && rb2d != null)
        {
            Vector2 direction = (targetPosition - transform.position).normalized;
            float distance = Vector2.Distance(transform.position, targetPosition);

            if (distance > 0.1f)
            {
                // Применяем силу для движения к цели
                Vector2 force = direction * moveSpeed * rb2d.mass;
                rb2d.AddForce(force, ForceMode2D.Force);

                // Ограничиваем максимальную скорость
                if (rb2d.linearVelocity.magnitude > moveSpeed)
                {
                    rb2d.linearVelocity = rb2d.linearVelocity.normalized * moveSpeed;
                }
            }
            else
            {
                // Достигли цели
                isMoving = false;
                rb2d.linearVelocity = Vector2.zero;
            }
        }
    }

    public void Cleanup()
    {
        if (inputReader != null)
        {
            inputReader.MoveEvent -= HandleMoveInput;
            inputReader.MoveCancelEvent -= HandleMoveCancel;
        }

        Debug.Log("PlayerMovement cleaned up");
    }

    // Публичные методы для получения информации о движении
    public bool IsMoving() => isMoving;
    public Vector3 GetTargetPosition() => targetPosition;
    public float GetMoveSpeed() => moveSpeed;

    // Метод для принудительной установки позиции (для телепортации/спавна)
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
        Debug.Log($"Player position set to: {clampedPosition}");
    }

    // Метод для обновления скорости движения (для улучшений)
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0.1f, newSpeed);
        Debug.Log($"Move speed updated to: {moveSpeed}");
    }
}