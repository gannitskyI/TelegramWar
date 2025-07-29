using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // Добавляем новый Input System

/// <summary>
/// Помощник для диагностики проблем с UI интерактивностью
/// Добавьте этот компонент на любой GameObject для отладки UI
/// </summary>
public class UIDebugHelper : MonoBehaviour
{
    [Header("Debug UI Settings")]
    [SerializeField] private bool logOnStart = true;
    [SerializeField] private bool showGUI = true;

    void Start()
    {
        if (logOnStart)
        {
            DiagnoseUIIssues();
        }
    }

    void Update()
    {
        // Используем новый Input System
        if (Keyboard.current != null && Keyboard.current.f4Key.wasPressedThisFrame)
        {
            DiagnoseUIIssues();
        }

        // Альтернативно можно использовать Mouse для диагностики
        if (Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame)
        {
            CheckMouseRaycast();
        }
    }

    [ContextMenu("Diagnose UI Issues")]
    public void DiagnoseUIIssues()
    {
        Debug.Log("=== UI DIAGNOSTIC REPORT ===");

        // 1. Проверяем EventSystem
        CheckEventSystem();

        // 2. Проверяем GraphicRaycaster
        CheckGraphicRaycasters();

        // 3. Проверяем Canvas настройки
        CheckCanvasSettings();

        // 4. Проверяем кнопки
        CheckButtons();

        // 5. Проверяем блокирующие элементы
        CheckBlockingElements();

        // 6. Проверяем Input System
        CheckInputSystem();

        Debug.Log("===========================");
    }

