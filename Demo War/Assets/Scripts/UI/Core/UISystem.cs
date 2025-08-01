using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UISystem : IInitializable, IUpdatable
{
    public int InitializationOrder => 15;

    private Dictionary<string, IUIController> uiControllers;
    private IUIController currentActiveUI;

    public IEnumerator Initialize()
    {
        uiControllers = new Dictionary<string, IUIController>();

        // Автоматическая регистрация UI-контроллеров в сцене
        var controllers = Object.FindObjectsOfType<MonoBehaviour>().OfType<IUIController>();
        foreach (var controller in controllers)
        {
            string uiId = controller.GetType().Name; // Или используйте константу, например, "GameOver"
            RegisterUIController(uiId, controller);
            Debug.Log($"Registered UI controller: {uiId}");
        }

        yield return null;
    }

    public void RegisterUIController(string uiId, IUIController controller)
    {
        if (string.IsNullOrEmpty(uiId))
        {
            Debug.LogError("UI ID cannot be null or empty");
            return;
        }

        if (uiControllers.ContainsKey(uiId))
        {
            Debug.LogWarning($"UI Controller {uiId} is already registered, replacing");
        }

        uiControllers[uiId] = controller;
        Debug.Log($"UI Controller registered: {uiId}");
    }

    public void UnregisterUIController(string uiId)
    {
        if (uiControllers.ContainsKey(uiId))
        {
            var controller = uiControllers[uiId];
            if (controller == currentActiveUI)
            {
                currentActiveUI = null;
            }

            uiControllers.Remove(uiId);
            Debug.Log($"UI Controller unregistered: {uiId}");
        }
    }

    public T GetUIController<T>(string uiId) where T : class, IUIController
    {
        if (uiControllers.TryGetValue(uiId, out var controller))
        {
            return controller as T;
        }
        Debug.LogWarning($"UI Controller not found: {uiId}");
        return null;
    }

    public void ShowUI(string uiId)
    {
        if (!uiControllers.TryGetValue(uiId, out var controller))
        {
            Debug.LogError($"UI Controller not found: {uiId}");
            return;
        }

        if (currentActiveUI != null && currentActiveUI != controller)
        {
            currentActiveUI.Hide();
        }

        controller.Show();
        currentActiveUI = controller;
        Debug.Log($"Showing UI: {uiId}");
    }

    public void HideUI(string uiId)
    {
        if (!uiControllers.TryGetValue(uiId, out var controller))
        {
            Debug.LogWarning($"UI Controller not found: {uiId}");
            return;
        }

        controller.Hide();

        if (currentActiveUI == controller)
        {
            currentActiveUI = null;
        }

        Debug.Log($"Hiding UI: {uiId}");
    }

    public void HideAllUI()
    {
        foreach (var controller in uiControllers.Values)
        {
            controller.Hide();
        }
        currentActiveUI = null;
        Debug.Log("All UI hidden");
    }

    public bool IsUIActive(string uiId)
    {
        if (uiControllers.TryGetValue(uiId, out var controller))
        {
            return controller.IsVisible();
        }
        return false;
    }

    public void OnUpdate(float deltaTime)
    {
        currentActiveUI?.Update(deltaTime);
    }

    public void Cleanup()
    {
        foreach (var controller in uiControllers.Values)
        {
            controller.Cleanup();
        }

        uiControllers.Clear();
        currentActiveUI = null;
        Debug.Log("UISystem cleaned up");
    }
}