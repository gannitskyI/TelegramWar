using System.Collections.Generic;
using UnityEngine;

public class UpgradeSelectionUIController : BaseUIController
{
    private const string TITLE_TEXT = "TitleText";
    private const string UPGRADE_BUTTON_1 = "UpgradeButton1";
    private const string UPGRADE_BUTTON_2 = "UpgradeButton2";
    private const string UPGRADE_BUTTON_3 = "UpgradeButton3";
    private const string UPGRADE_TEXT_1 = "UpgradeText1";
    private const string UPGRADE_TEXT_2 = "UpgradeText2";
    private const string UPGRADE_TEXT_3 = "UpgradeText3";
    private const string SKIP_BUTTON = "SkipButton";

    private readonly string[] requiredButtons = { UPGRADE_BUTTON_1, UPGRADE_BUTTON_2, UPGRADE_BUTTON_3, SKIP_BUTTON };
    private readonly string[] requiredTexts = { TITLE_TEXT, UPGRADE_TEXT_1, UPGRADE_TEXT_2, UPGRADE_TEXT_3 };

    private List<Upgrade> currentUpgrades;

    public System.Action<int> OnUpgradeSelected;

    public UpgradeSelectionUIController() : base("UpgradeSelectionUI") { }

    protected override void ValidateRequiredComponents()
    {
        base.ValidateRequiredComponents();

        foreach (var buttonName in requiredButtons)
        {
            if (!buttons.ContainsKey(buttonName))
            {
                Debug.LogError($"Required button missing: {buttonName}");
            }
        }

        foreach (var textName in requiredTexts)
        {
            if (!texts.ContainsKey(textName) && !tmpTexts.ContainsKey(textName))
            {
                Debug.LogError($"Required text component missing: {textName}");
            }
        }
    }

    protected override void CreateFallbackComponents()
    {
        CreateFallbackBackground();
        CreateFallbackTitle();
        CreateFallbackUpgradeButtons();
        CreateFallbackSkipButton();
    }

    private void CreateFallbackBackground()
    {
        var backgroundGO = new GameObject("Background");
        backgroundGO.transform.SetParent(uiGameObject.transform, false);

        var image = backgroundGO.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0f, 0f, 0f, 0.8f);

        var rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void CreateFallbackTitle()
    {
        var titleGO = new GameObject(TITLE_TEXT);
        titleGO.transform.SetParent(uiGameObject.transform, false);

        var titleText = titleGO.AddComponent<UnityEngine.UI.Text>();
        titleText.text = "Choose an Upgrade";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 48;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;

        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.8f);
        titleRect.anchorMax = new Vector2(0.9f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        texts[TITLE_TEXT] = titleText;
    }

    private void CreateFallbackUpgradeButtons()
    {
        string[] buttonNames = { UPGRADE_BUTTON_1, UPGRADE_BUTTON_2, UPGRADE_BUTTON_3 };
        string[] textNames = { UPGRADE_TEXT_1, UPGRADE_TEXT_2, UPGRADE_TEXT_3 };

        for (int i = 0; i < 3; i++)
        {
            float yPos = 0.65f - (i * 0.15f);
            CreateUpgradeButton(buttonNames[i], textNames[i], yPos);
        }
    }

    private void CreateUpgradeButton(string buttonName, string textName, float yPosition)
    {
        var buttonGO = new GameObject(buttonName);
        buttonGO.transform.SetParent(uiGameObject.transform, false);

        var button = buttonGO.AddComponent<UnityEngine.UI.Button>();
        var buttonImage = buttonGO.AddComponent<UnityEngine.UI.Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        var buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.1f, yPosition - 0.05f);
        buttonRect.anchorMax = new Vector2(0.9f, yPosition + 0.05f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        var textGO = new GameObject(textName);
        textGO.transform.SetParent(buttonGO.transform, false);

        var text = textGO.AddComponent<UnityEngine.UI.Text>();
        text.text = "Loading...";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        buttons[buttonName] = button;
        texts[textName] = text;
    }

    private void CreateFallbackSkipButton()
    {
        var skipGO = new GameObject(SKIP_BUTTON);
        skipGO.transform.SetParent(uiGameObject.transform, false);

        var skipButton = skipGO.AddComponent<UnityEngine.UI.Button>();
        var skipImage = skipGO.AddComponent<UnityEngine.UI.Image>();
        skipImage.color = new Color(0.5f, 0.1f, 0.1f, 0.8f);

        var skipRect = skipButton.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(0.35f, 0.05f);
        skipRect.anchorMax = new Vector2(0.65f, 0.15f);
        skipRect.offsetMin = Vector2.zero;
        skipRect.offsetMax = Vector2.zero;

        var skipTextGO = new GameObject("SkipText");
        skipTextGO.transform.SetParent(skipGO.transform, false);

        var skipText = skipTextGO.AddComponent<UnityEngine.UI.Text>();
        skipText.text = "Skip";
        skipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skipText.fontSize = 28;
        skipText.color = Color.white;
        skipText.alignment = TextAnchor.MiddleCenter;

        var skipTextRect = skipText.GetComponent<RectTransform>();
        skipTextRect.anchorMin = Vector2.zero;
        skipTextRect.anchorMax = Vector2.one;
        skipTextRect.offsetMin = Vector2.zero;
        skipTextRect.offsetMax = Vector2.zero;

        buttons[SKIP_BUTTON] = skipButton;
    }

