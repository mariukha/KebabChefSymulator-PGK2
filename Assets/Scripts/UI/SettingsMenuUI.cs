using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// \file SettingsMenuUI.cs
/// \brief Panel ustawień gry z opcjami obrazu, dźwięku i sterowania.
/// \details Tworzy programowo kompletny panel ustawień zawierający sekcje:
/// - OBRAZ: rozdzielczość, tryb ekranu, V-Sync, limit FPS, jakość grafiki
/// - DŹWIĘK: głośność główna, muzyki i efektów (suwaki)
/// - STEROWANIE: czułość myszy (suwak)
/// Panel jest dostępny zarówno z menu głównego, jak i z menu pauzy.
/// Zamykany klawiszem Escape. Implementuje wzorzec Singleton z metodą EnsureInstance.
/// </summary>
public class SettingsMenuUI : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singletona panelu ustawień.
    /// Umożliwia globalny dostęp do wyświetlania i ukrywania panelu ustawień.
    /// </summary>
    public static SettingsMenuUI Instance { get; private set; }

    /// <summary>
    /// Canvas panelu ustawień renderowany w trybie Screen Space Overlay.
    /// Ma wysoki porządek sortowania (240), aby był wyświetlany ponad innymi elementami UI.
    /// </summary>
    private Canvas settingsCanvas;

    /// <summary>
    /// Grupa Canvas kontrolująca przezroczystość i interaktywność panelu ustawień.
    /// </summary>
    private CanvasGroup canvasGroup;

    /// <summary>
    /// Główny obiekt panelu ustawień (aktywowany/dezaktywowany przy otwieraniu/zamykaniu).
    /// </summary>
    private GameObject panel;

    /// <summary>
    /// Tekst wyświetlający aktualną wartość rozdzielczości ekranu.
    /// </summary>
    private Text resolutionValueText;

    /// <summary>
    /// Tekst wyświetlający aktualny tryb ekranu (pełny ekran, okno itp.).
    /// </summary>
    private Text windowModeValueText;

    /// <summary>
    /// Tekst wyświetlający aktualny stan synchronizacji pionowej (V-Sync).
    /// </summary>
    private Text vSyncValueText;

    /// <summary>
    /// Tekst wyświetlający aktualny limit klatek na sekundę (FPS).
    /// </summary>
    private Text fpsValueText;

    /// <summary>
    /// Tekst wyświetlający aktualny poziom jakości grafiki.
    /// </summary>
    private Text qualityValueText;

    /// <summary>
    /// Tekst wyświetlający aktualną wartość głośności głównej w procentach.
    /// </summary>
    private Text masterValueText;

    /// <summary>
    /// Tekst wyświetlający aktualną wartość głośności muzyki w procentach.
    /// </summary>
    private Text musicValueText;

    /// <summary>
    /// Tekst wyświetlający aktualną wartość głośności efektów dźwiękowych w procentach.
    /// </summary>
    private Text sfxValueText;

    /// <summary>
    /// Tekst wyświetlający aktualną wartość czułości myszy.
    /// </summary>
    private Text sensitivityValueText;

    /// <summary>
    /// Suwak do regulacji głośności głównej (zakres 0.0 - 1.0).
    /// </summary>
    private Slider masterSlider;

    /// <summary>
    /// Suwak do regulacji głośności muzyki (zakres 0.0 - 1.0).
    /// </summary>
    private Slider musicSlider;

    /// <summary>
    /// Suwak do regulacji głośności efektów dźwiękowych (zakres 0.0 - 1.0).
    /// </summary>
    private Slider sfxSlider;

    /// <summary>
    /// Suwak do regulacji czułości myszy (zakres 0.0 - 1.0, mapowany na 0.5 - 5.0).
    /// </summary>
    private Slider sensitivitySlider;

    /// <summary>
    /// Buforowana czcionka używana do renderowania tekstu w panelu ustawień.
    /// </summary>
    private Font cachedFont;

    /// <summary>
    /// Flaga określająca, czy panel ustawień jest aktualnie otwarty.
    /// </summary>
    private bool isOpen;

    /// <summary>
    /// Kolor nakładki przyciemniającej tło za panelem ustawień.
    /// </summary>
    private static readonly Color OverlayColor = new Color(0.006f, 0.008f, 0.012f, 0.82f);

    /// <summary>
    /// Kolor tła głównego panelu ustawień.
    /// </summary>
    private static readonly Color PanelColor = new Color(0.018f, 0.022f, 0.028f, 0.98f);

    /// <summary>
    /// Kolor tła nagłówka panelu ustawień.
    /// </summary>
    private static readonly Color HeaderColor = new Color(0.028f, 0.034f, 0.044f, 0.96f);

    /// <summary>
    /// Kolor obramowania panelu ustawień.
    /// </summary>
    private static readonly Color BorderColor = new Color(0.15f, 0.16f, 0.17f, 0.95f);

    /// <summary>
    /// Kolor złotego akcentu w nagłówku i etykietach sekcji.
    /// </summary>
    private static readonly Color AccentGold = new Color(0.86f, 0.68f, 0.28f);

    /// <summary>
    /// Podstawowy kolor tekstu (jasny, niemal biały).
    /// </summary>
    private static readonly Color TextPrimary = new Color(0.91f, 0.92f, 0.94f);

    /// <summary>
    /// Drugorzędny kolor tekstu (szary, używany dla wskazówek i nagłówków sekcji).
    /// </summary>
    private static readonly Color TextSecondary = new Color(0.58f, 0.62f, 0.69f);

    /// <summary>
    /// Kolor tekstu wartości ustawień (złotawy, wyróżniający aktualne wartości).
    /// </summary>
    private static readonly Color ValueColor = new Color(0.88f, 0.78f, 0.52f);

    /// <summary>
    /// Kolor tła przycisków nawigacji opcji (strzałki, przełączniki).
    /// </summary>
    private static readonly Color ButtonColor = new Color(0.055f, 0.12f, 0.22f);

    /// <summary>
    /// Kolor podświetlenia przycisków nawigacji przy najechaniu kursorem.
    /// </summary>
    private static readonly Color ButtonHoverColor = new Color(0.08f, 0.18f, 0.32f);

    /// <summary>
    /// Kolor tła przycisku "Zamknij" (czerwony).
    /// </summary>
    private static readonly Color CloseButtonColor = new Color(0.30f, 0.065f, 0.06f);

    /// <summary>
    /// Kolor podświetlenia przycisku "Zamknij" przy najechaniu kursorem.
    /// </summary>
    private static readonly Color CloseButtonHoverColor = new Color(0.46f, 0.10f, 0.085f);

    /// <summary>
    /// Kolor tła ścieżki suwaka (ciemnoszary).
    /// </summary>
    private static readonly Color SliderBg = new Color(0.06f, 0.07f, 0.08f);

    /// <summary>
    /// Kolor wypełnienia suwaka (zielony, wskazujący aktualną wartość).
    /// </summary>
    private static readonly Color SliderFill = new Color(0.08f, 0.38f, 0.18f);

    /// <summary>
    /// Kolor uchwytu suwaka (jasny, niemal biały).
    /// </summary>
    private static readonly Color SliderHandle = new Color(0.92f, 0.92f, 0.86f);

    /// <summary>
    /// Zwraca informację, czy panel ustawień jest aktualnie otwarty.
    /// </summary>
    public bool IsOpen => isOpen;

    /// <summary>
    /// Zapewnia istnienie instancji panelu ustawień.
    /// Jeśli instancja już istnieje, zwraca ją. W przeciwnym razie tworzy nowy obiekt
    /// z komponentem SettingsMenuUI.
    /// </summary>
    /// <returns>Instancja panelu ustawień (istniejąca lub nowo utworzona).</returns>
    public static SettingsMenuUI EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        return new GameObject("SettingsMenuUI").AddComponent<SettingsMenuUI>();
    }

    /// <summary>
    /// Inicjalizuje Singleton, ładuje czcionkę, tworzy interfejs i ukrywa panel.
    /// Jeśli instancja już istnieje, niszczy duplikat obiektu.
    /// </summary>
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

    /// <summary>
    /// Sprawdza w każdej klatce, czy naciśnięto klawisz Escape w celu zamknięcia panelu.
    /// </summary>
    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    /// <summary>
    /// Otwiera panel ustawień, stosuje aktualne ustawienia gry i odświeża wyświetlane wartości.
    /// Aktywuje Canvas i CanvasGroup oraz odblokowuje kursor.
    /// </summary>
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

    /// <summary>
    /// Zamyka panel ustawień i zapisuje aktualne ustawienia na dysk.
    /// </summary>
    public void Hide()
    {
        GameSettingsManager.EnsureInstance().Save();
        HideImmediate();
    }

    /// <summary>
    /// Natychmiast ukrywa panel ustawień bez zapisywania.
    /// Dezaktywuje Canvas, CanvasGroup i obiekt panelu.
    /// </summary>
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

    /// <summary>
    /// Tworzy programowo cały interfejs użytkownika panelu ustawień.
    /// Buduje Canvas, nakładkę, panel z obramowaniem, nagłówek oraz sekcje
    /// opcji obrazu, dźwięku i sterowania z odpowiednimi kontrolkami.
    /// </summary>
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

    /// <summary>
    /// Odświeża wszystkie wyświetlane wartości w panelu ustawień
    /// na podstawie aktualnych ustawień z GameSettingsManager.
    /// Aktualizuje teksty opcji oraz pozycje suwaków.
    /// </summary>
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

    /// <summary>
    /// Zmienia rozdzielczość ekranu w zadanym kierunku i odświeża wyświetlane wartości.
    /// </summary>
    /// <param name="direction">Kierunek zmiany: -1 = poprzednia, 1 = następna rozdzielczość.</param>
    private void ChangeResolution(int direction)
    {
        GameSettingsManager.EnsureInstance().CycleResolution(direction);
        RefreshValues();
    }

    /// <summary>
    /// Zmienia tryb ekranu w zadanym kierunku i odświeża wyświetlane wartości.
    /// </summary>
    /// <param name="direction">Kierunek zmiany: -1 = poprzedni, 1 = następny tryb.</param>
    private void ChangeWindowMode(int direction)
    {
        GameSettingsManager.EnsureInstance().CycleWindowMode(direction);
        RefreshValues();
    }

    /// <summary>
    /// Przełącza stan synchronizacji pionowej (V-Sync) i odświeża wyświetlane wartości.
    /// </summary>
    private void ToggleVSync()
    {
        GameSettingsManager.EnsureInstance().ToggleVSync();
        RefreshValues();
    }

    /// <summary>
    /// Zmienia limit klatek na sekundę w zadanym kierunku i odświeża wyświetlane wartości.
    /// </summary>
    /// <param name="direction">Kierunek zmiany: -1 = niższy, 1 = wyższy limit FPS.</param>
    private void ChangeFpsLimit(int direction)
    {
        GameSettingsManager.EnsureInstance().CycleFpsLimit(direction);
        RefreshValues();
    }

    /// <summary>
    /// Zmienia poziom jakości grafiki w zadanym kierunku i odświeża wyświetlane wartości.
    /// </summary>
    /// <param name="direction">Kierunek zmiany: -1 = niższy, 1 = wyższy poziom jakości.</param>
    private void ChangeQuality(int direction)
    {
        GameSettingsManager.EnsureInstance().CycleQuality(direction);
        RefreshValues();
    }

    /// <summary>
    /// Obsługuje zmianę wartości suwaka głośności głównej.
    /// Przekazuje nową wartość do menedżera ustawień i aktualizuje wyświetlany tekst procentowy.
    /// </summary>
    /// <param name="value">Nowa wartość suwaka (0.0 - 1.0).</param>
    private void OnMasterChanged(float value)
    {
        GameSettingsManager.EnsureInstance().SetMasterVolume(value);
        if (masterValueText != null) masterValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    /// <summary>
    /// Obsługuje zmianę wartości suwaka głośności muzyki.
    /// Przekazuje nową wartość do menedżera ustawień i aktualizuje wyświetlany tekst procentowy.
    /// </summary>
    /// <param name="value">Nowa wartość suwaka (0.0 - 1.0).</param>
    private void OnMusicChanged(float value)
    {
        GameSettingsManager.EnsureInstance().SetMusicVolume(value);
        if (musicValueText != null) musicValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    /// <summary>
    /// Obsługuje zmianę wartości suwaka głośności efektów dźwiękowych.
    /// Przekazuje nową wartość do menedżera ustawień i aktualizuje wyświetlany tekst procentowy.
    /// </summary>
    /// <param name="value">Nowa wartość suwaka (0.0 - 1.0).</param>
    private void OnSfxChanged(float value)
    {
        GameSettingsManager.EnsureInstance().SetSFXVolume(value);
        if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    /// <summary>
    /// Obsługuje zmianę wartości suwaka czułości myszy.
    /// Mapuje wartość suwaka (0.0 - 1.0) na rzeczywisty zakres czułości (0.5 - 5.0)
    /// i aktualizuje wyświetlany tekst.
    /// </summary>
    /// <param name="sliderValue">Nowa wartość suwaka (0.0 - 1.0).</param>
    private void OnSensitivityChanged(float sliderValue)
    {
        float sensitivity = Mathf.Lerp(0.5f, 5f, sliderValue);
        GameSettingsManager.EnsureInstance().SetMouseSensitivity(sensitivity);
        if (sensitivityValueText != null) sensitivityValueText.text = sensitivity.ToString("F1");
    }

    /// <summary>
    /// Ustawia wartość suwaka bez wywoływania zdarzenia OnValueChanged.
    /// Używane przy odświeżaniu wartości z ustawień, aby uniknąć cyklicznych wywołań.
    /// </summary>
    /// <param name="slider">Suwak do aktualizacji.</param>
    /// <param name="value">Nowa wartość suwaka (0.0 - 1.0).</param>
    private void SetSliderValue(Slider slider, float value)
    {
        if (slider == null)
        {
            return;
        }

        slider.SetValueWithoutNotify(value);
    }

    /// <summary>
    /// Tworzy etykietę sekcji (np. "OBRAZ", "DŹWIĘK") w panelu ustawień.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="label">Tekst etykiety sekcji.</param>
    /// <param name="y">Pozycja Y etykiety względem góry panelu.</param>
    private void MakeSectionLabel(Transform parent, string label, float y)
    {
        Text text = MakeText(parent, label, 12, FontStyle.Bold, TextSecondary, TextAnchor.MiddleLeft);
        SetRect(text, new Vector2(0f, 1f), new Vector2(28f, y), new Vector2(180f, 22f), new Vector2(0f, 0.5f));
    }

    /// <summary>
    /// Tworzy wiersz opcji z przyciskami nawigacyjnymi (strzałki lewo/prawo) i wartością tekstową.
    /// Używany dla opcji takich jak rozdzielczość, tryb ekranu, jakość.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="label">Etykieta opcji wyświetlana po lewej stronie.</param>
    /// <param name="y">Pozycja Y wiersza względem góry panelu.</param>
    /// <param name="previous">Akcja wywoływana po kliknięciu strzałki w lewo.</param>
    /// <param name="next">Akcja wywoływana po kliknięciu strzałki w prawo.</param>
    /// <returns>Komponent Text wyświetlający aktualną wartość opcji.</returns>
    private Text MakeOptionRow(Transform parent, string label, float y, UnityEngine.Events.UnityAction previous, UnityEngine.Events.UnityAction next)
    {
        MakeRowLabel(parent, label, y);
        MakeButton(parent, "<", ButtonColor, ButtonHoverColor, new Vector2(1f, 1f), new Vector2(-294f, y), new Vector2(40f, 32f), previous);

        Text valueText = MakeText(parent, "", 15, FontStyle.Bold, ValueColor, TextAnchor.MiddleCenter);
        SetRect(valueText, new Vector2(1f, 1f), new Vector2(-192f, y), new Vector2(156f, 32f), new Vector2(0.5f, 0.5f));

        MakeButton(parent, ">", ButtonColor, ButtonHoverColor, new Vector2(1f, 1f), new Vector2(-90f, y), new Vector2(40f, 32f), next);
        return valueText;
    }

    /// <summary>
    /// Tworzy wiersz opcji z przyciskiem przełączania (toggle) i wartością tekstową.
    /// Używany dla opcji takich jak V-Sync.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="label">Etykieta opcji wyświetlana po lewej stronie.</param>
    /// <param name="y">Pozycja Y wiersza względem góry panelu.</param>
    /// <param name="toggle">Akcja wywoływana po kliknięciu przycisku przełączania.</param>
    /// <returns>Komponent Text wyświetlający aktualną wartość opcji.</returns>
    private Text MakeToggleRow(Transform parent, string label, float y, UnityEngine.Events.UnityAction toggle)
    {
        MakeRowLabel(parent, label, y);
        Text valueText = MakeText(parent, "", 15, FontStyle.Bold, ValueColor, TextAnchor.MiddleCenter);
        SetRect(valueText, new Vector2(1f, 1f), new Vector2(-192f, y), new Vector2(156f, 32f), new Vector2(0.5f, 0.5f));
        MakeButton(parent, "ZMIEN", ButtonColor, ButtonHoverColor, new Vector2(1f, 1f), new Vector2(-90f, y), new Vector2(82f, 32f), toggle);
        return valueText;
    }

    /// <summary>
    /// Tworzy wiersz opcji z suwakiem i tekstem wyświetlającym wartość.
    /// Używany dla opcji głośności i czułości myszy.
    /// Suwak składa się z tła (track), wypełnienia (fill) i uchwytu (handle).
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="label">Etykieta opcji wyświetlana po lewej stronie.</param>
    /// <param name="y">Pozycja Y wiersza względem góry panelu.</param>
    /// <param name="callback">Funkcja zwrotna wywoływana przy zmianie wartości suwaka.</param>
    /// <param name="valueText">Parametr wyjściowy — komponent Text wyświetlający wartość.</param>
    /// <returns>Utworzony komponent Slider.</returns>
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

    /// <summary>
    /// Tworzy etykietę wiersza opcji wyświetlaną po lewej stronie.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="label">Tekst etykiety opcji.</param>
    /// <param name="y">Pozycja Y etykiety względem góry panelu.</param>
    private void MakeRowLabel(Transform parent, string label, float y)
    {
        Text text = MakeText(parent, label, 15, FontStyle.Normal, TextPrimary, TextAnchor.MiddleLeft);
        SetRect(text, new Vector2(0f, 1f), new Vector2(28f, y), new Vector2(260f, 28f), new Vector2(0f, 0.5f));
    }

    /// <summary>
    /// Tworzy przycisk z efektem kliknięcia i dźwiękiem w panelu ustawień.
    /// </summary>
    /// <param name="parent">Transform rodzica, do którego przycisk zostanie dołączony.</param>
    /// <param name="label">Tekst etykiety wyświetlany na przycisku.</param>
    /// <param name="color">Kolor tła przycisku w stanie normalnym.</param>
    /// <param name="hoverColor">Kolor tła przycisku przy najechaniu kursorem.</param>
    /// <param name="anchor">Punkt kotwiczenia przycisku.</param>
    /// <param name="position">Pozycja przycisku względem kotwicy.</param>
    /// <param name="size">Rozmiar przycisku (szerokość, wysokość).</param>
    /// <param name="action">Akcja wywoływana po kliknięciu przycisku.</param>
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

    /// <summary>
    /// Tworzy pełnoekranowy prostokąt (Image) pokrywający cały obszar rodzica.
    /// Używany jako nakładka przyciemniająca tło za panelem ustawień.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="name">Nazwa tworzonego obiektu.</param>
    /// <param name="color">Kolor wypełnienia.</param>
    /// <returns>Utworzony komponent Image.</returns>
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

    /// <summary>
    /// Tworzy panel z określoną kotwicą, pozycją, rozmiarem i kolorem.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="name">Nazwa tworzonego obiektu.</param>
    /// <param name="anchor">Punkt kotwiczenia panelu.</param>
    /// <param name="position">Pozycja panelu względem kotwicy.</param>
    /// <param name="size">Rozmiar panelu (szerokość, wysokość).</param>
    /// <param name="color">Kolor tła panelu.</param>
    /// <returns>Utworzony obiekt panelu.</returns>
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

    /// <summary>
    /// Tworzy prosty prostokąt z komponentem Image bez obsługi raycastów.
    /// Używany do elementów dekoracyjnych i elementów suwaka.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="name">Nazwa tworzonego obiektu.</param>
    /// <param name="color">Kolor prostokąta.</param>
    /// <returns>Utworzony komponent Image.</returns>
    private Image MakeRect(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    /// <summary>
    /// Tworzy element tekstowy z określonymi parametrami stylu, rozmiaru i koloru.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="content">Treść tekstowa do wyświetlenia.</param>
    /// <param name="size">Rozmiar czcionki w pikselach.</param>
    /// <param name="style">Styl czcionki (normalny, pogrubiony itp.).</param>
    /// <param name="color">Kolor tekstu.</param>
    /// <param name="anchor">Wyrównanie tekstu wewnątrz prostokąta.</param>
    /// <returns>Utworzony komponent Text.</returns>
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

    /// <summary>
    /// Ustawia pozycję, rozmiar i pivot RectTransform komponentu.
    /// </summary>
    /// <param name="component">Komponent, którego RectTransform ma być skonfigurowany.</param>
    /// <param name="anchor">Punkt kotwiczenia (min i max ustawiane na tę samą wartość).</param>
    /// <param name="position">Pozycja względem kotwicy.</param>
    /// <param name="size">Rozmiar elementu (szerokość, wysokość).</param>
    /// <param name="pivot">Punkt obrotu elementu.</param>
    private void SetRect(Component component, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
    {
        RectTransform rect = component.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    /// <summary>
    /// Czyści referencję Singletona przy niszczeniu obiektu,
    /// zapobiegając odwoływaniu się do zniszczonej instancji.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
