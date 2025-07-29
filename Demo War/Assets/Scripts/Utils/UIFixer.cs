using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Автоматическое исправление критических UI проблем
/// </summary>
public class UIFixer : MonoBehaviour
{
    [Header("Auto Fix Settings")]
    [SerializeField] private bool fixOnStart = true;
    [SerializeField] private bool createMissingComponents = true;

    void Start()
    {
        if (fixOnStart)
        {
            Invoke(nameof(FixAllUIIssues), 0.5f);
        }
    }

    [ContextMenu("Fix All UI Issues")]
    public void FixAllUIIssues()
    {
        Debug.Log("🔧 Starting UI fixes...");

        // 1. Исправляем EventSystem
        FixEventSystem();

        // 2. Исправляем GraphicRaycasters
        FixGraphicRaycasters();

        // 3. Проверяем кнопки
        FixButtons();

        Debug.Log("✅ UI fix completed!");

        // Проверяем результат
        Invoke(nameof(VerifyFixes), 1f);
    }

    private void FixEventSystem()
    {
        Debug.Log("🔧 Fixing EventSystem...");

        var eventSystem = FindObjectOfType<EventSystem>();

        if (eventSystem == null)
        {
            Debug.Log("Creating new EventSystem...");
            var eventSystemGO = new GameObject("EventSystem");
            eventSystem = eventSystemGO.AddComponent<EventSystem>();
            DontDestroyOnLoad(eventSystemGO);
        }

        // Проверяем InputModule
        var inputModule = eventSystem.GetComponent<BaseInputModule>();
        if (inputModule == null)
        {
            Debug.Log("Adding InputSystemUIInputModule...");

            // Удаляем старые модули если есть
            var oldModules = eventSystem.GetComponents<BaseInputModule>();
            foreach (var oldModule in oldModules)
            {
                if (Application.isPlaying)
                    Destroy(oldModule);
                else
                    DestroyImmediate(oldModule);
            }

            // Добавляем новый Input System UI Module
            try
            {
                var newInputModule = eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("✅ InputSystemUIInputModule added successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to add InputSystemUIInputModule: {e.Message}");
                Debug.Log("Trying fallback StandaloneInputModule...");

                // Fallback к старому модулю
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
        }
        else
        {
            Debug.Log($"✅ InputModule already exists: {inputModule.GetType().Name}");
        }
    }

    private void FixGraphicRaycasters()
    {
        Debug.Log("🔧 Fixing GraphicRaycasters...");

        var canvases = FindObjectsOfType<Canvas>();
        int addedRaycasters = 0;

        foreach (var canvas in canvases)
        {
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                addedRaycasters++;
                Debug.Log($"✅ Added GraphicRaycaster to: {canvas.name}");
            }
            else
            {
                // Убеждаемся что раycaster включен
                if (!raycaster.enabled)
                {
                    raycaster.enabled = true;
                    Debug.Log($"✅ Enabled GraphicRaycaster on: {canvas.name}");
                }
            }
        }

        if (addedRaycasters > 0)
        {
            Debug.Log($"✅ Added {addedRaycasters} GraphicRaycasters");
        }
        else
        {
            Debug.Log("ℹ️ All canvases already have GraphicRaycasters");
        }
    }

    private void FixButtons()
    {
        Debug.Log("🔧 Checking buttons...");

        var buttons = FindObjectsOfType<Button>(true);
        int fixedButtons = 0;

        foreach (var button in buttons)
        {
            bool wasFixed = false;

            // Включаем кнопку
            if (!button.interactable)
            {
                button.interactable = true;
                wasFixed = true;
            }

            // Включаем raycast target
            if (button.targetGraphic != null && !button.targetGraphic.raycastTarget)
            {
                button.targetGraphic.raycastTarget = true;
                wasFixed = true;
            }

            // Активируем GameObject если нужно
            if (!button.gameObject.activeInHierarchy && button.transform.parent != null)
            {
                // Не активируем корневые объекты, только дочерние
                if (button.transform.parent.gameObject.activeInHierarchy)
                {
                    button.gameObject.SetActive(true);
                    wasFixed = true;
                }
            }

            if (wasFixed)
            {
                fixedButtons++;
                Debug.Log($"✅ Fixed button: {button.name}");
            }
        }

        Debug.Log($"ℹ️ Fixed {fixedButtons} buttons, total buttons: {buttons.Length}");
    }

    private void VerifyFixes()
    {
        Debug.Log("🔍 Verifying fixes...");

        // Проверяем EventSystem
        var eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            var inputModule = eventSystem.GetComponent<BaseInputModule>();
            if (inputModule != null)
            {
                Debug.Log($"✅ EventSystem OK: {inputModule.GetType().Name}");
            }
            else
            {
                Debug.LogError("❌ EventSystem still has no InputModule!");
            }
        }
        else
        {
            Debug.LogError("❌ EventSystem still missing!");
        }

        // Проверяем GraphicRaycasters
        var raycasters = FindObjectsOfType<GraphicRaycaster>();
        if (raycasters.Length > 0)
        {
            Debug.Log($"✅ GraphicRaycasters OK: {raycasters.Length} found");
        }
        else
        {
            Debug.LogError("❌ Still no GraphicRaycasters!");
        }

        // Проверяем кнопки
        var buttons = FindObjectsOfType<Button>();
        int workingButtons = 0;
        foreach (var button in buttons)
        {
            if (button.interactable && button.gameObject.activeInHierarchy)
            {
                workingButtons++;
            }
        }

        Debug.Log($"✅ Working buttons: {workingButtons}/{buttons.Length}");

        if (workingButtons > 0)
        {
            Debug.Log("🎉 UI should now be interactive! Try clicking buttons.");
        }
    }

    [ContextMenu("Test Start Button")]
    public void TestStartButton()
    {
        var startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        if (startButton != null)
        {
            Debug.Log("🧪 Testing StartButton click...");
            startButton.onClick.Invoke();
        }
        else
        {
            Debug.LogError("StartButton not found!");

            var allButtons = FindObjectsOfType<Button>();
            Debug.Log($"Available buttons: {string.Join(", ", System.Array.ConvertAll(allButtons, b => b.name))}");
        }
    }

    [ContextMenu("Create Missing EventSystem")]
    public void CreateMissingEventSystem()
    {
        var existing = FindObjectOfType<EventSystem>();
        if (existing != null)
        {
            Debug.Log("EventSystem already exists, destroying old one...");
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        var eventSystemGO = new GameObject("EventSystem");
        var eventSystem = eventSystemGO.AddComponent<EventSystem>();

        try
        {
            eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("✅ Created EventSystem with InputSystemUIInputModule");
        }
        catch
        {
            eventSystemGO.AddComponent<StandaloneInputModule>();
            Debug.Log("✅ Created EventSystem with StandaloneInputModule (fallback)");
        }

        DontDestroyOnLoad(eventSystemGO);
    }
}