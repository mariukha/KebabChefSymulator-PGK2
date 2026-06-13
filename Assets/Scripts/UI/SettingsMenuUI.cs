using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuUI : MonoBehaviour
{
    public static SettingsMenuUI Instance { get; private set; }

    private Canvas settingsCanvas;
    private CanvasGroup canvasGroup;
    private GameObject panel;
    private Text resolutionValueText;
    private Text windowModeValueText;
    private Text vSyncValueText;
    private Text fpsValueText;
    private Text qualityValueText;
    private Text masterValueText;
    private Text musicValueText;
    private Text sfxValueText;
    private Text sensitivityValueText;
    private Slider masterSlider;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Slider sensitivitySlider;
    private Font cachedFont;
    private bool isOpen;

    private static readonly Color OverlayColor = new Color(0.006f, 0.008f, 0.012f, 0.82f);
    private static readonly Color PanelColor = new Color(0.018f, 0.022f, 0.028f, 0.98f);
    private static readonly Color HeaderColor = new Color(0.028f, 0.034f, 0.044f, 0.96f);
    private static readonly Color BorderColor = new Color(0.15f, 0.16f, 0.17f, 0.95f);
    private static readonly Color AccentGold = new Color(0.86f, 0.68f, 0.28f);
    private static readonly Color TextPrimary = new Color(0.91f, 0.92f, 0.94f);
    private static readonly Color TextSecondary = new Color(0.58f, 0.62f, 0.69f);
    private static readonly Color ValueColor = new Color(0.88f, 0.78f, 0.52f);
    private static readonly Color ButtonColor = new Color(0.055f, 0.12f, 0.22f);
    private static readonly Color ButtonHoverColor = new Color(0.08f, 0.18f, 0.32f);
    private static readonly Color CloseButtonColor = new Color(0.30f, 0.065f, 0.06f);
    private static readonly Color CloseButtonHoverColor = new Color(0.46f, 0.10f, 0.085f);
    private static readonly Color SliderBg = new Color(0.06f, 0.07f, 0.08f);
    private static readonly Color SliderFill = new Color(0.08f, 0.38f, 0.18f);
    private static readonly Color SliderHandle = new Color(0.92f, 0.92f, 0.86f);

    public bool IsOpen => isOpen;

    public static SettingsMenuUI EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        return new GameObject("SettingsMenuUI").AddComponent<SettingsMenuUI>();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        CreateUI();
        HideImmediate();
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    public void Show()
    {
        isOpen = true;
        GameSettingsManager.EnsureInstance().ApplyAll();
        RefreshValues();

        if (settingsCanvas != null) settingsCanvas.enabled = true;
        if (panel != null) panel.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        GameSettingsManager.EnsureInstance().Save();
        HideImmediate();
    }

    private void HideImmediate()
    {
        isOpen = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        if (panel != null) panel.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.enabled = false;
    }

    private void CreateUI()
    {
        GameObject canvasObject = new GameObject("SettingsCanvas");
        canvasObject.transform.SetParent(transform, false);

        settingsCanvas = canvasObject.AddComponent<Canvas>();
        settingsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        settingsCanvas.sortingOrder = 240;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        MakeFullRect(canvasObject.transform, "Overlay", OverlayColor);

        GameObject border = MakePanel(canvasObject.transform, "SettingsBorder",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 650f), BorderColor);

        panel = MakePanel(border.transform, "SettingsPanel",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(714f, 644f), PanelColor);

        GameObject header = MakePanel(panel.transform, "Header",
            new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(714f, 72f), HeaderColor);

        Text title = MakeText(header.transform, "USTAWIENIA", 27, FontStyle.Bold, AccentGold, TextAnchor.MiddleLeft);
        SetRect(title, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(360f, 44f), new Vector2(0f, 0.5f));

        Text hint = MakeText(header.transform, "ESC zamyka panel", 12, FontStyle.Normal, TextSecondary, TextAnchor.MiddleRight);
        SetRect(hint, new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(230f, 24f), new Vector2(1f, 0.5f));

        MakeSectionLabel(panel.transform, "OBRAZ", -100f);
        resolutionValueText = MakeOptionRow(panel.transform, "Rozdzielczosc", -134f,
            () => ChangeResolution(-1), () => ChangeResolution(1));
        windowModeValueText = MakeOptionRow(panel.transform, "Tryb ekranu", -176f,
            () => ChangeWindowMode(-1), () => ChangeWindowMode(1));
        vSyncValueText = MakeToggleRow(panel.transform, "V-Sync", -218f, ToggleVSync);
        fpsValueText = MakeOptionRow(panel.transform, "Limit FPS", -260f,
            () => ChangeFpsLimit(-1), () => ChangeFpsLimit(1));
        qualityValueText = MakeOptionRow(panel.transform, "Jakosc", -302f,
            () => ChangeQuality(-1), () => ChangeQuality(1));

        MakeSectionLabel(panel.transform, "DZWIEK", -362f);
        masterSlider = MakeSliderRow(panel.transform, "Glosnosc", -398f, OnMasterChanged, out masterValueText);
        musicSlider = MakeSliderRow(panel.transform, "Muzyka", -440f, OnMusicChanged, out musicValueText);
        sfxSlider = MakeSliderRow(panel.transform, "Efekty", -482f, OnSfxChanged, out sfxValueText);

        MakeSectionLabel(panel.transform, "STEROWANIE", -536f);
        sensitivitySlider = MakeSliderRow(panel.transform, "Czulosc myszy", -572f, OnSensitivityChanged, out sensitivityValueText);

        MakeButton(panel.transform, "ZAMKNIJ", CloseButtonColor, CloseButtonHoverColor,
            new Vector2(1f, 0f), new Vector2(-92f, 30f), new Vector2(128f, 36f), Hide);
    }

    private void RefreshValues()
    {
        GameSettingsManager settings = GameSettingsManager.EnsureInstance();
        if (resolutionValueText != null) resolutionValueText.text = settings.ResolutionLabel;
        if (windowModeValueText != null) windowModeValueText.text = settings.WindowModeLabel;
        if (vSyncValueText != null) vSyncValueText.text = settings.VSyncLabel;
        if (fpsValueText != null) fpsValueText.text = settings.FpsLimitLabel;
        if (qualityValueText != null) qualityValueText.text = settings.QualityLabel;

        SetSliderValue(masterSlider, settings.MasterVolume);
        SetSliderValue(musicSlider, settings.MusicVolume);
        SetSliderValue(sfxSlider, settings.SFXVolume);
        SetSliderValue(sensitivitySlider, Mathf.InverseLerp(0.5f, 5f, settings.MouseSensitivity));

        if (masterValueText != null) masterValueText.text = Mathf.RoundToInt(settings.MasterVolume * 100f) + "%";
        if (musicValueText != null) musicValueText.text = Mathf.RoundToInt(settings.MusicVolume * 100f) + "%";
        if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(settings.SFXVolume * 100f) + "%";
        if (sensitivityValueText != null) sensitivityValueText.text = settings.MouseSensitivity.ToString("F1");
    }

    private void ChangeResolution(int direction)
    {
        GameSettingsManager.EnsureInstance().CycleResolution(direction);
        RefreshValues();
    }

    private void ChangeWindowMode(int direction)
    {
        GameSettingsManager.EnsureInstance().CycleWindowMode(direction);
        RefreshValues();
    }

    private void ToggleVSync()
    {
        GameSettingsManager.EnsureInstance().ToggleVSync();
        RefreshValues();
    }

    private void ChangeFpsLimit(int direction)
    {
        GameSettingsManager.EnsureInstance().CycleFpsLimit(direction);
        RefreshValues();
    }

    private void ChangeQuality(int direction)
    {
        GameSettingsManager.EnsureInstance().CycleQuality(direction);
        RefreshValues();
    }

    private void OnMasterChanged(float value)
    {
        GameSettingsManager.EnsureInstance().SetMasterVolume(value);
        if (masterValueText != null) masterValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void OnMusicChanged(float value)
    {
        GameSettingsManager.EnsureInstance().SetMusicVolume(value);
        if (musicValueText != null) musicValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void OnSfxChanged(float value)
    {
        GameSettingsManager.EnsureInstance().SetSFXVolume(value);
        if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void OnSensitivityChanged(float sliderValue)
    {
        float sensitivity = Mathf.Lerp(0.5f, 5f, sliderValue);
        GameSettingsManager.EnsureInstance().SetMouseSensitivity(sensitivity);
        if (sensitivityValueText != null) sensitivityValueText.text = sensitivity.ToString("F1");
    }

    private void SetSliderValue(Slider slider, float value)
    {
        if (slider == null)
        {
            return;
        }

        slider.SetValueWithoutNotify(value);
    }

    private void MakeSectionLabel(Transform parent, string label, float y)
    {
        Text text = MakeText(parent, label, 12, FontStyle.Bold, TextSecondary, TextAnchor.MiddleLeft);
        SetRect(text, new Vector2(0f, 1f), new Vector2(28f, y), new Vector2(180f, 22f), new Vector2(0f, 0.5f));
    }

    private Text MakeOptionRow(Transform parent, string label, float y, UnityEngine.Events.UnityAction previous, UnityEngine.Events.UnityAction next)
    {
        MakeRowLabel(parent, label, y);
        MakeButton(parent, "<", ButtonColor, ButtonHoverColor, new Vector2(1f, 1f), new Vector2(-294f, y), new Vector2(40f, 32f), previous);

        Text valueText = MakeText(parent, "", 15, FontStyle.Bold, ValueColor, TextAnchor.MiddleCenter);
        SetRect(valueText, new Vector2(1f, 1f), new Vector2(-192f, y), new Vector2(156f, 32f), new Vector2(0.5f, 0.5f));

        MakeButton(parent, ">", ButtonColor, ButtonHoverColor, new Vector2(1f, 1f), new Vector2(-90f, y), new Vector2(40f, 32f), next);
        return valueText;
    }

    private Text MakeToggleRow(Transform parent, string label, float y, UnityEngine.Events.UnityAction toggle)
    {
        MakeRowLabel(parent, label, y);
        Text valueText = MakeText(parent, "", 15, FontStyle.Bold, ValueColor, TextAnchor.MiddleCenter);
        SetRect(valueText, new Vector2(1f, 1f), new Vector2(-192f, y), new Vector2(156f, 32f), new Vector2(0.5f, 0.5f));
        MakeButton(parent, "ZMIEN", ButtonColor, ButtonHoverColor, new Vector2(1f, 1f), new Vector2(-90f, y), new Vector2(82f, 32f), toggle);
        return valueText;
    }

    private Slider MakeSliderRow(Transform parent, string label, float y, UnityEngine.Events.UnityAction<float> callback, out Text valueText)
    {
        MakeRowLabel(parent, label, y);
        valueText = MakeText(parent, "", 14, FontStyle.Bold, ValueColor, TextAnchor.MiddleRight);
        SetRect(valueText, new Vector2(1f, 1f), new Vector2(-48f, y), new Vector2(70f, 26f), new Vector2(1f, 0.5f));

        GameObject sliderObject = new GameObject("Slider_" + label);
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(1f, 1f);
        sliderRect.anchorMax = new Vector2(1f, 1f);
        sliderRect.pivot = new Vector2(1f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(-130f, y);
        sliderRect.sizeDelta = new Vector2(270f, 28f);

        Image hitArea = sliderObject.AddComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0.001f);
        hitArea.raycastTarget = true;

        Image track = MakeRect(sliderObject.transform, "Track", SliderBg);
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0.40f);
        trackRect.anchorMax = new Vector2(1f, 0.60f);
        trackRect.offsetMin = Vector2.zero;
        trackRect.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.40f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.60f);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        Image fill = MakeRect(fillArea.transform, "Fill", SliderFill);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handleArea = new GameObject("HandleArea");
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(6f, 0f);
        handleAreaRect.offsetMax = new Vector2(-6f, 0f);

        Image handle = MakeRect(handleArea.transform, "Handle", SliderHandle);
        handle.raycastTarget = true;
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(12f, 24f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.onValueChanged.AddListener(callback);
        return slider;
    }

    private void MakeRowLabel(Transform parent, string label, float y)
    {
        Text text = MakeText(parent, label, 15, FontStyle.Normal, TextPrimary, TextAnchor.MiddleLeft);
        SetRect(text, new Vector2(0f, 1f), new Vector2(28f, y), new Vector2(260f, 28f), new Vector2(0f, 0.5f));
    }

    private void MakeButton(Transform parent, string label, Color color, Color hoverColor,
        Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = MakePanel(parent, "Btn_" + label, anchor, position, size, color);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = color * 0.75f;
        colors.selectedColor = color;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        button.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            action.Invoke();
        });

        Text text = MakeText(buttonObject.transform, label, 14, FontStyle.Bold, TextPrimary, TextAnchor.MiddleCenter);
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Image MakeFullRect(Transform parent, string name, Color color)
    {
        Image image = MakeRect(parent, name, color);
        image.raycastTarget = true;
        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return image;
    }

    private GameObject MakePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return obj;
    }

    private Image MakeRect(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text MakeText(Transform parent, string content, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        GameObject obj = new GameObject("Txt");
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.font = cachedFont;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;
        text.text = content;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void SetRect(Component component, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
    {
        RectTransform rect = component.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
