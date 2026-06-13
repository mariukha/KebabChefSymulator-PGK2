using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    private Canvas shopCanvas;
    private GameObject shopPanel;
    private Text balanceText;
    private Text titleText;
    private Text hintText;
    private Text purchaseFeedbackText;
    private readonly List<ShopUpgradeRow> upgradeRows = new List<ShopUpgradeRow>();

    private bool isOpen;
    private float feedbackTimer;
    private float panelAnimationProgress;
    private CanvasGroup panelCanvasGroup;

    private const float FeedbackDuration = 2.5f;
    private const float AnimationSpeed = 8f;

    private static readonly Color BackgroundOverlay = new Color(0.005f, 0.008f, 0.012f, 0.82f);
    private static readonly Color PanelColor = new Color(0.018f, 0.022f, 0.028f, 0.98f);
    private static readonly Color PanelBorderColor = new Color(0.15f, 0.16f, 0.17f, 0.95f);
    private static readonly Color HeaderColor = new Color(0.028f, 0.034f, 0.044f, 0.96f);
    private static readonly Color TitleColor = new Color(0.875f, 0.725f, 0.32f, 1f);
    private static readonly Color BalanceValueColor = new Color(0.22f, 0.82f, 0.42f, 1f);
    private static readonly Color RowBackgroundColor = new Color(0.035f, 0.043f, 0.056f, 0.94f);
    private static readonly Color NameColor = new Color(0.93f, 0.97f, 1f, 1f);
    private static readonly Color DescriptionColor = new Color(0.65f, 0.72f, 0.82f, 1f);
    private static readonly Color LevelActiveColor = new Color(0.875f, 0.78f, 0.52f, 1f);
    private static readonly Color CostColor = new Color(0.96f, 0.97f, 0.99f, 1f);
    private static readonly Color ButtonNormalColor = new Color(0.08f, 0.38f, 0.18f, 1f);
    private static readonly Color ButtonDisabledColor = new Color(0.12f, 0.13f, 0.15f, 0.82f);
    private static readonly Color ButtonMaxedColor = new Color(0.14f, 0.28f, 0.42f, 0.9f);
    private static readonly Color ButtonTextColor = new Color(0.98f, 0.99f, 1f, 1f);
    private static readonly Color HintColor = new Color(0.50f, 0.58f, 0.68f, 0.85f);
    private static readonly Color FeedbackSuccessColor = new Color(0.22f, 0.82f, 0.42f, 1f);
    private static readonly Color FeedbackFailColor = new Color(0.92f, 0.24f, 0.2f, 1f);
    private static readonly Color DividerColor = new Color(0.16f, 0.17f, 0.18f, 0.62f);

    private Font cachedFont;
    private LobbyUI cachedLobbyUI;

    public bool IsShopOpen => isOpen;

    private void Awake()
    {
        EnsureEventSystem();
        CreateShopCanvas();
    }

    private void Update()
    {
        if (cachedLobbyUI == null)
        {
            cachedLobbyUI = FindFirstObjectByType<LobbyUI>();
        }
        bool lobbyOpen = cachedLobbyUI != null && cachedLobbyUI.IsLobbyOpen;
        bool settingsOpen = SettingsMenuUI.Instance != null && SettingsMenuUI.Instance.IsOpen;

        if (Input.GetKeyDown(KeyCode.B) && !lobbyOpen && !settingsOpen)
        {
            ToggleShop();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseShop();
        }

        UpdateAnimation();
        UpdateFeedback();

        if (isOpen)
        {
            RefreshContent();
        }
    }

    public void ToggleShop()
    {
        if (isOpen)
        {
            CloseShop();
        }
        else
        {
            OpenShop();
        }
    }

    public void OpenShop()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        shopPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshContent();
    }

    public void CloseShop()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void UpdateAnimation()
    {
        float target = isOpen ? 1f : 0f;
        panelAnimationProgress = Mathf.MoveTowards(panelAnimationProgress, target, Time.unscaledDeltaTime * AnimationSpeed);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, panelAnimationProgress);
        }

        if (shopPanel != null)
        {
            float scale = Mathf.Lerp(0.92f, 1f, Mathf.SmoothStep(0f, 1f, panelAnimationProgress));
            shopPanel.transform.localScale = new Vector3(scale, scale, 1f);
        }

        if (!isOpen && panelAnimationProgress <= 0.01f && shopPanel.activeSelf)
        {
            shopPanel.SetActive(false);
        }
    }

    private void UpdateFeedback()
    {
        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.unscaledDeltaTime;
            if (feedbackTimer <= 0f && purchaseFeedbackText != null)
            {
                purchaseFeedbackText.text = string.Empty;
            }
        }
    }

    private void RefreshContent()
    {
        if (ShopManager.Instance == null)
        {
            return;
        }

        float balance = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentBalance : 0f;
        if (balanceText != null)
        {
            balanceText.text = balance.ToString("F0") + " zl";
        }

        List<UpgradeDefinition> definitions = ShopManager.Instance.GetAllDefinitions();
        for (int i = 0; i < upgradeRows.Count && i < definitions.Count; i++)
        {
            RefreshRow(upgradeRows[i], definitions[i]);
        }
    }

    private void RefreshRow(ShopUpgradeRow row, UpgradeDefinition definition)
    {
        int currentLevel = ShopManager.Instance.GetUpgradeLevel(definition.type);
        bool isMaxed = currentLevel >= definition.maxLevel;
        bool canAfford = ShopManager.Instance.CanAffordUpgrade(definition.type);

        row.nameText.text = definition.displayName;
        row.descriptionText.text = definition.description;

        string levelDisplay = BuildLevelIndicator(currentLevel, definition.maxLevel);
        row.levelText.text = levelDisplay;

        if (isMaxed)
        {
            row.costText.text = string.Empty;
            row.effectText.text = "Maksymalny poziom";
            row.effectText.color = ButtonMaxedColor;
            row.buttonText.text = "MAX";
            row.buttonImage.color = ButtonMaxedColor;
            row.button.interactable = false;
        }
        else
        {
            float cost = ShopManager.Instance.GetNextUpgradeCost(definition.type);
            row.costText.text = cost.ToString("F0") + " zl";
            row.costText.color = canAfford ? CostColor : FeedbackFailColor;
            row.effectText.text = definition.GetEffectDescription(currentLevel);
            row.effectText.color = definition.accentColor;
            row.buttonText.text = "KUP";
            row.buttonImage.color = canAfford ? ButtonNormalColor : ButtonDisabledColor;
            row.button.interactable = canAfford;
        }
    }

    private string BuildLevelIndicator(int currentLevel, int maxLevel)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("Poziom ");
        builder.Append(currentLevel);
        builder.Append("/");
        builder.Append(maxLevel);

        return builder.ToString();
    }

    private void OnUpgradeButtonClicked(UpgradeType type)
    {
        if (ShopManager.Instance == null)
        {
            return;
        }

        if (ShopManager.Instance.IsMaxLevel(type))
        {
            ShowFeedback("To ulepszenie jest juz na maksymalnym poziomie.", false);
            return;
        }

        if (!ShopManager.Instance.CanAffordUpgrade(type))
        {
            float cost = ShopManager.Instance.GetNextUpgradeCost(type);
            ShowFeedback("Za malo pieniedzy. Potrzebujesz " + cost.ToString("F0") + " zl.", false);
            return;
        }

        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                bool success = ShopManager.Instance.TryPurchaseUpgrade(type);
                HandlePurchaseResult(success, type);
            }
            else
            {
                NetworkPlayer localPlayer = NetworkPlayer.FindLocalPlayer();
                if (localPlayer != null)
                {
                    localPlayer.PurchaseUpgradeServerRpc((int)type);
                }
            }
        }
        else
        {
            bool success = ShopManager.Instance.TryPurchaseUpgrade(type);
            HandlePurchaseResult(success, type);
        }
    }

    public void HandlePurchaseResult(bool success, UpgradeType type)
    {
        if (success)
        {
            UpgradeDefinition definition = ShopManager.Instance.GetDefinition(type);
            string upgradeName = definition != null ? definition.displayName : type.ToString();
            int newLevel = ShopManager.Instance.GetUpgradeLevel(type);
            ShowFeedback("Zakupiono " + upgradeName + " (poz. " + newLevel + ")!", true);
            PlayPurchaseFeedback(definition);
        }
        else
        {
            ShowFeedback("Nie udalo sie zakupic ulepszenia.", false);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayFailSound();
        }
    }

    private void PlayPurchaseFeedback(UpgradeDefinition definition)
    {
        Color accent = definition != null ? definition.accentColor : TitleColor;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUpgradeSound();
        }

        if (VFXManager.Instance != null)
        {
            Vector3 effectPosition = VFXManager.Instance.GetCameraFacingPosition(2.0f, -0.18f);
            VFXManager.Instance.PlayUpgradeEffect(effectPosition, accent);
        }

        if (PostProcessSetup.Instance != null)
        {
            PostProcessSetup.Instance.PulseBloom(0.28f, 0.32f);
        }

        if (CameraEffects.Instance != null)
        {
            CameraEffects.Instance.ShakeCamera(0.025f, 0.12f);
        }
    }

    private void ShowFeedback(string message, bool success)
    {
        if (purchaseFeedbackText != null)
        {
            purchaseFeedbackText.text = message;
            purchaseFeedbackText.color = success ? FeedbackSuccessColor : FeedbackFailColor;
        }

        feedbackTimer = FeedbackDuration;
    }

    private void CreateShopCanvas()
    {
        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject canvasObject = new GameObject("ShopCanvas");
        canvasObject.transform.SetParent(transform, false);

        shopCanvas = canvasObject.AddComponent<Canvas>();
        shopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        shopCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        shopPanel = new GameObject("ShopPanel");
        shopPanel.transform.SetParent(canvasObject.transform, false);

        panelCanvasGroup = shopPanel.AddComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0f;

        RectTransform panelRect = shopPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        CreateOverlay(shopPanel.transform);
        Transform contentPanel = CreateContentPanel(shopPanel.transform);
        CreateHeader(contentPanel);
        CreateUpgradeRows(contentPanel);
        CreateFooter(contentPanel);

        shopPanel.SetActive(false);
    }

    private void CreateOverlay(Transform parent)
    {
        GameObject overlay = new GameObject("Overlay");
        overlay.transform.SetParent(parent, false);

        Image image = overlay.AddComponent<Image>();
        image.color = BackgroundOverlay;
        image.raycastTarget = true;

        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Transform CreateContentPanel(Transform parent)
    {
        GameObject borderPanel = new GameObject("PanelBorder");
        borderPanel.transform.SetParent(parent, false);

        Image borderImage = borderPanel.AddComponent<Image>();
        borderImage.color = PanelBorderColor;

        RectTransform borderRect = borderPanel.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.5f, 0.5f);
        borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = new Vector2(820f, 704f);
        borderRect.anchoredPosition = Vector2.zero;

        GameObject contentObject = new GameObject("ContentPanel");
        contentObject.transform.SetParent(borderPanel.transform, false);

        Image contentImage = contentObject.AddComponent<Image>();
        contentImage.color = PanelColor;

        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(3f, 3f);
        contentRect.offsetMax = new Vector2(-3f, -3f);

        return contentObject.transform;
    }

    private void CreateHeader(Transform parent)
    {
        GameObject headerBand = new GameObject("HeaderBand");
        headerBand.transform.SetParent(parent, false);

        Image headerImage = headerBand.AddComponent<Image>();
        headerImage.color = HeaderColor;

        RectTransform headerRect = headerBand.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, 90f);
        headerRect.anchoredPosition = Vector2.zero;

        titleText = CreateTextElement(
            headerBand.transform,
            "Title",
            "ULEPSZENIA",
            30,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            TitleColor);

        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(0.55f, 1f);
        titleRect.offsetMin = new Vector2(28f, 0f);
        titleRect.offsetMax = new Vector2(0f, 0f);

        balanceText = CreateTextElement(
            headerBand.transform,
            "Balance",
            "0 zl",
            24,
            FontStyle.Bold,
            TextAnchor.MiddleRight,
            BalanceValueColor);

        RectTransform balanceRect = balanceText.GetComponent<RectTransform>();
        balanceRect.anchorMin = new Vector2(0.55f, 0f);
        balanceRect.anchorMax = new Vector2(1f, 1f);
        balanceRect.offsetMin = new Vector2(0f, 0f);
        balanceRect.offsetMax = new Vector2(-28f, 0f);

        CreateDivider(parent, new Vector2(0f, -90f));
    }

    private void CreateUpgradeRows(Transform parent)
    {
        if (ShopManager.Instance == null)
        {
            return;
        }

        List<UpgradeDefinition> definitions = ShopManager.Instance.GetAllDefinitions();
        float startY = -102f;
        float rowHeight = 96f;
        float rowSpacing = 10f;

        for (int i = 0; i < definitions.Count; i++)
        {
            float yPos = startY - i * (rowHeight + rowSpacing);
            ShopUpgradeRow row = CreateSingleUpgradeRow(parent, definitions[i], yPos, rowHeight);
            upgradeRows.Add(row);
        }
    }

    private ShopUpgradeRow CreateSingleUpgradeRow(Transform parent, UpgradeDefinition definition, float yPos, float height)
    {
        ShopUpgradeRow row = new ShopUpgradeRow();

        GameObject rowObject = new GameObject("Row_" + definition.type);
        rowObject.transform.SetParent(parent, false);

        Image rowImage = rowObject.AddComponent<Image>();
        rowImage.color = RowBackgroundColor;

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(-36f, height);
        rowRect.anchoredPosition = new Vector2(0f, yPos);

        row.rowImage = rowImage;

        row.nameText = CreateTextElement(
            rowObject.transform,
            "Name",
            definition.displayName,
            18,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            NameColor);

        RectTransform nameRect = row.nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(0f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.sizeDelta = new Vector2(360f, 26f);
        nameRect.anchoredPosition = new Vector2(28f, -14f);

        row.descriptionText = CreateTextElement(
            rowObject.transform,
            "Description",
            definition.description,
            13,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            DescriptionColor);

        RectTransform descRect = row.descriptionText.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 1f);
        descRect.anchorMax = new Vector2(0f, 1f);
        descRect.pivot = new Vector2(0f, 1f);
        descRect.sizeDelta = new Vector2(390f, 20f);
        descRect.anchoredPosition = new Vector2(28f, -40f);

        row.effectText = CreateTextElement(
            rowObject.transform,
            "Effect",
            "",
            14,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            definition.accentColor);

        RectTransform effectRect = row.effectText.GetComponent<RectTransform>();
        effectRect.anchorMin = new Vector2(0f, 1f);
        effectRect.anchorMax = new Vector2(0f, 1f);
        effectRect.pivot = new Vector2(0f, 1f);
        effectRect.sizeDelta = new Vector2(390f, 22f);
        effectRect.anchoredPosition = new Vector2(28f, -64f);

        row.levelText = CreateTextElement(
            rowObject.transform,
            "Level",
            "",
            14,
            FontStyle.Normal,
            TextAnchor.MiddleRight,
            LevelActiveColor);

        RectTransform levelRect = row.levelText.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(1f, 0.5f);
        levelRect.anchorMax = new Vector2(1f, 0.5f);
        levelRect.pivot = new Vector2(1f, 0.5f);
        levelRect.sizeDelta = new Vector2(150f, 24f);
        levelRect.anchoredPosition = new Vector2(-160f, 18f);

        row.costText = CreateTextElement(
            rowObject.transform,
            "Cost",
            "",
            16,
            FontStyle.Bold,
            TextAnchor.MiddleRight,
            CostColor);

        RectTransform costRect = row.costText.GetComponent<RectTransform>();
        costRect.anchorMin = new Vector2(1f, 0.5f);
        costRect.anchorMax = new Vector2(1f, 0.5f);
        costRect.pivot = new Vector2(1f, 0.5f);
        costRect.sizeDelta = new Vector2(150f, 24f);
        costRect.anchoredPosition = new Vector2(-160f, -14f);

        GameObject buttonObject = new GameObject("BuyButton");
        buttonObject.transform.SetParent(rowObject.transform, false);

        row.buttonImage = buttonObject.AddComponent<Image>();
        row.buttonImage.color = ButtonNormalColor;

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.sizeDelta = new Vector2(112f, 44f);
        buttonRect.anchoredPosition = new Vector2(-20f, 0f);

        row.buttonText = CreateTextElement(
            buttonObject.transform,
            "ButtonLabel",
            "KUP",
            16,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            ButtonTextColor);

        RectTransform buttonTextRect = row.buttonText.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = row.buttonImage;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.disabledColor = Color.white;
        button.colors = colors;

        UpgradeType capturedType = definition.type;
        button.onClick.AddListener(() => OnUpgradeButtonClicked(capturedType));

        row.button = button;

        return row;
    }

    private void CreateFooter(Transform parent)
    {
        float footerY = -690f;

        purchaseFeedbackText = CreateTextElement(
            parent,
            "PurchaseFeedback",
            "",
            17,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            FeedbackSuccessColor);

        RectTransform feedbackRect = purchaseFeedbackText.GetComponent<RectTransform>();
        feedbackRect.anchorMin = new Vector2(0f, 1f);
        feedbackRect.anchorMax = new Vector2(1f, 1f);
        feedbackRect.pivot = new Vector2(0.5f, 1f);
        feedbackRect.sizeDelta = new Vector2(-36f, 28f);
        feedbackRect.anchoredPosition = new Vector2(0f, footerY);

        hintText = CreateTextElement(
            parent,
            "HintText",
            "TAB  Zamknij",
            13,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            HintColor);

        RectTransform hintRect = hintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(0f, 34f);
        hintRect.anchoredPosition = new Vector2(0f, 8f);
    }

    private void CreateDivider(Transform parent, Vector2 position)
    {
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(parent, false);

        Image image = divider.AddComponent<Image>();
        image.color = DividerColor;

        RectTransform rect = divider.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(-36f, 2f);
        rect.anchoredPosition = position;
    }

    private Text CreateTextElement(
        Transform parent,
        string objectName,
        string defaultText,
        int fontSize,
        FontStyle style,
        TextAnchor alignment,
        Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = cachedFont;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.text = defaultText;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        return text;
    }

    private class ShopUpgradeRow
    {
        public Image rowImage;
        public Image buttonImage;
        public Text nameText;
        public Text descriptionText;
        public Text effectText;
        public Text levelText;
        public Text costText;
        public Text buttonText;
        public Button button;
    }
}
