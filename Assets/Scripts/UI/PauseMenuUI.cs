using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// \file PauseMenuUI.cs
/// \brief Menu pauzy dostępne podczas rozgrywki.
/// \details Profesjonalne menu pauzy obsługiwane klawiszem Escape. Pozwala na wznowienie gry,
/// otwarcie ustawień lub powrót do menu głównego. Ustawienia znajdują się w dedykowanym
/// panelu SettingsMenuUI. Menu animuje się płynnie za pomocą CanvasGroup i blokuje
/// otwieranie, gdy aktywne jest menu główne, lobby lub panel ustawień.
/// Implementuje wzorzec Singleton.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singletona menu pauzy.
    /// Umożliwia globalny dostęp do stanu pauzy z dowolnego miejsca w grze.
    /// </summary>
    public static PauseMenuUI Instance { get; private set; }

    /// <summary>
    /// Canvas menu pauzy do wyświetlania elementów interfejsu.
    /// Renderowany w trybie Screen Space Overlay z porządkiem sortowania 180.
    /// </summary>
    private Canvas pauseCanvas;

    /// <summary>
    /// Grupa Canvas kontrolująca przezroczystość i interaktywność całego menu pauzy.
    /// </summary>
    private CanvasGroup canvasGroup;

    /// <summary>
    /// Flaga określająca, czy menu pauzy jest aktualnie otwarte.
    /// </summary>
    private bool isOpen;

    /// <summary>
    /// Postęp animacji przejścia menu pauzy (0.0 = ukryte, 1.0 = w pełni widoczne).
    /// </summary>
    private float animProgress;

    /// <summary>
    /// Zapamiętany stan Time.timeScale przed otwarciem pauzy.
    /// Jeśli czas już był zatrzymany (np. menu główne), nie jest wznawiany po zamknięciu pauzy.
    /// </summary>
    private bool wasTimeScaleZero;

    /// <summary>
    /// Buforowana referencja do interfejsu menu głównego.
    /// Używana do sprawdzania, czy menu główne jest otwarte (blokuje otwarcie pauzy).
    /// </summary>
    private MainMenuUI cachedMainMenuUI;

    /// <summary>
    /// Buforowana referencja do interfejsu lobby.
    /// Używana do sprawdzania, czy lobby jest otwarte (blokuje otwarcie pauzy).
    /// </summary>
    private LobbyUI cachedLobbyUI;

    /// <summary>
    /// Buforowana czcionka używana do renderowania tekstu w elementach menu pauzy.
    /// </summary>
    private Font cachedFont;

    /// <summary>
    /// Kolor nakładki przyciemniającej tło za menu pauzy.
    /// </summary>
    private static readonly Color OverlayColor = new Color(0.008f, 0.01f, 0.018f, 0.88f);

    /// <summary>
    /// Kolor tła głównego panelu menu pauzy.
    /// </summary>
    private static readonly Color PanelBg = new Color(0.03f, 0.035f, 0.055f, 0.97f);

    /// <summary>
    /// Kolor obramowania panelu menu pauzy.
    /// </summary>
    private static readonly Color PanelBorder = new Color(0.08f, 0.09f, 0.12f);

    /// <summary>
    /// Kolor złotego akcentu używany w tytule "PAUZA" i dekoracyjnej linii.
    /// </summary>
    private static readonly Color AccentGold = new Color(0.875f, 0.725f, 0.32f);

    /// <summary>
    /// Podstawowy kolor tekstu w menu pauzy (jasny, niemal biały).
    /// </summary>
    private static readonly Color TextPrimary = new Color(0.9f, 0.91f, 0.93f);

    /// <summary>
    /// Kolor tła przycisku "Wznów" (ciemnozielony).
    /// </summary>
    private static readonly Color BtnResumeBg = new Color(0.06f, 0.36f, 0.17f);

    /// <summary>
    /// Kolor podświetlenia przycisku "Wznów" przy najechaniu kursorem.
    /// </summary>
    private static readonly Color BtnResumeHover = new Color(0.08f, 0.48f, 0.23f);

    /// <summary>
    /// Kolor tła przycisku "Ustawienia" (ciemnoniebieski).
    /// </summary>
    private static readonly Color BtnSettingsBg = new Color(0.055f, 0.12f, 0.22f);

    /// <summary>
    /// Kolor podświetlenia przycisku "Ustawienia" przy najechaniu kursorem.
    /// </summary>
    private static readonly Color BtnSettingsHover = new Color(0.08f, 0.18f, 0.32f);

    /// <summary>
    /// Kolor tła przycisku "Wyjdź" (czerwony).
    /// </summary>
    private static readonly Color BtnQuitBg = new Color(0.44f, 0.075f, 0.065f);

    /// <summary>
    /// Kolor podświetlenia przycisku "Wyjdź" przy najechaniu kursorem.
    /// </summary>
    private static readonly Color BtnQuitHover = new Color(0.58f, 0.11f, 0.095f);

    /// <summary>
    /// Kolor linii separatora w menu pauzy.
    /// </summary>
    private static readonly Color DividerColor = new Color(0.12f, 0.13f, 0.17f);

    /// <summary>
    /// Zwraca informację, czy gra jest aktualnie wstrzymana (menu pauzy jest otwarte).
    /// </summary>
    public bool IsPaused => isOpen;

    /// <summary>
    /// Inicjalizuje Singleton menu pauzy i ładuje czcionkę.
    /// Jeśli instancja już istnieje, niszczy duplikat obiektu.
    /// </summary>
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    /// <summary>
    /// Tworzy interfejs użytkownika menu pauzy i wyłącza Canvas na starcie.
    /// </summary>
    private void Start()
    {
        CreateUI();
        pauseCanvas.enabled = false;
    }

    /// <summary>
    /// Obsługuje wejście klawiatury (Escape) i animuje przejście menu w każdej klatce.
    /// Blokuje otwarcie pauzy, gdy menu główne, lobby lub panel ustawień jest aktywny.
    /// </summary>
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

    /// <summary>
    /// Wstrzymuje grę — otwiera menu pauzy, zatrzymuje czas i odblokowuje kursor.
    /// Zapamiętuje poprzedni stan Time.timeScale, aby poprawnie go przywrócić przy wznowieniu.
    /// </summary>
    public void Pause()
    {
        isOpen = true;
        wasTimeScaleZero = Time.timeScale < 0.01f;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Wznawia grę — zamyka menu pauzy, przywraca czas i blokuje kursor.
    /// Zapisuje aktualne ustawienia gry przed zamknięciem.
    /// Nie przywraca Time.timeScale jeśli był już zerowy przed otwarciem pauzy.
    /// </summary>
    public void Resume()
    {
        isOpen = false;
        if (!wasTimeScaleZero) Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SaveSettings();
    }

    /// <summary>
    /// Obsługuje powrót do menu głównego z menu pauzy.
    /// Zapisuje ustawienia i stan gry, zamyka menu pauzy, rozłącza się z siecią,
    /// niszczy kamery graczy sieciowych, tworzy tymczasową kamerę rezerwową
    /// i wyświetla menu główne.
    /// </summary>
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

        {
            
            Camera[] allCams = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Camera c in allCams)
            {
                if (c != null && c.GetComponentInParent<NetworkPlayer>() != null)
                {
                    Destroy(c.gameObject);
                }
            }

            GameObject fallbackCam = new GameObject("FallbackCamera");
            Camera cam = fallbackCam.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            fallbackCam.transform.position = new Vector3(0f, 1.75f, -1.9f);

            if (FindFirstObjectByType<AudioListener>() == null)
            {
                fallbackCam.AddComponent<AudioListener>();
            }

            var urpData = fallbackCam.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            urpData.renderPostProcessing = true;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Zapisuje aktualne ustawienia gry za pomocą menedżera ustawień.
    /// </summary>
    private void SaveSettings()
    {
        GameSettingsManager.EnsureInstance().Save();
    }

    /// <summary>
    /// Otwiera panel ustawień gry (SettingsMenuUI).
    /// </summary>
    private void OpenSettings()
    {
        SettingsMenuUI.EnsureInstance().Show();
    }

    /// <summary>
    /// Tworzy programowo cały interfejs użytkownika menu pauzy.
    /// Buduje Canvas, nakładkę, panel z obramowaniem, tytuł, separatory
    /// oraz przyciski "Wznów", "Ustawienia" i "Wyjdź".
    /// </summary>
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

    /// <summary>
    /// Tworzy przycisk menu pauzy z efektem kliknięcia i dźwiękiem.
    /// </summary>
    /// <param name="parent">Transform rodzica, do którego przycisk zostanie dołączony.</param>
    /// <param name="label">Tekst etykiety wyświetlany na przycisku.</param>
    /// <param name="bg">Kolor tła przycisku w stanie normalnym.</param>
    /// <param name="hover">Kolor tła przycisku przy najechaniu kursorem.</param>
    /// <param name="anchor">Punkt kotwiczenia przycisku.</param>
    /// <param name="pos">Pozycja przycisku względem kotwicy.</param>
    /// <param name="size">Rozmiar przycisku (szerokość, wysokość).</param>
    /// <param name="action">Akcja wywoływana po kliknięciu przycisku.</param>
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

    /// <summary>
    /// Tworzy pełnoekranowy prostokąt (Image) pokrywający cały obszar rodzica.
    /// Używany jako nakładka przyciemniająca tło.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="name">Nazwa tworzonego obiektu.</param>
    /// <param name="color">Kolor wypełnienia.</param>
    /// <returns>Utworzony komponent Image.</returns>
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

    /// <summary>
    /// Tworzy panel z określoną kotwicą, pozycją, rozmiarem i kolorem.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="name">Nazwa tworzonego obiektu.</param>
    /// <param name="color">Kolor tła panelu.</param>
    /// <param name="anchor">Punkt kotwiczenia panelu.</param>
    /// <param name="pos">Pozycja panelu względem kotwicy.</param>
    /// <param name="size">Rozmiar panelu (szerokość, wysokość).</param>
    /// <returns>Utworzony obiekt panelu.</returns>
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

    /// <summary>
    /// Tworzy prosty prostokąt z komponentem Image bez obsługi raycastów.
    /// Używany do elementów dekoracyjnych takich jak linie i akcenty.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="name">Nazwa tworzonego obiektu.</param>
    /// <param name="color">Kolor prostokąta.</param>
    /// <returns>Utworzony komponent Image.</returns>
    private Image MakeRectImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color; img.raycastTarget = false;
        return img;
    }

    /// <summary>
    /// Tworzy element tekstowy z określonymi parametrami stylu.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="content">Treść tekstowa do wyświetlenia.</param>
    /// <param name="size">Rozmiar czcionki w pikselach.</param>
    /// <param name="style">Styl czcionki (normalny, pogrubiony itp.).</param>
    /// <param name="color">Kolor tekstu.</param>
    /// <param name="anchor">Wyrównanie tekstu wewnątrz prostokąta.</param>
    /// <returns>Utworzony komponent Text.</returns>
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

    /// <summary>
    /// Ustawia pozycję i rozmiar RectTransform komponentu za pomocą koordynatów skalarnych.
    /// </summary>
    /// <param name="c">Komponent, którego RectTransform ma być skonfigurowany.</param>
    /// <param name="anchorX">Współrzędna X kotwicy (min i max ustawiane na tę samą wartość).</param>
    /// <param name="anchorY">Współrzędna Y kotwicy.</param>
    /// <param name="posX">Pozycja X względem kotwicy.</param>
    /// <param name="posY">Pozycja Y względem kotwicy.</param>
    /// <param name="w">Szerokość elementu.</param>
    /// <param name="h">Wysokość elementu.</param>
    private void SetRect(Component c, float anchorX, float anchorY, float posX, float posY, float w, float h)
    {
        RectTransform r = c.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(anchorX, anchorY);
        r.anchorMax = new Vector2(anchorX, anchorY);
        r.pivot = new Vector2(anchorX, anchorY);
        r.anchoredPosition = new Vector2(posX, posY);
        r.sizeDelta = new Vector2(w, h);
    }

    /// <summary>
    /// Czyści referencję Singletona przy niszczeniu obiektu,
    /// zapobiegając odwoływaniu się do zniszczonej instancji.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
