using UnityEngine;
using UnityEngine.UI;

public class KitchenHUD : MonoBehaviour
{
    private PlayerInteraction playerInteraction;

    private Canvas hudCanvas;
    private Text balanceText;
    private Text heldItemText;
    private Text sessionText;
    private Text promptText;
    private Text feedbackText;
    private Text crosshairText;
    private Image promptBackground;

    private void Awake()
    {
        CreateCanvas();
    }

    private void Update()
    {
        if (playerInteraction == null)
        {
            playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        }

        RefreshTexts();
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

        if (playerInteraction != null)
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
            feedbackText.text = string.Empty;
            if (promptBackground != null)
            {
                promptBackground.enabled = false;
            }
        }
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