    protected override void OnShow()
    {
        base.OnShow();

        SetText(TITLE_TEXT, "Choose an Upgrade");

        if (currentUpgrades != null)
        {
            UpdateUpgradeDisplay();
        }
        else
        {
            ShowLoadingState();
        }

        SetButtonInteractable(SKIP_BUTTON, true);
    }

    private void ShowLoadingState()
    {
        for (int i = 0; i < 3; i++)
        {
            SetText(GetUpgradeTextId(i), "Loading upgrades...");
            SetButtonInteractable(GetUpgradeButtonId(i), false);
        }
    }

    public void SetUpgradeOptions(List<Upgrade> upgrades)
    {
        currentUpgrades = upgrades;

        if (IsVisible())
        {
            UpdateUpgradeDisplay();
        }
    }

    private void UpdateUpgradeDisplay()
    {
        if (currentUpgrades == null) return;

        for (int i = 0; i < 3; i++)
        {
            bool hasUpgrade = i < currentUpgrades.Count;

            SetText(GetUpgradeTextId(i), hasUpgrade ? currentUpgrades[i].GetDisplayText() : "");
            SetButtonInteractable(GetUpgradeButtonId(i), hasUpgrade);

            if (hasUpgrade)
            {
                SetUpgradeButtonColor(GetUpgradeButtonId(i), currentUpgrades[i].type);
            }
        }
    }

    private void SetUpgradeButtonColor(string buttonId, UpgradeType upgradeType)
    {
        if (!buttons.TryGetValue(buttonId, out var button)) return;

        var buttonImage = button.GetComponent<UnityEngine.UI.Image>();
        if (buttonImage != null)
        {
            buttonImage.color = GetUpgradeTypeColor(upgradeType);
        }
    }

    private Color GetUpgradeTypeColor(UpgradeType type)
    {
        return type switch
        {
            UpgradeType.Damage or UpgradeType.CriticalDamage => new Color(1f, 0.3f, 0.3f, 1f),
            UpgradeType.AttackSpeed or UpgradeType.AttackRange => new Color(1f, 0.7f, 0.3f, 1f),
            UpgradeType.Health or UpgradeType.HealthRegen => new Color(0.3f, 1f, 0.3f, 1f),
            UpgradeType.MoveSpeed => new Color(0.3f, 0.7f, 1f, 1f),
            UpgradeType.ExperienceMultiplier => new Color(1f, 1f, 0.3f, 1f),
            UpgradeType.CriticalChance => new Color(0.8f, 0.3f, 1f, 1f),
            _ => Color.white
        };
    }

    private string GetUpgradeButtonId(int index) => index switch
    {
        0 => UPGRADE_BUTTON_1,
        1 => UPGRADE_BUTTON_2,
        2 => UPGRADE_BUTTON_3,
        _ => ""
    };

    private string GetUpgradeTextId(int index) => index switch
    {
        0 => UPGRADE_TEXT_1,
        1 => UPGRADE_TEXT_2,
        2 => UPGRADE_TEXT_3,
        _ => ""
    };

    protected override void HandleButtonClick(string buttonName)
    {
        switch (buttonName)
        {
            case UPGRADE_BUTTON_1: SelectUpgrade(0); break;
            case UPGRADE_BUTTON_2: SelectUpgrade(1); break;
            case UPGRADE_BUTTON_3: SelectUpgrade(2); break;
            case SKIP_BUTTON: SkipUpgrade(); break;
        }
    }

    private void SelectUpgrade(int upgradeIndex)
    {
        if (currentUpgrades == null || upgradeIndex >= currentUpgrades.Count) return;

        DisableAllButtons();
        OnUpgradeSelected?.Invoke(upgradeIndex);
    }

    private void SkipUpgrade()
    {
        DisableAllButtons();
        GamePauseManager.Instance.ResumeGame();
    }

    private void DisableAllButtons()
    {
        foreach (var buttonName in requiredButtons)
        {
            SetButtonInteractable(buttonName, false);
        }
    }

    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        AnimateButtons();
    }

    private void AnimateButtons()
    {
        if (currentUpgrades == null) return;

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 2f) * 0.03f;

        for (int i = 0; i < Mathf.Min(3, currentUpgrades.Count); i++)
        {
            string buttonId = GetUpgradeButtonId(i);
            if (buttons.TryGetValue(buttonId, out var button) && button.interactable)
            {
                button.transform.localScale = Vector3.one * pulse;
            }
        }
    }

    protected override void OnHide()
    {
        base.OnHide();
        ResetButtonScales();
    }

    private void ResetButtonScales()
    {
        foreach (var button in buttons.Values)
        {
            if (button != null)
            {
                button.transform.localScale = Vector3.one;
            }
        }
    }

    protected override void OnCleanup()
    {
        base.OnCleanup();
        currentUpgrades = null;
        OnUpgradeSelected = null;
    }
}