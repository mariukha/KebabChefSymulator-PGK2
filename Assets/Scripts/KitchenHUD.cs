using UnityEngine;
using UnityEngine.UI;

public class KitchenHUD : MonoBehaviour
{
    private PlayerInteraction playerInteraction;
    private ShopUI cachedShopUI;
    private LobbyUI cachedLobbyUI;

    private Canvas hudCanvas;
    private Text balanceText;
    private Text heldItemText;
    private Text sessionText;
    private Text promptText;
    private Text feedbackText;
    private Text crosshairText;
    private Text shopHintText;
    private Text upgradeStatusText;
    private Image promptBackground;
    private RectTransform upgradeBar;

    // Floating money text animation
    private Text floatingMoneyText;
    private float floatingMoneyTimer;
    private float floatingMoneyStartY;
    private float lastKnownBalance;
    private float balancePulseTimer;

    private void Awake()
    {
        CreateCanvas();
    }

    private void Update()
    {
        if (playerInteraction == null)
        {
            // Find the LOCAL player's interaction (not a remote player's disabled one)
            NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (NetworkPlayer np in players)
            {
                if (np.IsOwner)
                {
                    playerInteraction = np.GetComponent<PlayerInteraction>();
                    break;
                }
            }
        }

        if (cachedShopUI == null)
        {
            cachedShopUI = FindFirstObjectByType<ShopUI>();
        }

        if (cachedLobbyUI == null)
        {
            cachedLobbyUI = FindFirstObjectByType<LobbyUI>();
        }

        // Hide HUD when lobby is open
        bool lobbyOpen = cachedLobbyUI != null && cachedLobbyUI.IsLobbyOpen;
        if (hudCanvas != null)
        {
            hudCanvas.enabled = !lobbyOpen;
        }

        if (!lobbyOpen)
        {
            RefreshTexts();
            UpdateFloatingMoney();
        }
    }

    private void CreateCanvas()
    {
        if (hudCanvas != null)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject canvasObject = new GameObject("HUDCanvas");
        canvasObject.transform.SetParent(transform, false);

        hudCanvas = canvasObject.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform topBar = CreatePanel(
            canvasObject.transform,
            "TopStatusBar",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, 0f),
            new Vector2(0f, 28f),
            new Color(0.03f, 0.05f, 0.07f, 0.52f));

        CreateSectionDivider(topBar, "DividerOne", new Vector2(0.333f, 0.5f));
        CreateSectionDivider(topBar, "DividerTwo", new Vector2(0.666f, 0.5f));

        balanceText = CreateText(
            topBar,
            "BalanceText",
            font,
            11,
            TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(14f, -3f),
            new Vector2(220f, 18f),
            FontStyle.Bold,
            new Color(0.95f, 0.97f, 1f, 0.94f));

