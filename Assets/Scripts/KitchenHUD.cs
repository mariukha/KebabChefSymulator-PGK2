using UnityEngine;
using UnityEngine.UI;

public class KitchenHUD : MonoBehaviour
{
    private PlayerInteraction playerInteraction;
    private ShopUI cachedShopUI;
    private LobbyUI cachedLobbyUI;
    private float lastPlayerSearchTime;

    private Canvas hudCanvas;
    private Text balanceText;
    private Text heldItemText;
    private Text sessionText;
    private Text promptText;
    private Text feedbackText;
    private Text shopHintText;
    private Text upgradeStatusText;
    private Image promptBackground;
    private RectTransform upgradeBar;
    private RectTransform toastPanel;
    private CanvasGroup toastCanvasGroup;
    private Image toastAccentImage;
    private float toastTimer;
    private string lastToastMessage = string.Empty;
    private Color toastAccentColor = AccentGold;

    private RectTransform crosshairContainer;
    private Image crosshairTop;
    private Image crosshairBottom;
    private Image crosshairLeft;
    private Image crosshairRight;
    private Image crosshairDot;

    private Text floatingMoneyText;
    private float floatingMoneyTimer;
    private float floatingMoneyStartY;
    private float lastKnownBalance;
    private float balancePulseTimer;
    private float displayedBalance;

    private Text streakText;
    private Text timerText;
    private Image timerPanelImage;
    private int currentStreak;
    private float streakDisplayTimer;

    private Font hudFont;

    private static readonly Color PanelDark = new Color(0.025f, 0.03f, 0.04f, 0.58f);
    private static readonly Color PanelMedium = new Color(0.035f, 0.043f, 0.058f, 0.48f);
    private static readonly Color PanelLight = new Color(0.045f, 0.052f, 0.068f, 0.48f);
    private static readonly Color AccentGold = new Color(0.86f, 0.68f, 0.28f, 1f);

    private static readonly Color TextPrimary = new Color(0.92f, 0.93f, 0.95f, 0.95f);
    private static readonly Color TextSecondary = new Color(0.68f, 0.72f, 0.78f, 0.85f);
    private static readonly Color TextMuted = new Color(0.55f, 0.58f, 0.65f, 0.70f);
    private static readonly Color GreenAccent = new Color(0.35f, 0.85f, 0.50f, 1f);
    private static readonly Color RedAccent = new Color(0.95f, 0.35f, 0.30f, 1f);
    private static readonly Color DividerColor = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color CrosshairDefault = new Color(0.85f, 0.87f, 0.90f, 0.32f);
    private static readonly Color CrosshairActive = new Color(0.95f, 0.78f, 0.30f, 0.72f);
    private static readonly Color ToastBackground = new Color(0.012f, 0.014f, 0.018f, 0.74f);

    private void Awake()
    {
        CreateCanvas();
    }

    private void Update()
    {
        if (playerInteraction == null)
        {
            if (Time.time - lastPlayerSearchTime > 1f)
            {
                lastPlayerSearchTime = Time.time;
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
        }

        if (cachedShopUI == null)
        {
            cachedShopUI = FindFirstObjectByType<ShopUI>();
        }

        if (cachedLobbyUI == null)
        {
            cachedLobbyUI = FindFirstObjectByType<LobbyUI>();
        }

        bool lobbyOpen = cachedLobbyUI != null && cachedLobbyUI.IsLobbyOpen;
        if (hudCanvas != null)
        {
            hudCanvas.enabled = !lobbyOpen;
        }

        if (!lobbyOpen)
        {
            RefreshTexts();
            UpdateFloatingMoney();
            UpdateToastAnimation();
        }
    }

    private void CreateCanvas()
    {
        if (hudCanvas != null)
        {
            return;
        }

        hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (hudFont == null)
        {
            hudFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject canvasObject = new GameObject("HUDCanvas");
        canvasObject.transform.SetParent(transform, false);

        hudCanvas = canvasObject.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform topBar = CreatePanel(
            canvasObject.transform,
            "TopStatusBar",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, 0f),
            new Vector2(0f, 36f),
            PanelDark);

        CreatePanel(topBar, "TopBarEdge",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(0f, 1f),
            new Color(AccentGold.r, AccentGold.g, AccentGold.b, 0.18f));

        CreateSectionDivider(topBar, "DividerOne", new Vector2(0.35f, 0.5f));
        CreateSectionDivider(topBar, "DividerTwo", new Vector2(0.65f, 0.5f));

        balanceText = CreateText(
            topBar, "BalanceText", hudFont, 15,
            TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(16f, 0f), new Vector2(200f, 22f),
            FontStyle.Bold, AccentGold);
        AddTextShadow(balanceText);

        heldItemText = CreateText(
            topBar, "HeldItemText", hudFont, 14,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(360f, 22f),
            FontStyle.Normal, TextPrimary);
        AddTextShadow(heldItemText);

        sessionText = CreateText(
            topBar, "SessionText", hudFont, 14,
            TextAnchor.MiddleRight,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-16f, 0f), new Vector2(140f, 22f),
            FontStyle.Bold, TextPrimary);
        AddTextShadow(sessionText);

        upgradeBar = CreatePanel(
            canvasObject.transform,
            "UpgradeStatusBar",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -36f),
            new Vector2(0f, 24f),
            PanelMedium);

