using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu for choosing solo or multiplayer mode.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private Canvas menuCanvas;
    private CanvasGroup canvasGroup;
    private Text titleText;

    private bool isVisible = true;
    private float animationProgress = 1f;

    private Font cachedFont;

    private const float AnimSpeed = 3.5f;

    public static bool IsSoloMode { get; private set; } = false;

    private static readonly Color BgDark = new Color(0.008f, 0.01f, 0.014f, 0.58f);
    private static readonly Color PanelBg = new Color(0.014f, 0.016f, 0.019f, 0.82f);
    private static readonly Color AccentGold = new Color(0.86f, 0.68f, 0.28f);
    private static readonly Color TextPrimary = new Color(0.91f, 0.92f, 0.94f);
    private static readonly Color BtnPlayBg = new Color(0.045f, 0.30f, 0.14f);
    private static readonly Color BtnPlayHover = new Color(0.07f, 0.42f, 0.19f);
    private static readonly Color BtnMultiBg = new Color(0.055f, 0.17f, 0.36f);
    private static readonly Color BtnMultiHover = new Color(0.08f, 0.24f, 0.50f);
    private static readonly Color BtnSettingsBg = new Color(0.075f, 0.085f, 0.10f);
    private static readonly Color BtnSettingsHover = new Color(0.12f, 0.135f, 0.16f);
    private static readonly Color BtnQuitBg = new Color(0.15f, 0.035f, 0.035f);
    private static readonly Color BtnQuitHover = new Color(0.27f, 0.06f, 0.055f);
    private static readonly Color DividerColor = new Color(0.86f, 0.68f, 0.28f, 0.24f);

    public bool IsMenuOpen => isVisible;

    private void Start()
    {
        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        CreateUI();
        Show();
    }

    private void Update()
    {
        float target = isVisible ? 1f : 0f;
        animationProgress = Mathf.MoveTowards(animationProgress, target, Time.unscaledDeltaTime * AnimSpeed);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = animationProgress;
            canvasGroup.blocksRaycasts = animationProgress > 0.1f;
        }
        if (menuCanvas != null)
            menuCanvas.enabled = animationProgress > 0.01f;

        if (animationProgress <= 0.01f && !isVisible) return;

        if (titleText != null)
        {
            titleText.color = AccentGold;
        }

        if (isVisible)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Show()
    {
        isVisible = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        isVisible = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnSoloClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        IsSoloMode = true;
        if (SaveManager.Instance != null) SaveManager.Instance.UseSaveSlot(true);
        Hide();
        LobbyUI lobby = FindFirstObjectByType<LobbyUI>();
        if (lobby != null) lobby.HideLobby();

        if (NetworkSetup.Instance != null)
        {
            NetworkSetup.Instance.StartHost("127.0.0.1");
        }
    }

    private void OnMultiplayerClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        IsSoloMode = false;
        if (SaveManager.Instance != null) SaveManager.Instance.UseSaveSlot(false);
        Hide();
        LobbyUI lobby = FindFirstObjectByType<LobbyUI>();
        if (lobby != null) lobby.ShowLobby();
    }

    private void OnQuitClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnSettingsClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        SettingsMenuUI.EnsureInstance().Show();
    }

    private void CreateUI()
    {
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        canvasObj.transform.SetParent(transform, false);
        menuCanvas = canvasObj.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 200;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();

        MakeFullRect(canvasObj.transform, "BG", BgDark);
        MakeFullRect(canvasObj.transform, "Vignette", new Color(0f, 0f, 0f, 0.30f));

        GameObject stage = MakeAnchoredPanel(canvasObj.transform, "MenuStage",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 520f), PanelBg);
        stage.name = "MenuStage";

        GameObject contentPanel = MakeAnchoredPanel(canvasObj.transform, "ContentPanel",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 430f), new Color(0, 0, 0, 0));

        titleText = MakeText(contentPanel.transform, "KEBAB CHEF", 58, FontStyle.Bold, AccentGold, TextAnchor.MiddleCenter);
        SetRect(titleText, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(460f, 72f));

        Image divider = MakePanel(contentPanel.transform, "Divider", DividerColor);
        SetRect(divider, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(220f, 2f));

        float btnY = -178f;
        float btnSpacing = 58f;

        CreateButton(contentPanel.transform, "GRA SOLO", BtnPlayBg, BtnPlayHover, btnY, 330f, 50f, OnSoloClicked, true);
        CreateButton(contentPanel.transform, "MULTIPLAYER", BtnMultiBg, BtnMultiHover, btnY - btnSpacing, 330f, 50f, OnMultiplayerClicked, true);
        CreateButton(contentPanel.transform, "USTAWIENIA", BtnSettingsBg, BtnSettingsHover, btnY - btnSpacing * 2f, 330f, 48f, OnSettingsClicked, true);
        CreateButton(contentPanel.transform, "WYJDZ", BtnQuitBg, BtnQuitHover, btnY - btnSpacing * 3f, 240f, 42f, OnQuitClicked, false);
    }

    private void CreateButton(Transform parent, string label, Color bgColor, Color hoverColor,
        float yPos, float width, float height, UnityEngine.Events.UnityAction onClick, bool isPrimary)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = bgColor;

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, yPos);
        rect.sizeDelta = new Vector2(width, height);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = hoverColor;
        cb.pressedColor = bgColor * 0.7f;
        cb.selectedColor = bgColor;
        cb.fadeDuration = 0.12f;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        MenuButtonHover hover = btnObj.AddComponent<MenuButtonHover>();
        hover.normalScale = Vector3.one;
        hover.hoverScale = isPrimary ? new Vector3(1.03f, 1.03f, 1f) : new Vector3(1.02f, 1.02f, 1f);

        int fontSize = isPrimary ? 16 : 13;
        Text txt = MakeText(btnObj.transform, label, fontSize, FontStyle.Bold, TextPrimary, TextAnchor.MiddleCenter);
        RectTransform tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(18f, 0f); tr.offsetMax = new Vector2(-18f, 0f);

    }

    private Image MakeFullRect(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        return img;
    }

    private GameObject MakeAnchoredPanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
        return obj;
    }

    private Image MakePanel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Text MakeText(Transform parent, string content, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        GameObject obj = new GameObject("Txt");
        obj.transform.SetParent(parent, false);
        Text t = obj.AddComponent<Text>();
        t.font = cachedFont;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = anchor;
        t.text = content;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
        shadow.effectDistance = new Vector2(1.2f, -1.2f);
        return t;
    }

    private void SetRect(Component c, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        SetRect(c, anchor, pos, size, new Vector2(0.5f, 0.5f));
    }

    private void SetRect(Component c, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        RectTransform r = c.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = pivot;
        r.anchoredPosition = pos; r.sizeDelta = size;
    }
}

/// <summary>Smooth hover scale for menu buttons.</summary>
public class MenuButtonHover : MonoBehaviour,
    UnityEngine.EventSystems.IPointerEnterHandler,
    UnityEngine.EventSystems.IPointerExitHandler
{
    public Vector3 normalScale = Vector3.one;
    public Vector3 hoverScale = new Vector3(1.03f, 1.03f, 1f);
    private Vector3 targetScale;
    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        targetScale = normalScale;
    }

    private void Update()
    {
        if (rect != null)
            rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.unscaledDeltaTime * 10f);
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) { targetScale = hoverScale; }
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) { targetScale = normalScale; }
}
