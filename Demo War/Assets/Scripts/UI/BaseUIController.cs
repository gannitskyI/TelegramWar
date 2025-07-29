using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public abstract class BaseUIController : IUIController, IButtonUI, ITextUI
{
    protected GameObject uiGameObject;
    protected readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
    protected readonly Dictionary<string, Text> texts = new Dictionary<string, Text>();
    protected readonly Dictionary<string, TextMeshProUGUI> tmpTexts = new Dictionary<string, TextMeshProUGUI>();
    protected readonly Dictionary<string, System.Action> buttonCallbacks = new Dictionary<string, System.Action>();

    protected bool isVisible;
    protected bool isInitialized;
    protected bool isDestroyed;
    protected readonly string prefabAddress;

    protected BaseUIController(string prefabAddress)
    {
        this.prefabAddress = prefabAddress;
    }

    public async void Show()
    {
        if (isDestroyed || isVisible) return;

        if (!isInitialized)
        {
            await InitializeUI();
        }

        if (uiGameObject != null && !isDestroyed)
        {
            uiGameObject.SetActive(true);
            isVisible = true;
            OnShow();
        }
    }

    public void Hide()
    {
        if (isDestroyed || !isVisible) return;

        if (uiGameObject != null)
        {
            uiGameObject.SetActive(false);
            isVisible = false;
            OnHide();
        }
    }

    protected virtual async Task InitializeUI()
    {
        if (isInitialized || isDestroyed) return;

        var addressableManager = ServiceLocator.Get<AddressableManager>();
        if (addressableManager == null)
        {
            CreateFallbackUI();
            return;
        }

        try
        {
            uiGameObject = await addressableManager.InstantiateAsync(prefabAddress);

            if (uiGameObject == null)
            {
                CreateFallbackUI();
                return;
            }

            if (isDestroyed)
            {
                Object.Destroy(uiGameObject);
                return;
            }

            Object.DontDestroyOnLoad(uiGameObject);

            // КРИТИЧНО: Ждем до конца кадра для полной инициализации Unity UI
            await WaitForEndOfFrame();

            InitializeComponents();
            SetupButtonCallbacks();

            uiGameObject.SetActive(false);
            isInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize UI {prefabAddress}: {e.Message}");
            CreateFallbackUI();
        }
    }

    private async Task WaitForEndOfFrame()
    {
        // Ждем конца текущего кадра и еще один кадр для полной инициализации
        await Task.Yield();
        await Task.Yield();
    }

    protected virtual void InitializeComponents()
    {
        if (uiGameObject == null) return;

        CacheComponents();
        ValidateRequiredComponents();
    }

    private void CacheComponents()
    {
        var allButtons = uiGameObject.GetComponentsInChildren<Button>(true);
        var allTexts = uiGameObject.GetComponentsInChildren<Text>(true);
        var allTmpTexts = uiGameObject.GetComponentsInChildren<TextMeshProUGUI>(true);

        foreach (var button in allButtons)
        {
            if (button != null && !string.IsNullOrEmpty(button.name))
            {
                buttons[button.name] = button;
            }
        }

        foreach (var text in allTexts)
        {
            if (text != null && !string.IsNullOrEmpty(text.name))
            {
                texts[text.name] = text;
            }
        }

        foreach (var tmpText in allTmpTexts)
        {
            if (tmpText != null && !string.IsNullOrEmpty(tmpText.name))
            {
                tmpTexts[tmpText.name] = tmpText;
            }
        }
    }

    protected virtual void ValidateRequiredComponents()
    {
        // Переопределяется в наследниках для проверки обязательных компонентов
    }

    protected virtual void SetupButtonCallbacks()
    {
        foreach (var button in buttons.Values)
        {
            if (button == null) continue;

            var buttonName = button.name;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnButtonClicked(buttonName));
        }
    }

    protected virtual void OnButtonClicked(string buttonName)
    {
        if (isDestroyed) return;

        if (buttonCallbacks.TryGetValue(buttonName, out var callback))
        {
            callback?.Invoke();
        }
        else
        {
            HandleButtonClick(buttonName);
        }
    }

    protected virtual void HandleButtonClick(string buttonName) { }

    public void RegisterButtonCallback(string buttonId, System.Action callback)
    {
        if (!string.IsNullOrEmpty(buttonId))
        {
            buttonCallbacks[buttonId] = callback;
        }
    }

    public void SetButtonInteractable(string buttonId, bool interactable)
    {
        if (buttons.TryGetValue(buttonId, out var button) && button != null)
        {
            button.interactable = interactable;
        }
    }

    public void SetText(string textId, string text)
    {
        if (tmpTexts.TryGetValue(textId, out var tmpText) && tmpText != null)
        {
            tmpText.text = text;
            return;
        }

        if (texts.TryGetValue(textId, out var regularText) && regularText != null)
        {
            regularText.text = text;
        }
    }

    public string GetText(string textId)
    {
        if (tmpTexts.TryGetValue(textId, out var tmpText) && tmpText != null)
        {
            return tmpText.text;
        }

        if (texts.TryGetValue(textId, out var regularText) && regularText != null)
        {
            return regularText.text;
        }

        return string.Empty;
    }

    public void SetTextColor(string textId, Color color)
    {
        if (tmpTexts.TryGetValue(textId, out var tmpText) && tmpText != null)
        {
            tmpText.color = color;
            return;
        }

        if (texts.TryGetValue(textId, out var regularText) && regularText != null)
        {
            regularText.color = color;
        }
    }

    public void SetFontSize(string textId, float fontSize)
    {
        if (tmpTexts.TryGetValue(textId, out var tmpText) && tmpText != null)
        {
            tmpText.fontSize = fontSize;
            return;
        }

        if (texts.TryGetValue(textId, out var regularText) && regularText != null)
        {
            regularText.fontSize = (int)fontSize;
        }
    }

    protected void CreateFallbackUI()
    {
        CreateFallbackCanvas();
        CreateFallbackComponents();

        uiGameObject.SetActive(false);
        isInitialized = true;
    }

    private void CreateFallbackCanvas()
    {
        uiGameObject = new GameObject($"Fallback_{prefabAddress}");
        Object.DontDestroyOnLoad(uiGameObject);

        var canvas = uiGameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var canvasScaler = uiGameObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        uiGameObject.AddComponent<GraphicRaycaster>();
    }

    protected virtual void CreateFallbackComponents()
    {
        var fallbackText = CreateFallbackText($"Fallback UI: {prefabAddress}");
        texts["FallbackText"] = fallbackText;
    }

    protected Text CreateFallbackText(string content)
    {
        var textGO = new GameObject("FallbackText");
        textGO.transform.SetParent(uiGameObject.transform, false);

        var text = textGO.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        var rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return text;
    }

    public void SetUIParent(Transform parentTransform) { }
    public bool IsVisible() => !isDestroyed && isVisible && uiGameObject != null && uiGameObject.activeInHierarchy;

    public void Update(float deltaTime)
    {
        if (isDestroyed || !isVisible) return;
        OnUpdate(deltaTime);
    }

    public void Cleanup()
    {
        isDestroyed = true;
        isVisible = false;
        isInitialized = false;

        foreach (var button in buttons.Values)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        buttonCallbacks.Clear();
        buttons.Clear();
        texts.Clear();
        tmpTexts.Clear();

        if (uiGameObject != null)
        {
            Object.Destroy(uiGameObject);
            uiGameObject = null;
        }

        OnCleanup();
    }

    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
    protected virtual void OnUpdate(float deltaTime) { }
    protected virtual void OnCleanup() { }
}