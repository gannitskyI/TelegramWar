using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "Game/Input Reader")]
public class InputReader : ScriptableObject, PlayerControls.IGameplayActions
{
    private PlayerControls controls;

    // События для движения
    public event Action<Vector2> MoveEvent;
    public event Action MoveCancelEvent;

    public Vector2 MoveInput { get; private set; }

    public void Initialize()
    {
        if (controls == null)
        {
            controls = new PlayerControls();
            controls.Gameplay.SetCallbacks(this);
        }
        EnableGameplayInput();
    }

    public void EnableGameplayInput()
    {
        controls.Gameplay.Enable();
    }

    public void DisableAllInput()
    {
        controls.Gameplay.Disable();
    }

    // Callbacks from Input System - реализация интерфейса
    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            Vector2 screenPosition = context.ReadValue<Vector2>();
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Camera.main.nearClipPlane));
            MoveInput = new Vector2(worldPosition.x, worldPosition.y);
            MoveEvent?.Invoke(MoveInput);
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            MoveCancelEvent?.Invoke();
        }
    }

    public void OnTouch(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            // Для Touch используем другой подход
            Vector2 touchPosition = context.ReadValue<Vector2>();
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, Camera.main.nearClipPlane));
            MoveInput = new Vector2(worldPosition.x, worldPosition.y);
            MoveEvent?.Invoke(MoveInput);
        }
    }
}