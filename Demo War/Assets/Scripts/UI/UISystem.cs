using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISystem : IInitializable, IUpdatable
{
    public int InitializationOrder => 15; // После InputSystem но до других систем

    private Dictionary<string, IUIController> uiControllers;
    private IUIController currentActiveUI;

    public IEnumerator Initialize()
    { 
        uiControllers = new Dictionary<string, IUIController>();
 
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
            
        }
    }

    public T GetUIController<T>(string uiId) where T : class, IUIController
    {
        if (uiControllers.TryGetValue(uiId, out var controller))
        {
            return controller as T;
        }
        return null;
    }

    public void ShowUI(string uiId)
    {
        if (!uiControllers.TryGetValue(uiId, out var controller))
        {
            Debug.LogError($"UI Controller not found: {uiId}");
            return;
        }

        // Скрываем текущий активный UI
        if (currentActiveUI != null && currentActiveUI != controller)
        {
            currentActiveUI.Hide();
        }

        // Показываем новый UI
        controller.Show();
        currentActiveUI = controller;
 
    }

    public void HideUI(string uiId)
    {
        if (!uiControllers.TryGetValue(uiId, out var controller))
        {
            Debug.LogError($"UI Controller not found: {uiId}");
            return;
        }

        controller.Hide();

        if (currentActiveUI == controller)
        {
            currentActiveUI = null;
        }
 
    }

    public void HideAllUI()
    {
        foreach (var controller in uiControllers.Values)
        {
            controller.Hide();
        }
        currentActiveUI = null; 
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
        // Обновляем активный UI контроллер
        currentActiveUI?.Update(deltaTime);
    }

    public void Cleanup()
    {
        // Очищаем все UI контроллеры
        foreach (var controller in uiControllers.Values)
        {
            controller.Cleanup();
        }

        uiControllers.Clear();
        currentActiveUI = null;
 
    }
}