        CreatePanel(upgradeBar, "UpgradeBarEdge",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 1f),
            new Color(1f, 1f, 1f, 0.04f));

        upgradeStatusText = CreateText(
            upgradeBar, "UpgradeStatusText", hudFont, 11,
            TextAnchor.MiddleCenter,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            FontStyle.Normal, TextSecondary);
        RectTransform upgradeTextRect = upgradeStatusText.GetComponent<RectTransform>();
        upgradeTextRect.anchorMin = Vector2.zero;
        upgradeTextRect.anchorMax = Vector2.one;
        upgradeTextRect.offsetMin = new Vector2(14f, 0f);
        upgradeTextRect.offsetMax = new Vector2(-14f, 0f);
        AddTextShadow(upgradeStatusText);

        RectTransform hintPill = CreateRoundedPill(
            canvasObject.transform,
            "ControlHintsPill",
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-16f, 14f),
            new Vector2(230f, 28f),
            PanelLight);

        shopHintText = CreateText(
            hintPill, "ShopHintText", hudFont, 11,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(220f, 20f),
            FontStyle.Normal, TextMuted);
        UpdateControlHintText();

        RectTransform timerPanel = CreateRoundedPill(
            canvasObject.transform,
            "TimerPanel",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -66f),
            new Vector2(168f, 28f),
            PanelDark);
        timerPanelImage = timerPanel.GetComponent<Image>();

        timerText = CreateText(
            timerPanel, "TimerText", hudFont, 14,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(148f, 22f),
            FontStyle.Bold, AccentGold);
        timerText.text = string.Empty;
        AddTextShadow(timerText);

        RectTransform promptPanel = CreateRoundedPill(
            canvasObject.transform,
            "PromptPanel",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 58f),
            new Vector2(340f, 34f),
            PanelDark);
        promptBackground = promptPanel.GetComponent<Image>();

        promptText = CreateText(
            promptPanel, "PromptText", hudFont, 13,
            TextAnchor.MiddleCenter,
            new Vector2(0f, 0f),
            new Vector2(320f, 22f),
            FontStyle.Bold, AccentGold);
        AddTextShadow(promptText);

        toastPanel = CreateRoundedPill(
            canvasObject.transform,
            "FeedbackToast",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 98f),
            new Vector2(520f, 34f),
            ToastBackground);
        toastCanvasGroup = toastPanel.gameObject.AddComponent<CanvasGroup>();
        toastCanvasGroup.alpha = 0f;
        toastCanvasGroup.blocksRaycasts = false;

        toastAccentImage = CreatePanel(
            toastPanel,
            "ToastAccent",
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0f),
            new Vector2(3f, 24f),
            AccentGold).GetComponent<Image>();

        feedbackText = CreateText(
            toastPanel, "FeedbackText", hudFont, 12,
            TextAnchor.MiddleLeft,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
            new Vector2(16f, 6f), new Vector2(-30f, 22f),
            FontStyle.Normal, TextPrimary);
        AddTextShadow(feedbackText);
        toastPanel.gameObject.SetActive(false);

        CreateModernCrosshair(canvasObject.transform);

        floatingMoneyText = CreateText(
            canvasObject.transform, "FloatingMoney", hudFont, 24,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 80f), new Vector2(300f, 40f),
            FontStyle.Bold, new Color(0.22f, 0.82f, 0.42f, 0f));
        floatingMoneyText.text = string.Empty;
        floatingMoneyStartY = 80f;
        AddTextShadow(floatingMoneyText);

        lastKnownBalance = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentBalance : 0f;
        displayedBalance = lastKnownBalance;

        streakText = CreateText(
            canvasObject.transform, "StreakText", hudFont, 21,
            TextAnchor.MiddleRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-18f, -64f), new Vector2(200f, 32f),
            FontStyle.Bold, new Color(1f, 0.6f, 0.15f, 0f));
        streakText.text = string.Empty;
        AddTextShadow(streakText);

    }

    private void CreateModernCrosshair(Transform parent)
    {
        GameObject container = new GameObject("CrosshairContainer");
        container.transform.SetParent(parent, false);
        crosshairContainer = container.AddComponent<RectTransform>();
        crosshairContainer.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairContainer.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairContainer.pivot = new Vector2(0.5f, 0.5f);
        crosshairContainer.anchoredPosition = Vector2.zero;
        crosshairContainer.sizeDelta = new Vector2(24f, 24f);

        float lineLength = 8f;
        float lineThickness = 1.5f;
        float gap = 3f;

        crosshairTop = CreateCrosshairLine(crosshairContainer, "Top",
            new Vector2(0f, gap + lineLength / 2f), new Vector2(lineThickness, lineLength));

        crosshairBottom = CreateCrosshairLine(crosshairContainer, "Bottom",
            new Vector2(0f, -(gap + lineLength / 2f)), new Vector2(lineThickness, lineLength));

        crosshairLeft = CreateCrosshairLine(crosshairContainer, "Left",
            new Vector2(-(gap + lineLength / 2f), 0f), new Vector2(lineLength, lineThickness));

        crosshairRight = CreateCrosshairLine(crosshairContainer, "Right",
            new Vector2(gap + lineLength / 2f, 0f), new Vector2(lineLength, lineThickness));

        crosshairDot = CreateCrosshairLine(crosshairContainer, "Dot",
            Vector2.zero, new Vector2(2f, 2f));
    }

    private Image CreateCrosshairLine(RectTransform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject("Crosshair_" + name);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = CrosshairDefault;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return img;
    }

    private void UpdateCrosshair(bool hasTarget, bool shopOpen)
    {
        if (crosshairContainer == null)
        {
            return;
        }

        crosshairContainer.gameObject.SetActive(!shopOpen);

        Color targetColor = hasTarget ? CrosshairActive : CrosshairDefault;
        float targetGap = hasTarget ? 5f : 3f;
        float lineLength = 8f;

        float lerpSpeed = Time.deltaTime * 10f;

        crosshairTop.color = Color.Lerp(crosshairTop.color, targetColor, lerpSpeed);
        crosshairBottom.color = Color.Lerp(crosshairBottom.color, targetColor, lerpSpeed);
        crosshairLeft.color = Color.Lerp(crosshairLeft.color, targetColor, lerpSpeed);
        crosshairRight.color = Color.Lerp(crosshairRight.color, targetColor, lerpSpeed);
        crosshairDot.color = Color.Lerp(crosshairDot.color, targetColor, lerpSpeed);

        RectTransform topRect = crosshairTop.GetComponent<RectTransform>();
        RectTransform bottomRect = crosshairBottom.GetComponent<RectTransform>();
        RectTransform leftRect = crosshairLeft.GetComponent<RectTransform>();
        RectTransform rightRect = crosshairRight.GetComponent<RectTransform>();

        float currentGap = topRect.anchoredPosition.y - lineLength / 2f;
        float newGap = Mathf.Lerp(currentGap, targetGap, lerpSpeed);

        topRect.anchoredPosition = new Vector2(0f, newGap + lineLength / 2f);
        bottomRect.anchoredPosition = new Vector2(0f, -(newGap + lineLength / 2f));
        leftRect.anchoredPosition = new Vector2(-(newGap + lineLength / 2f), 0f);
        rightRect.anchoredPosition = new Vector2(newGap + lineLength / 2f, 0f);
    }

    private int lastCompletedCount;
    private int lastFailedCount;

    private void UpdateStreak(int completed, int failed)
    {
        if (streakText == null) return;

        if (completed > lastCompletedCount)
        {
            currentStreak += (completed - lastCompletedCount);
            streakDisplayTimer = 3f;
        }

        if (failed > lastFailedCount)
        {
            currentStreak = 0;
        }

        lastCompletedCount = completed;
        lastFailedCount = failed;

        if (currentStreak >= 2 && streakDisplayTimer > 0f)
        {
            streakDisplayTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(streakDisplayTimer / 0.5f);
            streakText.text = currentStreak + "x COMBO!";
            streakText.color = new Color(1f, 0.65f, 0.15f, alpha);

            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.05f;
            streakText.transform.localScale = Vector3.one * pulse;
        }
        else
        {
            streakText.text = string.Empty;
        }
    }

    private void UpdateUrgencyTimer()
    {
        if (timerText == null) return;

        OrderManager om = OrderManager.Instance;
        if (om == null || !om.HasActiveOrder)
        {
            timerText.text = string.Empty;
            if (timerPanelImage != null) timerPanelImage.enabled = false;
            return;
        }

        if (timerPanelImage != null) timerPanelImage.enabled = true;

        float remaining = om.RemainingTime;
        int seconds = Mathf.CeilToInt(remaining);
        timerText.text = "ZAMOWIENIE " + seconds.ToString("00") + "s";

        if (remaining < 10f)
        {

            float flash = Mathf.Sin(Time.time * 8f) * 0.5f + 0.5f;
            timerText.color = Color.Lerp(RedAccent, new Color(1f, 0.5f, 0.3f, 1f), flash);
            if (timerPanelImage != null)
            {
                timerPanelImage.color = Color.Lerp(
                    PanelDark,
                    new Color(0.3f, 0.05f, 0.05f, 0.85f),
                    flash * 0.4f);
            }
        }
        else if (remaining < 20f)
        {
            timerText.color = AccentGold;
            if (timerPanelImage != null) timerPanelImage.color = PanelDark;
        }
        else
        {
            timerText.color = new Color(0.65f, 0.88f, 0.65f, 0.85f);
            if (timerPanelImage != null) timerPanelImage.color = PanelDark;
        }
    }

    private void UpdateControlHintText()
    {
        if (shopHintText == null)
        {
            return;
        }

        shopHintText.text = MainMenuUI.IsSoloMode
            ? "B: Sklep  |  TAB: Gracze"
            : "B: Sklep  |  TAB: Gracze  |  F1: Lobby";
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
        string heldItem = playerInteraction != null ? Truncate(playerInteraction.GetHeldItemSummary(), 28) : "brak danych";

        displayedBalance = Mathf.Lerp(displayedBalance, balance, Time.deltaTime * 6f);
        balanceText.text = displayedBalance.ToString("F0") + " zl";
        heldItemText.text = heldItem;
        sessionText.text = completed + " OK  /  " + failed + " FAIL";
        UpdateControlHintText();

        UpdateStreak(completed, failed);

        UpdateUrgencyTimer();

        bool shopOpen = cachedShopUI != null && cachedShopUI.IsShopOpen;
        MainMenuUI mainMenu = FindFirstObjectByType<MainMenuUI>();
        bool menuOpen = mainMenu != null && mainMenu.IsMenuOpen;
        bool pauseOpen = PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused;
        bool settingsOpen = SettingsMenuUI.Instance != null && SettingsMenuUI.Instance.IsOpen;
        bool promptBlocked = shopOpen || menuOpen || pauseOpen || settingsOpen;

        if (playerInteraction != null && !promptBlocked)
        {
            promptText.text = playerInteraction.CurrentPrompt;
            string feedback = playerInteraction.FeedbackMessage;
            if (!string.IsNullOrWhiteSpace(feedback))
            {
                ShowFeedbackToast(feedback);
            }
            if (promptBackground != null)
            {
                promptBackground.enabled = !string.IsNullOrWhiteSpace(playerInteraction.CurrentPrompt);
            }
        }
        else
        {
            promptText.text = string.Empty;
            if (promptBackground != null)
            {
                promptBackground.enabled = false;
            }
        }

        bool hasTarget = playerInteraction != null
            && !string.IsNullOrWhiteSpace(playerInteraction.CurrentPrompt)
            && !shopOpen;
        UpdateCrosshair(hasTarget, shopOpen);

        UpdateUpgradeStatus();

        float currentBalance = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentBalance : 0f;
        if (currentBalance > lastKnownBalance + 0.01f)
        {
            float earned = currentBalance - lastKnownBalance;
            ShowFloatingMoney(earned);
        }
        lastKnownBalance = currentBalance;

        if (balancePulseTimer > 0f)
        {
            balancePulseTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(balancePulseTimer / 0.8f);

            balanceText.color = Color.Lerp(AccentGold, GreenAccent, t);
        }
    }

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

    private void ShowFeedbackToast(string message)
    {
        if (toastPanel == null || feedbackText == null || message == lastToastMessage)
        {
            return;
        }

        lastToastMessage = message;
        toastTimer = 2.25f;
        toastPanel.gameObject.SetActive(true);

        toastAccentColor = GetToastColor(message);

        if (toastAccentImage != null)
        {
            toastAccentImage.color = toastAccentColor;
        }

        feedbackText.text = message;
        feedbackText.color = TextPrimary;

        RectTransform rect = toastPanel;
        rect.anchoredPosition = new Vector2(0f, 92f);
        rect.localScale = Vector3.one * 0.96f;
    }

    private Color GetToastColor(string message)
    {
        string lower = message.ToLowerInvariant();
        if (lower.Contains("zly") || lower.Contains("brak") || lower.Contains("nie ") ||
            lower.Contains("najpierw") || lower.Contains("masz juz") || lower.Contains("potrzebujesz") ||
            lower.Contains("klient czeka") || lower.Contains("taca"))
        {
            return new Color(0.85f, 0.32f, 0.28f, 0.95f);
        }

        if (lower.Contains("zamowienie zrealizowane") || lower.Contains("nagroda") || lower.Contains("zakupiono"))
        {
            return new Color(0.30f, 0.72f, 0.42f, 0.95f);
        }

        if (lower.Contains("zawiniety") || lower.Contains("odebrano") || lower.Contains("pobrano") ||
            lower.Contains("dodano") || lower.Contains("polozono"))
        {
            return new Color(0.78f, 0.62f, 0.28f, 0.95f);
        }

        return new Color(0.42f, 0.48f, 0.56f, 0.92f);
    }

    private void UpdateToastAnimation()
    {
        if (toastPanel == null || toastCanvasGroup == null)
        {
            return;
        }

        if (toastTimer > 0f)
        {
            toastTimer -= Time.deltaTime;
        }
        else if (toastTimer <= 0f && !string.IsNullOrEmpty(lastToastMessage))
        {
            lastToastMessage = string.Empty;
        }

        float target = toastTimer > 0f ? 1f : 0f;
        toastCanvasGroup.alpha = Mathf.MoveTowards(toastCanvasGroup.alpha, target, Time.deltaTime * 8f);

        float eased = Mathf.SmoothStep(0f, 1f, toastCanvasGroup.alpha);
        toastPanel.anchoredPosition = new Vector2(0f, Mathf.Lerp(86f, 98f, eased));
        toastPanel.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, eased);

        if (toastAccentImage != null)
        {
            toastAccentImage.color = new Color(
                toastAccentColor.r,
                toastAccentColor.g,
                toastAccentColor.b,
                toastAccentColor.a);
        }

        if (toastCanvasGroup.alpha <= 0.01f && toastTimer <= 0f && toastPanel.gameObject.activeSelf)
        {
            toastPanel.gameObject.SetActive(false);
        }
    }

    private void UpdateFloatingMoney()
    {
        if (floatingMoneyText == null || floatingMoneyTimer <= 0f)
        {
            return;
        }

        floatingMoneyTimer -= Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(floatingMoneyTimer / 1.6f);

        RectTransform rect = floatingMoneyText.GetComponent<RectTransform>();
        if (rect != null)
        {
            float yOffset = floatingMoneyStartY + progress * 60f;
            rect.anchoredPosition = new Vector2(0f, yOffset);
        }

        float alpha = progress < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.5f) * 2f);
        float scale = 1f + Mathf.Sin(progress * Mathf.PI) * 0.15f;
        floatingMoneyText.color = new Color(0.22f, 0.92f, 0.42f, alpha);
        floatingMoneyText.fontSize = Mathf.RoundToInt(24f * scale);

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
        sb.Append("BONUSY:  ");
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

    private void AddTextShadow(Text sourceText)
    {
        if (sourceText == null) return;

        Shadow shadow = sourceText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    private void CreateSectionDivider(Transform parent, string objectName, Vector2 anchor)
    {
        CreatePanel(
            parent, objectName,
            anchor, anchor,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1f, 20f),
            DividerColor);
    }

    private RectTransform CreateRoundedPill(
        Transform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size,
        Color backgroundColor)
    {

        return CreatePanel(parent, objectName, anchorMin, anchorMax, pivot, anchoredPosition, size, backgroundColor);
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
        image.raycastTarget = false;

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
            parent, objectName, font, fontSize, alignment,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            anchoredPosition, size, fontStyle, color);
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
        text.raycastTarget = false;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        return text;
    }
}
