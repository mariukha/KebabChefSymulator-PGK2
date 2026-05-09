using UnityEngine;
using UnityEngine.UI;

public class KitchenOrderBoard : MonoBehaviour
{
    private Text headerText;
    private Text metaText;
    private Text ingredientsText;
    private Text footerText;
    private Image urgencyBar;

    public void Initialize()
    {
        CreateMonitorVisuals();
    }

    private void Update()
    {
        RefreshBoard();
    }

    private void CreateMonitorVisuals()
    {
        CreateBlock("MonitorBack", new Vector3(0f, 0f, 0f), new Vector3(3.5f, 2.05f, 0.16f), new Color(0.06f, 0.06f, 0.07f));
        CreateBlock("MonitorFrame", new Vector3(0f, 0f, 0.045f), new Vector3(3.28f, 1.84f, 0.05f), new Color(0.12f, 0.12f, 0.13f));
        CreateBlock("ScreenGlow", new Vector3(0f, 0f, 0.073f), new Vector3(3.04f, 1.6f, 0.01f), new Color(0.03f, 0.05f, 0.08f));
        CreateBlock("MonitorShadowLip", new Vector3(0f, -0.98f, 0.02f), new Vector3(2.1f, 0.06f, 0.09f), new Color(0.08f, 0.08f, 0.09f));
        CreateBlock("StatusLight", new Vector3(1.46f, -0.88f, 0.045f), new Vector3(0.08f, 0.08f, 0.03f), new Color(0.18f, 0.95f, 0.42f));

        GameObject canvasObject = new GameObject("MonitorCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = new Vector3(0f, 0f, 0.082f);
        canvasObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        canvasObject.transform.localScale = Vector3.one * 0.00245f;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1280f, 720f);

        canvasObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        CreatePanel(canvasObject.transform, "ScreenBackground", Vector2.zero, new Vector2(1280f, 720f), new Color(0.035f, 0.05f, 0.075f, 0.98f));
        CreatePanel(canvasObject.transform, "HeaderBand", new Vector2(0f, -250f), new Vector2(1280f, 110f), new Color(0.07f, 0.11f, 0.18f, 0.95f));
        CreatePanel(canvasObject.transform, "FooterBand", new Vector2(0f, 286f), new Vector2(1280f, 84f), new Color(0.06f, 0.09f, 0.14f, 0.95f));

        urgencyBar = CreateImage(
            canvasObject.transform,
            "UrgencyBar",
            new Vector2(0f, -304f),
            new Vector2(1280f, 26f),
            new Color(0.2f, 0.8f, 0.4f, 1f));

        headerText = CreateText(
            canvasObject.transform,
            "HeaderText",
            font,
            58,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            new Vector2(48f, -38f),
            new Vector2(1180f, 90f),
            new Color(0.93f, 0.97f, 1f));

        metaText = CreateText(
            canvasObject.transform,
            "MetaText",
            font,
            46,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            new Vector2(52f, -160f),
            new Vector2(1180f, 240f),
            new Color(0.96f, 0.97f, 0.99f));

        ingredientsText = CreateText(
            canvasObject.transform,
            "IngredientsText",
            font,
            38,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            new Vector2(60f, -360f),
            new Vector2(1160f, 250f),
            new Color(0.9f, 0.96f, 1f));

        footerText = CreateText(
            canvasObject.transform,
            "FooterText",
            font,
            30,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            new Vector2(50f, -654f),
            new Vector2(1180f, 56f),
            new Color(0.82f, 0.9f, 1f));

        RefreshBoard();
    }

    private void RefreshBoard()
    {
        if (headerText == null)
        {
            return;
        }

        if (OrderManager.Instance == null)
        {
            return;
        }

        string orderDesc = OrderManager.Instance.ActiveOrderDescription;
        if (string.IsNullOrEmpty(orderDesc))
        {
            headerText.text = "LIVE ORDERS";
            metaText.text = "No active order";
            ingredientsText.text = string.Empty;
            footerText.text = "Kitchen display waiting for next ticket";
            if (urgencyBar != null)
            {
                urgencyBar.color = new Color(0.2f, 0.75f, 0.4f, 1f);
            }
            return;
        }

        float timeRemaining = Mathf.CeilToInt(OrderManager.Instance.RemainingOrderTime);

        headerText.text = "LIVE ORDERS";
        
        Order order = OrderManager.Instance.ActiveOrder;
        if (order != null)
        {
            float reward = order.nagrodaPieniezna;
            metaText.text =
                "CLIENT   " + order.nazwaKlienta + "\n" +
                "DISH     " + order.nazwaZamowienia + "\n" +
                "TIME     " + timeRemaining + " s\n" +
                "REWARD   " + reward + " zl";

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine("INGREDIENTS");
            foreach (IngredientRequirement requirement in order.wymaganeSkladniki)
            {
                builder.AppendLine("• " + requirement.ToDisplayString());
            }
            ingredientsText.text = builder.ToString();
        }
        else
        {
            // Fallback for clients without full Order object
            metaText.text = "TIME     " + timeRemaining + " s\n";
            ingredientsText.text = orderDesc;
        }

        footerText.text =
            "Completed " + OrderManager.Instance.CompletedOrders +
            "   |   Failed " + OrderManager.Instance.FailedOrders;

        if (urgencyBar != null)
        {
            float fullTime = order != null ? order.czasNaRealizacje : 120f;
            urgencyBar.color = GetUrgencyColor(OrderManager.Instance.RemainingOrderTime, fullTime);
        }
    }

    private Color GetUrgencyColor(float remainingTime, float fullTime)
    {
        float ratio = fullTime <= 0.01f ? 1f : Mathf.Clamp01(remainingTime / fullTime);
        if (ratio > 0.5f)
        {
            return new Color(0.22f, 0.82f, 0.42f, 1f);
        }

        if (ratio > 0.25f)
        {
            return new Color(0.94f, 0.72f, 0.18f, 1f);
        }

        return new Color(0.92f, 0.24f, 0.2f, 1f);
    }

    private void CreateBlock(string objectName, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = objectName;
        block.transform.SetParent(transform, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = localScale;

        Renderer renderer = block.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        renderer.material = new Material(shader);
        renderer.material.color = color;
    }

    private void CreatePanel(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject panelObject = new GameObject(objectName);
        panelObject.transform.SetParent(parent, false);

        Image image = panelObject.AddComponent<Image>();
        image.color = color;

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private Image CreateImage(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = color;

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        return image;
    }

    private Text CreateText(
        Transform parent,
        string objectName,
        Font font,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        return text;
    }
}
