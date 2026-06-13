using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Professional pause menu. Esc to toggle.
/// Settings live in the dedicated SettingsMenuUI panel.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    private Canvas pauseCanvas;
    private CanvasGroup canvasGroup;
    private bool isOpen;
    private float animProgress;
    private bool wasTimeScaleZero;

    private MainMenuUI cachedMainMenuUI;
    private LobbyUI cachedLobbyUI;

    private Font cachedFont;

    private static readonly Color OverlayColor = new Color(0.008f, 0.01f, 0.018f, 0.88f);
    private static readonly Color PanelBg = new Color(0.03f, 0.035f, 0.055f, 0.97f);
    private static readonly Color PanelBorder = new Color(0.08f, 0.09f, 0.12f);
    private static readonly Color AccentGold = new Color(0.875f, 0.725f, 0.32f);
    private static readonly Color TextPrimary = new Color(0.9f, 0.91f, 0.93f);
    private static readonly Color BtnResumeBg = new Color(0.06f, 0.36f, 0.17f);
    private static readonly Color BtnResumeHover = new Color(0.08f, 0.48f, 0.23f);
    private static readonly Color BtnSettingsBg = new Color(0.055f, 0.12f, 0.22f);
    private static readonly Color BtnSettingsHover = new Color(0.08f, 0.18f, 0.32f);
    private static readonly Color BtnQuitBg = new Color(0.44f, 0.075f, 0.065f);
    private static readonly Color BtnQuitHover = new Color(0.58f, 0.11f, 0.095f);
    private static readonly Color DividerColor = new Color(0.12f, 0.13f, 0.17f);

    public bool IsPaused => isOpen;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void Start()
    {
        CreateUI();
        pauseCanvas.enabled = false;
    }

    private void Update()
    {
        if (cachedMainMenuUI == null) cachedMainMenuUI = FindFirstObjectByType<MainMenuUI>();
        if (cachedMainMenuUI != null && cachedMainMenuUI.IsMenuOpen) return;

        if (cachedLobbyUI == null) cachedLobbyUI = FindFirstObjectByType<LobbyUI>();
        if (cachedLobbyUI != null && cachedLobbyUI.IsLobbyOpen) return;

        if (SettingsMenuUI.Instance != null && SettingsMenuUI.Instance.IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen) Resume(); else Pause();
        }

        float target = isOpen ? 1f : 0f;
        animProgress = Mathf.MoveTowards(animProgress, target, Time.unscaledDeltaTime * 8f);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = animProgress;
            canvasGroup.blocksRaycasts = isOpen;
        }
        if (pauseCanvas != null)
            pauseCanvas.enabled = animProgress > 0.01f;
    }

    public void Pause()
    {
        isOpen = true;
        wasTimeScaleZero = Time.timeScale < 0.01f;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        isOpen = false;
        if (!wasTimeScaleZero) Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SaveSettings();
    }

    private void ReturnToMainMenu()
    {
        SaveSettings();

        SaveManager saveManager = SaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.SaveGame();
            saveManager.MarkSessionEnded();
        }

        isOpen = false;
        animProgress = 0f;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        if (pauseCanvas != null)
        {
            pauseCanvas.enabled = false;
        }

        if (cachedMainMenuUI == null)
        {
            cachedMainMenuUI = FindFirstObjectByType<MainMenuUI>();
        }
        if (cachedMainMenuUI == null)
        {
            cachedMainMenuUI = new GameObject("MainMenuUI").AddComponent<MainMenuUI>();
        }
        cachedMainMenuUI.Show();

        if (cachedLobbyUI == null)
        {
            cachedLobbyUI = FindFirstObjectByType<LobbyUI>();
        }
        if (cachedLobbyUI != null)
        {
            cachedLobbyUI.HideLobby();
        }

        if (NetworkSetup.Instance != null)
        {
            NetworkSetup.Instance.Disconnect();
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SaveSettings()
    {
        GameSettingsManager.EnsureInstance().Save();
    }

    private void OpenSettings()
    {
        SettingsMenuUI.EnsureInstance().Show();
    }

    private void CreateUI()
    {
        GameObject canvasObj = new GameObject("PauseCanvas");
        canvasObj.transform.SetParent(transform, false);
        pauseCanvas = canvasObj.AddComponent<Canvas>();
        pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        pauseCanvas.sortingOrder = 180;
        CanvasScaler sc = canvasObj.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        MakeFullRect(canvasObj.transform, "Overlay", OverlayColor);

        GameObject border = MakePanel(canvasObj.transform, "PanelBorder", PanelBorder,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(424f, 324f));
        GameObject panel = MakePanel(border.transform, "Panel", PanelBg,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 320f));

        Text title = Txt(panel.transform, "PAUZA", 28, FontStyle.Bold, AccentGold, TextAnchor.MiddleCenter);
        SetRect(title, 0.5f, 1f, 0f, -42f, 300f, 36f);

        Image goldAccent = MakeRectImage(panel.transform, "GoldAccent", AccentGold);
        SetRect(goldAccent, 0.5f, 1f, 0f, -78f, 180f, 2f);

        Image div1 = MakeRectImage(panel.transform, "Div1", DividerColor);
        SetRect(div1, 0.5f, 1f, 0f, -92f, 340f, 1f);

        float btnWidth = 300f;
        float btnHeight = 42f;

        MakeButton(panel.transform, "WZNOW", BtnResumeBg, BtnResumeHover,
            new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(btnWidth, btnHeight), Resume);
        MakeButton(panel.transform, "USTAWIENIA", BtnSettingsBg, BtnSettingsHover,
            new Vector2(0.5f, 1f), new Vector2(0f, -184f), new Vector2(btnWidth, btnHeight), OpenSettings);
        MakeButton(panel.transform, "WYJDZ", BtnQuitBg, BtnQuitHover,
            new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(btnWidth, btnHeight), ReturnToMainMenu);

        Text hint = Txt(panel.transform, "ESC - zamknij", 11, FontStyle.Normal, new Color(0.4f, 0.42f, 0.46f, 0.6f), TextAnchor.MiddleCenter);
        SetRect(hint, 0.5f, 0f, 0f, 16f, 200f, 18f);
    }

    private void MakeButton(Transform parent, string label, Color bg, Color hover, Vector2 anchor, Vector2 pos, Vector2 size,
        UnityEngine.Events.UnityAction action)
    {
        GameObject obj = new GameObject("Btn_" + label);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = bg;
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = bg; cb.highlightedColor = hover;
        cb.pressedColor = bg * 0.65f; cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            action.Invoke();
        });

        Text txt = Txt(obj.transform, label, 14, FontStyle.Bold, TextPrimary, TextAnchor.MiddleCenter);
        RectTransform tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
    }

    private Image MakeFullRect(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color; img.raycastTarget = true;
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        return img;
    }

    private GameObject MakePanel(Transform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
        return obj;
    }

    private Image MakeRectImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color; img.raycastTarget = false;
        return img;
    }

    private Text Txt(Transform parent, string content, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        GameObject obj = new GameObject("Txt");
        obj.transform.SetParent(parent, false);
        Text t = obj.AddComponent<Text>();
        t.font = cachedFont; t.fontSize = size; t.fontStyle = style;
        t.color = color; t.alignment = anchor; t.text = content;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private void SetRect(Component c, float anchorX, float anchorY, float posX, float posY, float w, float h)
    {
        RectTransform r = c.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(anchorX, anchorY);
        r.anchorMax = new Vector2(anchorX, anchorY);
        r.pivot = new Vector2(anchorX, anchorY);
        r.anchoredPosition = new Vector2(posX, posY);
        r.sizeDelta = new Vector2(w, h);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