        heldItemText = CreateText(
            topBar,
            "HeldItemText",
            font,
            11,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -3f),
            new Vector2(260f, 18f),
            FontStyle.Normal,
            new Color(0.9f, 0.95f, 1f, 0.92f));

        sessionText = CreateText(
            topBar,
            "SessionText",
            font,
            11,
            TextAnchor.MiddleRight,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-14f, -3f),
            new Vector2(240f, 18f),
            FontStyle.Normal,
            new Color(0.9f, 0.95f, 1f, 0.92f));

        upgradeBar = CreatePanel(
            canvasObject.transform,
            "UpgradeStatusBar",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -28f),
            new Vector2(0f, 22f),
            new Color(0.04f, 0.06f, 0.09f, 0.48f));

        shopHintText = CreateText(
            upgradeBar,
            "ShopHintText",
            font,
            10,
            TextAnchor.MiddleRight,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-14f, 0f),
            new Vector2(120f, 14f),
            FontStyle.Normal,
            new Color(0.78f, 0.72f, 0.45f, 0.85f));
        shopHintText.text = "B: Sklep | TAB: Gracze | F1: Lobby";

        upgradeStatusText = CreateText(
            upgradeBar,
            "UpgradeStatusText",
            font,
            10,
            TextAnchor.MiddleCenter,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            FontStyle.Normal,
            new Color(0.72f, 0.80f, 0.90f, 0.82f));
        upgradeStatusText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        upgradeStatusText.GetComponent<RectTransform>().anchorMax = Vector2.one;
        upgradeStatusText.GetComponent<RectTransform>().offsetMin = new Vector2(14f, 0f);
        upgradeStatusText.GetComponent<RectTransform>().offsetMax = new Vector2(-14f, 0f);

        RectTransform promptPanel = CreatePanel(
            canvasObject.transform,
            "PromptPanel",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 22f),
            new Vector2(360f, 28f),
            new Color(0.08f, 0.08f, 0.08f, 0.68f));
        promptBackground = promptPanel.GetComponent<Image>();

        promptText = CreateText(
            promptPanel,
            "PromptText",
            font,
            11,
            TextAnchor.MiddleCenter,
            new Vector2(0f, -1f),
            new Vector2(338f, 18f),
            FontStyle.Bold,
            new Color(1f, 0.92f, 0.65f, 1f));

        feedbackText = CreateText(
            canvasObject.transform,
            "FeedbackText",
            font,
            10,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 52f),
            new Vector2(580f, 18f),
            FontStyle.Normal,
            new Color(0.94f, 0.96f, 0.98f));

        crosshairText = CreateText(
            canvasObject.transform,
            "Crosshair",
            font,
            16,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(18f, 18f),
            FontStyle.Bold,
            new Color(1f, 1f, 1f, 0.92f));
        crosshairText.text = "+";

        // Floating money text (hidden by default, shown when player earns money)
        floatingMoneyText = CreateText(
            canvasObject.transform,
            "FloatingMoney",
            font,
            22,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 80f),
            new Vector2(300f, 40f),
            FontStyle.Bold,
            new Color(0.22f, 0.82f, 0.42f, 0f));
        floatingMoneyText.text = string.Empty;
        floatingMoneyStartY = 80f;

        // Initialize balance tracking
        lastKnownBalance = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentBalance : 0f;
    }

    private void RefreshTexts()
    {
        if (balanceText == null || heldItemText == null || sessionText == null)
        {
            return;
        }

        float balance = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentBalance : 0f;
        int completed = OrderManager.Instance != null ? OrderManager.Instance.CompletedOrders : 0;
        int failed = OrderManager.Instance != null ? OrderManager.Instance.FailedOrders : 0;
        string heldItem = playerInteraction != null ? Truncate(playerInteraction.GetHeldItemSummary(), 24) : "brak danych";

        balanceText.text = "BALANCE  " + balance + " zl";
        heldItemText.text = "HELD  " + heldItem;
        sessionText.text = "SESSION  " + completed + " OK / " + failed + " FAIL";

        bool shopOpen = cachedShopUI != null && cachedShopUI.IsShopOpen;

        if (playerInteraction != null && !shopOpen)
        {
            promptText.text = playerInteraction.CurrentPrompt;
            feedbackText.text = playerInteraction.FeedbackMessage;
            if (promptBackground != null)
            {
                promptBackground.enabled = !string.IsNullOrWhiteSpace(playerInteraction.CurrentPrompt);
            }
        }
        else
        {
            promptText.text = string.Empty;
            feedbackText.text = shopOpen ? string.Empty : feedbackText.text;
            if (promptBackground != null)
            {
                promptBackground.enabled = false;
            }
        }

        if (crosshairText != null)
        {
            crosshairText.enabled = !shopOpen;
        }

        UpdateUpgradeStatus();

        // Detect balance change and trigger floating money effect
        float currentBalance = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentBalance : 0f;
        if (currentBalance > lastKnownBalance + 0.01f)
        {
            float earned = currentBalance - lastKnownBalance;
            ShowFloatingMoney(earned);
        }
        lastKnownBalance = currentBalance;

        // Pulse balance text color when recently changed
        if (balancePulseTimer > 0f)
        {
            balancePulseTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(balancePulseTimer / 0.8f);
            // Interpolate from bright gold/green back to normal white
            balanceText.color = Color.Lerp(
                new Color(0.95f, 0.97f, 1f, 0.94f),
                new Color(0.22f, 1f, 0.42f, 1f),
                t);
        }
    }

    /// <summary>
    /// Pokazuje animowany tekst "+X zl" unoszacy sie od srodka ekranu.
    /// Wywoływany automatycznie gdy balance wzrosnie.
    /// </summary>
    private void ShowFloatingMoney(float amount)
    {
        if (floatingMoneyText == null)
        {
            return;
        }

        floatingMoneyText.text = "+" + amount.ToString("F0") + " zl";
        floatingMoneyText.color = new Color(0.22f, 0.92f, 0.42f, 1f);
        floatingMoneyTimer = 1.6f;
        balancePulseTimer = 0.8f;

        RectTransform rect = floatingMoneyText.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(0f, floatingMoneyStartY);
        }
    }

    /// <summary>
    /// Animuje floating money text: unosi sie do gory i zanika.
    /// </summary>
    private void UpdateFloatingMoney()
    {
        if (floatingMoneyText == null || floatingMoneyTimer <= 0f)
        {
            return;
        }

        floatingMoneyTimer -= Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(floatingMoneyTimer / 1.6f);

        // Ruch do gory
        RectTransform rect = floatingMoneyText.GetComponent<RectTransform>();
        if (rect != null)
        {
            float yOffset = floatingMoneyStartY + progress * 60f;
            rect.anchoredPosition = new Vector2(0f, yOffset);
        }

        // Fade out w drugiej polowie animacji
        float alpha = progress < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.5f) * 2f);
        float scale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.15f;
        floatingMoneyText.color = new Color(0.22f, 0.92f, 0.42f, alpha);
        floatingMoneyText.fontSize = Mathf.RoundToInt(22f * scale);

        if (floatingMoneyTimer <= 0f)
        {
            floatingMoneyText.text = string.Empty;
        }
    }

    private void UpdateUpgradeStatus()
    {
        if (upgradeStatusText == null || upgradeBar == null)
        {
            return;
        }

        if (ShopManager.Instance == null || ShopManager.Instance.TotalUpgradesPurchased <= 0)
        {
            upgradeBar.gameObject.SetActive(false);
            return;
        }

        upgradeBar.gameObject.SetActive(true);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("BONUSY AKTYWNE:  ");
        bool hasAny = false;

        int grillLvl = ShopManager.Instance.GetUpgradeLevel(UpgradeType.GrillSpeed);
        if (grillLvl > 0)
        {
            int pct = Mathf.RoundToInt((1f - ShopManager.Instance.GetProcessingSpeedMultiplier(KitchenStationType.Grill)) * 100f);
            sb.Append("Grill -" + pct + "%");
            hasAny = true;
        }

        int cutLvl = ShopManager.Instance.GetUpgradeLevel(UpgradeType.CuttingSpeed);
        if (cutLvl > 0)
        {
            if (hasAny) sb.Append("  |  ");
            int pct = Mathf.RoundToInt((1f - ShopManager.Instance.GetProcessingSpeedMultiplier(KitchenStationType.CuttingBoard)) * 100f);
            sb.Append("Noz -" + pct + "%");
            hasAny = true;
        }

        int rewardLvl = ShopManager.Instance.GetUpgradeLevel(UpgradeType.RewardBonus);
        if (rewardLvl > 0)
        {
            if (hasAny) sb.Append("  |  ");
            int pct = Mathf.RoundToInt((ShopManager.Instance.GetRewardMultiplier() - 1f) * 100f);
            sb.Append("Nagroda +" + pct + "%");
            hasAny = true;
        }

        int timeLvl = ShopManager.Instance.GetUpgradeLevel(UpgradeType.OrderTime);
        if (timeLvl > 0)
        {
            if (hasAny) sb.Append("  |  ");
            float bonus = ShopManager.Instance.GetOrderTimeBonus();
            sb.Append("Czas +" + bonus + "s");
            hasAny = true;
        }

        int meatLvl = ShopManager.Instance.GetUpgradeLevel(UpgradeType.MeatBatchSize);
        if (meatLvl > 0)
        {
            if (hasAny) sb.Append("  |  ");
            int batch = ShopManager.Instance.GetMeatBatchSize();
            sb.Append("Mieso x" + batch);
        }

        upgradeStatusText.text = sb.ToString();
    }

    private string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength - 1) + "...";
    }

    private void CreateSectionDivider(Transform parent, string objectName, Vector2 anchor)
    {
        CreatePanel(
            parent,
            objectName,
            anchor,
            anchor,
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -10f),
            new Vector2(1f, 14f),
            new Color(1f, 1f, 1f, 0.12f));
    }

    private RectTransform CreatePanel(
        Transform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size,
        Color backgroundColor)
    {
        GameObject panelObject = new GameObject(objectName);
        panelObject.transform.SetParent(parent, false);

        Image image = panelObject.AddComponent<Image>();
        image.color = backgroundColor;

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private Text CreateText(
        Transform parent,
        string objectName,
        Font font,
        int fontSize,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 size,
        FontStyle fontStyle,
        Color color)
    {
        return CreateText(
            parent,
            objectName,
            font,
            fontSize,
            alignment,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            anchoredPosition,
            size,
            fontStyle,
            color);
    }

    private Text CreateText(
        Transform parent,
        string objectName,
        Font font,
        int fontSize,
        TextAnchor alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size,
        FontStyle fontStyle,
        Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = fontStyle;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        return text;
    }
}
