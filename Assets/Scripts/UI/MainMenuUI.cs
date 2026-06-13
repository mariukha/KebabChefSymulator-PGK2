using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// \file MainMenuUI.cs
/// \brief Menu główne gry z wyborem trybu solo lub multiplayer.
/// \details Tworzy programowo pełny interfejs menu głównego, zawierający przyciski
/// do gry solo, multiplayer, ustawień i wyjścia z gry. Menu zatrzymuje czas gry,
/// odblokowuje kursor myszy i animuje przejścia za pomocą CanvasGroup.
/// Obsługuje również przejście do lobby multiplayer i konfigurację zapisu gry.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    /// <summary>
    /// Canvas menu głównego do wyświetlania elementów interfejsu.
    /// Renderowany w trybie Screen Space Overlay z najwyższym priorytetem (200).
    /// </summary>
    private Canvas menuCanvas;

    /// <summary>
    /// Grupa Canvas kontrolująca przezroczystość i interaktywność całego menu.
    /// Używana do animacji pojawiania się i znikania menu.
    /// </summary>
    private CanvasGroup canvasGroup;

    /// <summary>
    /// Komponent tekstu tytułu gry ("KEBAB CHEF").
    /// Wyświetlany w kolorze złotym na górze panelu menu.
    /// </summary>
    private Text titleText;

    /// <summary>
    /// Flaga określająca, czy menu główne jest aktualnie widoczne.
    /// </summary>
    private bool isVisible = true;

    /// <summary>
    /// Postęp animacji przejścia menu (0.0 = ukryte, 1.0 = w pełni widoczne).
    /// Animowany płynnie w kierunku wartości docelowej.
    /// </summary>
    private float animationProgress = 1f;

    /// <summary>
    /// Buforowana czcionka używana do renderowania tekstu w elementach menu.
    /// </summary>
    private Font cachedFont;

    /// <summary>
    /// Szybkość animacji przejścia menu (jednostek na sekundę w czasie nieskalowanym).
    /// </summary>
    private const float AnimSpeed = 3.5f;

    /// <summary>
    /// Określa, czy gra jest w trybie solo.
    /// Ustawiane przy wyborze trybu gry z menu głównego.
    /// </summary>
    public static bool IsSoloMode { get; private set; } = false;

    /// <summary>
    /// Kolor ciemnego tła menu głównego (półprzezroczysty).
    /// </summary>
    private static readonly Color BgDark = new Color(0.008f, 0.01f, 0.014f, 0.58f);

    /// <summary>
    /// Kolor panelu centralnego menu (ciemny, półprzezroczysty).
    /// </summary>
    private static readonly Color PanelBg = new Color(0.014f, 0.016f, 0.019f, 0.82f);

    /// <summary>
    /// Kolor złotego akcentu używany w tytule i elementach dekoracyjnych.
    /// </summary>
    private static readonly Color AccentGold = new Color(0.86f, 0.68f, 0.28f);

    /// <summary>
    /// Podstawowy kolor tekstu interfejsu (jasny, niemal biały).
    /// </summary>
    private static readonly Color TextPrimary = new Color(0.91f, 0.92f, 0.94f);

    /// <summary>
    /// Kolor tła przycisku "Gra Solo" (ciemnozielony).
    /// </summary>
    private static readonly Color BtnPlayBg = new Color(0.045f, 0.30f, 0.14f);

    /// <summary>
    /// Kolor podświetlenia przycisku "Gra Solo" przy najechaniu kursorem.
    /// </summary>
    private static readonly Color BtnPlayHover = new Color(0.07f, 0.42f, 0.19f);

    /// <summary>
    /// Kolor tła przycisku "Multiplayer" (ciemnoniebieski).
    /// </summary>
    private static readonly Color BtnMultiBg = new Color(0.055f, 0.17f, 0.36f);

    /// <summary>
    /// Kolor podświetlenia przycisku "Multiplayer" przy najechaniu kursorem.
    /// </summary>
    private static readonly Color BtnMultiHover = new Color(0.08f, 0.24f, 0.50f);

    /// <summary>
    /// Kolor tła przycisku "Ustawienia" (ciemnoszary).
    /// </summary>
    private static readonly Color BtnSettingsBg = new Color(0.075f, 0.085f, 0.10f);

    /// <summary>
    /// Kolor podświetlenia przycisku "Ustawienia" przy najechaniu kursorem.
    /// </summary>
    private static readonly Color BtnSettingsHover = new Color(0.12f, 0.135f, 0.16f);

    /// <summary>
    /// Kolor tła przycisku "Wyjdź" (ciemnoczerwony).
    /// </summary>
    private static readonly Color BtnQuitBg = new Color(0.15f, 0.035f, 0.035f);

    /// <summary>
    /// Kolor podświetlenia przycisku "Wyjdź" przy najechaniu kursorem.
    /// </summary>
    private static readonly Color BtnQuitHover = new Color(0.27f, 0.06f, 0.055f);

    /// <summary>
    /// Kolor poziomej linii dekoracyjnej (separator) w menu głównym.
    /// </summary>
    private static readonly Color DividerColor = new Color(0.86f, 0.68f, 0.28f, 0.24f);

    /// <summary>
    /// Zwraca informację, czy menu główne jest aktualnie otwarte i widoczne.
    /// </summary>
    public bool IsMenuOpen => isVisible;

    /// <summary>
    /// Inicjalizuje czcionkę, tworzy interfejs użytkownika i wyświetla menu.
    /// </summary>
    private void Start()
    {
        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        CreateUI();
        Show();
    }

    /// <summary>
    /// Aktualizuje animację przejścia menu w każdej klatce.
    /// Kontroluje przezroczystość, interaktywność oraz stan kursora i czasu gry.
    /// Gdy menu jest widoczne, czas gry jest zatrzymany, a kursor odblokowany.
    /// </summary>
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

    /// <summary>
    /// Wyświetla menu główne, zatrzymując czas gry i odblokowując kursor.
    /// </summary>
    public void Show()
    {
        isVisible = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Ukrywa menu główne, wznawiając czas gry i blokując kursor.
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Obsługuje kliknięcie przycisku "Gra Solo".
    /// Ustawia tryb solo, konfiguruje slot zapisu, ukrywa menu i lobby,
    /// a następnie uruchamia hosta sieciowego na adresie lokalnym.
    /// </summary>
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

    /// <summary>
    /// Obsługuje kliknięcie przycisku "Multiplayer".
    /// Ustawia tryb multiplayer, konfiguruje slot zapisu, ukrywa menu główne
    /// i wyświetla interfejs lobby do wyboru serwera.
    /// </summary>
    private void OnMultiplayerClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        IsSoloMode = false;
        if (SaveManager.Instance != null) SaveManager.Instance.UseSaveSlot(false);
        Hide();
        LobbyUI lobby = FindFirstObjectByType<LobbyUI>();
        if (lobby != null) lobby.ShowLobby();
    }

    /// <summary>
    /// Obsługuje kliknięcie przycisku "Wyjdź".
    /// W edytorze Unity zatrzymuje tryb odtwarzania, w buildzie zamyka aplikację.
    /// </summary>
    private void OnQuitClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Obsługuje kliknięcie przycisku "Ustawienia".
    /// Otwiera panel ustawień gry za pomocą SettingsMenuUI.
    /// </summary>
    private void OnSettingsClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        SettingsMenuUI.EnsureInstance().Show();
    }

    /// <summary>
    /// Tworzy programowo cały interfejs użytkownika menu głównego.
    /// Buduje Canvas, tło, panel centralny, tytuł, separator oraz przyciski
    /// z odpowiednimi kolorami i zdarzeniami kliknięcia.
    /// </summary>
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

    /// <summary>
    /// Tworzy pojedynczy przycisk menu z efektem hover i konfigurowanymi kolorami.
    /// </summary>
    /// <param name="parent">Transform rodzica, do którego przycisk zostanie dołączony.</param>
    /// <param name="label">Tekst etykiety wyświetlany na przycisku.</param>
    /// <param name="bgColor">Kolor tła przycisku w stanie normalnym.</param>
    /// <param name="hoverColor">Kolor tła przycisku przy najechaniu kursorem.</param>
    /// <param name="yPos">Pozycja Y przycisku względem kotwicy (ujemne = w dół).</param>
    /// <param name="width">Szerokość przycisku w pikselach.</param>
    /// <param name="height">Wysokość przycisku w pikselach.</param>
    /// <param name="onClick">Akcja wywoływana po kliknięciu przycisku.</param>
    /// <param name="isPrimary">Czy przycisk jest główny (wpływa na rozmiar czcionki i efekt hover).</param>
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

    /// <summary>
    /// Tworzy pełnoekranowy prostokąt (Image) pokrywający cały obszar rodzica.
    /// Używany do tworzenia tła i nakładek.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="name">Nazwa tworzonego obiektu.</param>
    /// <param name="color">Kolor wypełnienia prostokąta.</param>
    /// <returns>Utworzony komponent Image.</returns>
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

    /// <summary>
    /// Tworzy zakotwiczony panel z określoną pozycją, rozmiarem i kolorem.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="name">Nazwa tworzonego obiektu.</param>
    /// <param name="anchor">Punkt kotwiczenia panelu (np. środek ekranu).</param>
    /// <param name="pos">Pozycja panelu względem kotwicy.</param>
    /// <param name="size">Rozmiar panelu (szerokość, wysokość).</param>
    /// <param name="color">Kolor tła panelu.</param>
    /// <returns>Utworzony obiekt panelu.</returns>
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

    /// <summary>
    /// Tworzy prosty panel z komponentem Image i podanym kolorem.
    /// </summary>
    /// <param name="parent">Transform rodzica.</param>
    /// <param name="name">Nazwa tworzonego obiektu.</param>
    /// <param name="color">Kolor tła panelu.</param>
    /// <returns>Komponent Image utworzonego panelu.</returns>
    private Image MakePanel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>
    /// Tworzy element tekstowy z określonymi parametrami stylu, rozmiaru i koloru.
    /// Dodaje cień dla lepszej czytelności na ciemnym tle.
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

    /// <summary>
    /// Ustawia pozycję i rozmiar RectTransform komponentu z domyślnym pivotem (0.5, 0.5).
    /// </summary>
    /// <param name="c">Komponent, którego RectTransform ma być skonfigurowany.</param>
    /// <param name="anchor">Punkt kotwiczenia.</param>
    /// <param name="pos">Pozycja względem kotwicy.</param>
    /// <param name="size">Rozmiar elementu (szerokość, wysokość).</param>
    private void SetRect(Component c, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        SetRect(c, anchor, pos, size, new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Ustawia pozycję, rozmiar i pivot RectTransform komponentu.
    /// </summary>
    /// <param name="c">Komponent, którego RectTransform ma być skonfigurowany.</param>
    /// <param name="anchor">Punkt kotwiczenia (min i max ustawiane na tę samą wartość).</param>
    /// <param name="pos">Pozycja względem kotwicy.</param>
    /// <param name="size">Rozmiar elementu (szerokość, wysokość).</param>
    /// <param name="pivot">Punkt obrotu elementu.</param>
    private void SetRect(Component c, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        RectTransform r = c.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = pivot;
        r.anchoredPosition = pos; r.sizeDelta = size;
    }
}

/// <summary>
/// \brief Komponent obsługujący płynne skalowanie przycisków menu przy najechaniu kursorem.
/// \details Implementuje interfejsy IPointerEnterHandler i IPointerExitHandler,
/// aby reagować na zdarzenia wejścia i wyjścia kursora. Animuje skalę przycisku
/// za pomocą interpolacji liniowej (Lerp) niezależnie od Time.timeScale.
/// </summary>
public class MenuButtonHover : MonoBehaviour,
    UnityEngine.EventSystems.IPointerEnterHandler,
    UnityEngine.EventSystems.IPointerExitHandler
{
    /// <summary>
    /// Skala przycisku w stanie normalnym (bez najechania kursorem).
    /// </summary>
    public Vector3 normalScale = Vector3.one;

    /// <summary>
    /// Skala przycisku po najechaniu kursorem (lekkie powiększenie).
    /// </summary>
    public Vector3 hoverScale = new Vector3(1.03f, 1.03f, 1f);

    /// <summary>
    /// Docelowa skala, do której animacja zmierza w bieżącej klatce.
    /// </summary>
    private Vector3 targetScale;

    /// <summary>
    /// Buforowany RectTransform przycisku używany do animacji skalowania.
    /// </summary>
    private RectTransform rect;

    /// <summary>
    /// Inicjalizuje komponent, pobierając RectTransform i ustawiając domyślną skalę docelową.
    /// </summary>
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        targetScale = normalScale;
    }

    /// <summary>
    /// Animuje skalę przycisku w kierunku docelowej skali za pomocą interpolacji liniowej.
    /// Używa Time.unscaledDeltaTime, aby animacja działała nawet gdy gra jest wstrzymana.
    /// </summary>
    private void Update()
    {
        if (rect != null)
            rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.unscaledDeltaTime * 10f);
    }

    /// <summary>
    /// Obsługuje zdarzenie wejścia kursora na przycisk — ustawia skalę docelową na powiększoną.
    /// </summary>
    /// <param name="e">Dane zdarzenia wskaźnika.</param>
    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) { targetScale = hoverScale; }

    /// <summary>
    /// Obsługuje zdarzenie wyjścia kursora z przycisku — ustawia skalę docelową na normalną.
    /// </summary>
    /// <param name="e">Dane zdarzenia wskaźnika.</param>
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) { targetScale = normalScale; }
}