    private void CheckEventSystem()
    {
        var eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("? EventSystem not found! UI will not be interactive.");
            Debug.LogError("?? Fix: Add EventSystem to scene (GameObject ? UI ? Event System)");
        }
        else
        {
            Debug.Log($"? EventSystem found: {eventSystem.name}");

            if (!eventSystem.enabled)
            {
                Debug.LogError("? EventSystem is disabled!");
            }

            // Проверяем InputModule
            var inputModule = eventSystem.currentInputModule;
            if (inputModule == null)
            {
                Debug.LogError("? No InputModule found in EventSystem!");
            }
            else
            {
                Debug.Log($"? InputModule: {inputModule.GetType().Name}");
            }
        }
    }

    private void CheckInputSystem()
    {
        Debug.Log("=== INPUT SYSTEM CHECK ===");

        // Проверяем что Input System активен
        var inputSystemPackage = "com.unity.inputsystem";
        Debug.Log($"Using new Input System package");

        // Проверяем наличие InputReader
        if (ServiceLocator.TryGet<InputReader>(out var inputReader))
        {
            Debug.Log("? InputReader found in ServiceLocator");
        }
        else
        {
            Debug.LogWarning("?? InputReader not found in ServiceLocator");
        }

        // Проверяем мышь и клавиатуру
        if (Mouse.current != null)
        {
            Debug.Log("? Mouse detected");
            Debug.Log($"Mouse position: {Mouse.current.position.ReadValue()}");
        }
        else
        {
            Debug.LogWarning("?? Mouse not detected");
        }

        if (Keyboard.current != null)
        {
            Debug.Log("? Keyboard detected");
        }
        else
        {
            Debug.LogWarning("?? Keyboard not detected");
        }
    }

    private void CheckGraphicRaycasters()
    {
        var raycasters = FindObjectsOfType<GraphicRaycaster>();
        if (raycasters.Length == 0)
        {
            Debug.LogError("? No GraphicRaycasters found! UI clicks won't work.");
        }
        else
        {
            Debug.Log($"? Found {raycasters.Length} GraphicRaycaster(s)");

            foreach (var raycaster in raycasters)
            {
                if (!raycaster.enabled)
                {
                    Debug.LogWarning($"?? GraphicRaycaster disabled on: {raycaster.name}");
                }
                else
                {
                    Debug.Log($"  - {raycaster.name}: enabled");
                }
            }
        }
    }

    private void CheckCanvasSettings()
    {
        var canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"Found {canvases.Length} Canvas(es):");

        foreach (var canvas in canvases)
        {
            Debug.Log($"Canvas: {canvas.name}");
            Debug.Log($"  - Enabled: {canvas.enabled}");
            Debug.Log($"  - Render Mode: {canvas.renderMode}");
            Debug.Log($"  - Sort Order: {canvas.sortingOrder}");
            Debug.Log($"  - Override Sorting: {canvas.overrideSorting}");

            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                Debug.LogWarning("?? WorldSpace Canvas detected - make sure it has proper collision setup");
            }
        }
    }

    private void CheckButtons()
    {
        var buttons = FindObjectsOfType<Button>(true); // includeInactive = true
        Debug.Log($"Found {buttons.Length} Button(s):");

        foreach (var button in buttons)
        {
            Debug.Log($"Button: {button.name}");
            Debug.Log($"  - GameObject Active: {button.gameObject.activeInHierarchy}");
            Debug.Log($"  - Component Enabled: {button.enabled}");
            Debug.Log($"  - Interactable: {button.interactable}");
            Debug.Log($"  - Raycast Target: {button.targetGraphic?.raycastTarget}");

            // Проверяем блокирующие родительские объекты
            var current = button.transform.parent;
            while (current != null)
            {
                var canvasGroup = current.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    if (!canvasGroup.interactable)
                    {
                        Debug.LogWarning($"?? CanvasGroup blocks interaction: {current.name}");
                    }
                    if (!canvasGroup.blocksRaycasts)
                    {
                        Debug.LogWarning($"?? CanvasGroup blocks raycasts: {current.name}");
                    }
                }
                current = current.parent;
            }
        }
    }

    private void CheckBlockingElements()
    {
        // Проверяем CanvasGroup компоненты
        var canvasGroups = FindObjectsOfType<CanvasGroup>();
        foreach (var group in canvasGroups)
        {
            if (!group.interactable || !group.blocksRaycasts)
            {
                Debug.LogWarning($"?? CanvasGroup may block UI: {group.name}");
                Debug.LogWarning($"  - Interactable: {group.interactable}");
                Debug.LogWarning($"  - Blocks Raycasts: {group.blocksRaycasts}");
            }
        }

        // Проверяем перекрывающие элементы с высоким Sort Order
        var canvases = FindObjectsOfType<Canvas>();
        System.Array.Sort(canvases, (a, b) => b.sortingOrder.CompareTo(a.sortingOrder));

        Debug.Log("Canvas Sort Order (highest first):");
        foreach (var canvas in canvases)
        {
            Debug.Log($"  {canvas.name}: {canvas.sortingOrder}");
        }
    }

    void OnGUI()
    {
        if (!showGUI || !Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 250, 350, 250));
        GUILayout.Label("UI Debug Helper (New Input System)");

        if (GUILayout.Button("Diagnose UI Issues (F4)"))
        {
            DiagnoseUIIssues();
        }

        if (GUILayout.Button("Check Mouse Position (Middle Click)"))
        {
            CheckMouseRaycast();
        }

        if (GUILayout.Button("List All UI Elements"))
        {
            ListAllUIElements();
        }

        if (GUILayout.Button("Test Button Clicks"))
        {
            TestButtonClicks();
        }

        GUILayout.Label("Press F4 for full diagnosis");
        GUILayout.Label("Middle click to check mouse raycast");

        GUILayout.EndArea();
    }

    [ContextMenu("Check Mouse Raycast")]
    public void CheckMouseRaycast()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("No EventSystem found!");
            return;
        }

        Vector2 mousePosition = Vector2.zero;

        // Получаем позицию мыши через новый Input System
        if (Mouse.current != null)
        {
            mousePosition = Mouse.current.position.ReadValue();
        }
        else
        {
            Debug.LogError("Mouse not available!");
            return;
        }

        var pointerEventData = new PointerEventData(eventSystem)
        {
            position = mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        eventSystem.RaycastAll(pointerEventData, results);

        Debug.Log($"Mouse position: {mousePosition}");
        Debug.Log($"Raycast hits: {results.Count}");

        foreach (var result in results)
        {
            Debug.Log($"  - {result.gameObject.name} (depth: {result.depth})");
        }
    }

    [ContextMenu("List All UI Elements")]
    public void ListAllUIElements()
    {
        var selectables = FindObjectsOfType<Selectable>(true);
        Debug.Log($"All UI Selectables ({selectables.Length}):");

        foreach (var selectable in selectables)
        {
            var status = selectable.gameObject.activeInHierarchy ? "Active" : "Inactive";
            var interactable = selectable.interactable ? "Interactable" : "Non-interactable";
            Debug.Log($"  {selectable.name} ({selectable.GetType().Name}): {status}, {interactable}");
        }
    }

    [ContextMenu("Test Button Clicks")]
    public void TestButtonClicks()
    {
        var buttons = FindObjectsOfType<Button>();
        Debug.Log($"Testing {buttons.Length} buttons:");

        foreach (var button in buttons)
        {
            if (button.interactable && button.gameObject.activeInHierarchy)
            {
                Debug.Log($"Simulating click on: {button.name}");
                button.onClick.Invoke();
            }
            else
            {
                Debug.LogWarning($"Cannot click {button.name}: interactable={button.interactable}, active={button.gameObject.activeInHierarchy}");
            }
        }
    }
}