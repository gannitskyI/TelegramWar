using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "Game/Input Reader")]
public class InputReader : ScriptableObject, PlayerControls.IGameplayActions
{
    private PlayerControls controls;
    private Camera mainCamera;

    public event Action<Vector2> MoveEvent;
    public event Action MoveCancelEvent;

    public Vector2 MoveInput { get; private set; }

    public void EnableGameplayInput()
    {
        if (controls != null) controls.Gameplay.Enable();
    }

    public void DisableAllInput()
    {
        if (controls != null) controls.Gameplay.Disable();
    }

    public void Initialize()
    {
        if (controls == null)
        {
            controls = new PlayerControls();
            controls.Gameplay.SetCallbacks(this);
        }
        mainCamera = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
        EnableGameplayInput();
    }


    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            Vector2 screenPosition = context.ReadValue<Vector2>();
            Vector2 worldPosition = ScreenToWorldPoint(screenPosition);
            MoveInput = worldPosition;
            MoveEvent?.Invoke(MoveInput);
        }
    }

    public void OnTouch(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            var touchControl = context.control as UnityEngine.InputSystem.Controls.TouchControl;
            if (touchControl != null)
            {
                Vector2 touchPosition = touchControl.position.ReadValue();
                Vector2 worldPosition = ScreenToWorldPoint(touchPosition);
                MoveInput = worldPosition;
                MoveEvent?.Invoke(MoveInput);
            }
        }
    }

    private Vector2 ScreenToWorldPoint(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            if (mainCamera == null) return screenPosition;
        }
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));
        return new Vector2(worldPos.x, worldPos.y);
    }

    private void OnDestroy()
    {
        DisableAllInput();
        controls?.Dispose();
    }
}
