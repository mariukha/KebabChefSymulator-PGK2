/// \file PlayerListUI.cs
/// \brief Plik zawierający klasę interfejsu listy graczy online.
/// \details Implementuje nakładkę UI wyświetlaną po przytrzymaniu klawisza TAB,
/// prezentującą listę aktualnie połączonych graczy z ich pseudonimami,
/// rolami (HOST/KLIENT) oraz wartościami pingu.

using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Klasa zarządzająca interfejsem użytkownika listy graczy online.
/// Wyświetla tabelę z maksymalnie 4 graczami, ich pseudonimami,
/// kolorami identyfikacyjnymi, rolami sieciowymi oraz pingiem.
/// </summary>
/// <remarks>
/// Lista jest widoczna tylko podczas przytrzymywania klawisza TAB.
/// Interfejs jest tworzony w całości programistycznie w metodzie <see cref="CreateUI"/>.
/// Dane graczy są odświeżane co klatkę z obiektów <see cref="NetworkPlayer"/>.
/// Panel obsługuje do 4 graczy jednocześnie.
/// </remarks>
public class PlayerListUI : MonoBehaviour
{

    /// <summary>
    /// Kolor tła głównego panelu listy graczy.
    /// </summary>
    private static readonly Color PanelBackground = new Color(0.025f, 0.03f, 0.045f, 0.94f);

    /// <summary>
    /// Kolor tła nagłówka panelu listy graczy.
    /// </summary>
    private static readonly Color HeaderColor = new Color(0.06f, 0.10f, 0.16f, 0.95f);

    /// <summary>
    /// Kolor tła parzystych wierszy listy graczy (efekt naprzemiennych kolorów).
    /// </summary>
    private static readonly Color RowEven = new Color(0.05f, 0.07f, 0.10f, 0.80f);

    /// <summary>
    /// Kolor tła nieparzystych wierszy listy graczy (efekt naprzemiennych kolorów).
    /// </summary>
    private static readonly Color RowOdd = new Color(0.07f, 0.09f, 0.13f, 0.80f);

    /// <summary>
    /// Kolor złotego tekstu używany dla tytułu listy.
    /// </summary>
    private static readonly Color GoldText = new Color(0.875f, 0.725f, 0.32f);

    /// <summary>
    /// Kolor białego tekstu używany dla pseudonimów graczy.
    /// </summary>
    private static readonly Color WhiteText = new Color(0.9f, 0.91f, 0.93f, 0.92f);

    /// <summary>
    /// Kolor tekstu drugorzędnego używany dla nagłówków kolumn, ról i pingu.
    /// </summary>
    private static readonly Color SubText = new Color(0.65f, 0.70f, 0.78f);

    /// <summary>
    /// Kolor ramki otaczającej panel listy graczy.
    /// </summary>
    private static readonly Color FrameColor = new Color(0.14f, 0.16f, 0.20f);

    /// <summary>
    /// Komponent Canvas używany do renderowania nakładki listy graczy.
    /// </summary>
    private Canvas listCanvas;

    /// <summary>
    /// Główny obiekt panelu listy graczy zawierający wszystkie elementy interfejsu.
    /// </summary>
    private GameObject listPanel;

    /// <summary>
    /// Tekst tytułowy wyświetlający "GRACZE ONLINE" wraz z liczbą połączonych graczy.
    /// </summary>
    private Text titleText;

    /// <summary>
    /// Tekst podpowiedzi na dole panelu informujący o sterowaniu (TAB, B).
    /// </summary>
    private Text hintText;

    /// <summary>
    /// Kontener przechowujący wiersze z danymi poszczególnych graczy.
    /// </summary>
    private GameObject rowContainer;

    /// <summary>
    /// Tablica obiektów GameObject reprezentujących wiersze graczy (maksymalnie 4).
    /// </summary>
    private GameObject[] playerRows = new GameObject[4];

    /// <summary>
    /// Tablica kolorowych kropek identyfikujących poszczególnych graczy.
    /// Każdy gracz ma przypisany unikalny kolor z palety <see cref="NetworkPlayer.PlayerColors"/>.
    /// </summary>
    private Image[] playerColorDots = new Image[4];

    /// <summary>
    /// Tablica tekstów wyświetlających pseudonimy graczy.
    /// Pseudonim lokalnego gracza jest oznaczony sufiksem "(TY)".
    /// </summary>
    private Text[] playerNameTexts = new Text[4];

    /// <summary>
    /// Tablica tekstów wyświetlających role graczy (HOST lub KLIENT).
    /// </summary>
    private Text[] playerRoleTexts = new Text[4];

    /// <summary>
    /// Tablica tekstów wyświetlających przybliżone wartości pingu graczy.
    /// </summary>
    private Text[] playerPingTexts = new Text[4];

    /// <summary>
    /// Buforowana czcionka używana do renderowania wszystkich elementów tekstowych.
    /// Ładowana raz podczas inicjalizacji z zasobów wbudowanych.
    /// </summary>
    private Font cachedFont;

    /// <summary>
    /// Flaga określająca czy lista graczy jest aktualnie widoczna na ekranie.
    /// </summary>
    private bool isVisible;

    /// <summary>
    /// Inicjalizuje interfejs listy graczy podczas przebudzenia obiektu.
    /// Wywołuje <see cref="CreateUI"/> do zbudowania struktury UI.
    /// </summary>
    private void Awake()
    {
        CreateUI();
    }

    /// <summary>
    /// Aktualizacja wywoływana co klatkę. Obsługuje wejście klawiszowe (TAB)
    /// do wyświetlania i ukrywania listy graczy oraz odświeża dane graczy,
    /// gdy lista jest widoczna.
    /// </summary>
    /// <remarks>
    /// Lista jest wyświetlana po naciśnięciu klawisza TAB i ukrywana po jego puszczeniu,
    /// co odpowiada typowemu wzorcowi "przytrzymaj aby zobaczyć" znanemu z gier wieloosobowych.
    /// </remarks>
    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ShowList();
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            HideList();
        }

        if (isVisible)
        {
            RefreshPlayerData();
        }
    }

    /// <summary>
    /// Wyświetla listę graczy, włączając canvas nakładki.
    /// </summary>
    private void ShowList()
    {
        isVisible = true;
        if (listCanvas != null)
        {
            listCanvas.enabled = true;
        }
    }

    /// <summary>
    /// Ukrywa listę graczy, wyłączając canvas nakładki.
    /// </summary>
    private void HideList()
    {
        isVisible = false;
        if (listCanvas != null)
        {
            listCanvas.enabled = false;
        }
    }

    /// <summary>
    /// Tworzy cały interfejs listy graczy programistycznie, bez użycia prefabów.
    /// </summary>
    /// <remarks>
    /// Buduje następujące elementy:
    /// <list type="bullet">
    ///   <item><description>Canvas z CanvasScaler i GraphicRaycaster (sortingOrder 150)</description></item>
    ///   <item><description>Półprzezroczyste tło przyciemniające (backdrop)</description></item>
    ///   <item><description>Panel główny z ramką i nagłówkiem</description></item>
    ///   <item><description>Nagłówki kolumn: NICK, ROLA, PING</description></item>
    ///   <item><description>4 wiersze graczy z kropką kolorową, pseudonimem, rolą i pingiem</description></item>
    ///   <item><description>Tekst podpowiedzi na dole panelu</description></item>
    /// </list>
    /// </remarks>
    private void CreateUI()
    {
        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont == null)
        {
            cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject canvasObject = new GameObject("PlayerListCanvas");
        canvasObject.transform.SetParent(transform, false);
        listCanvas = canvasObject.AddComponent<Canvas>();
        listCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        listCanvas.sortingOrder = 150;
        listCanvas.enabled = false;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject backdrop = CreatePanel(canvasObject.transform, "ListBackdrop",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.35f));
        RectTransform bdRect = backdrop.GetComponent<RectTransform>();
        bdRect.anchorMin = Vector2.zero;
        bdRect.anchorMax = Vector2.one;
        bdRect.offsetMin = Vector2.zero;
        bdRect.offsetMax = Vector2.zero;

        listPanel = CreatePanel(canvasObject.transform, "PlayerListPanel",
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(500f, 320f), PanelBackground);

        CreatePanelBorder(listPanel.transform, FrameColor);

        CreatePanel(listPanel.transform, "HeaderBg",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -4f), new Vector2(-8f, 50f), HeaderColor);

        titleText = CreateText(listPanel.transform, "Title", 20, TextAnchor.MiddleCenter,
            GoldText, FontStyle.Bold);
        titleText.text = "GRACZE ONLINE";
        PositionRect(titleText, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(460f, 40f));

        Text colName = CreateText(listPanel.transform, "ColName", 12, TextAnchor.MiddleLeft,
            SubText, FontStyle.Normal);
        colName.text = "NICK";
        PositionRect(colName, new Vector2(0f, 1f), new Vector2(70f, -68f), new Vector2(200f, 20f));

        Text colRole = CreateText(listPanel.transform, "ColRole", 12, TextAnchor.MiddleCenter,
            SubText, FontStyle.Normal);
        colRole.text = "ROLA";
        PositionRect(colRole, new Vector2(0.5f, 1f), new Vector2(60f, -68f), new Vector2(100f, 20f));

        Text colPing = CreateText(listPanel.transform, "ColPing", 12, TextAnchor.MiddleRight,
            SubText, FontStyle.Normal);
        colPing.text = "PING";
        PositionRect(colPing, new Vector2(1f, 1f), new Vector2(-30f, -68f), new Vector2(80f, 20f));

        CreatePanel(listPanel.transform, "HeaderSep",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -82f), new Vector2(-16f, 1f), new Color(1f, 1f, 1f, 0.12f));

        for (int i = 0; i < 4; i++)
        {
            CreatePlayerRow(i);
        }

        hintText = CreateText(listPanel.transform, "Hint", 11, TextAnchor.MiddleCenter,
            new Color(0.5f, 0.55f, 0.65f), FontStyle.Italic);
        hintText.text = "Przytrzymaj TAB aby widziec liste  |  B — sklep";
        PositionRect(hintText, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(460f, 20f));
    }

    /// <summary>
    /// Tworzy pojedynczy wiersz gracza w tabeli listy.
    /// Wiersz zawiera kolorową kropkę identyfikacyjną, pseudonim, rolę sieciową i ping.
    /// </summary>
    /// <param name="index">
    /// Indeks wiersza (0-3). Określa pozycję pionową, kolor tła (parzyste/nieparzyste)
    /// oraz domyślny kolor kropki identyfikacyjnej gracza.
    /// </param>
    /// <remarks>
    /// Wiersze są domyślnie nieaktywne i aktywowane dopiero gdy odpowiadający im
    /// gracz jest połączony i zspawnowany na serwerze.
    /// </remarks>
    private void CreatePlayerRow(int index)
    {
        float yOffset = -92f - index * 48f;
        Color rowBg = index % 2 == 0 ? RowEven : RowOdd;

        playerRows[index] = CreatePanel(listPanel.transform, "PlayerRow" + index,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, yOffset), new Vector2(-12f, 44f), rowBg);
        playerRows[index].SetActive(false);

        GameObject dotObj = new GameObject("Dot" + index);
        dotObj.transform.SetParent(playerRows[index].transform, false);
        playerColorDots[index] = dotObj.AddComponent<Image>();
        playerColorDots[index].color = NetworkPlayer.PlayerColors[index % NetworkPlayer.PlayerColors.Length];
        RectTransform dotRect = dotObj.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0f, 0.5f);
        dotRect.anchorMax = new Vector2(0f, 0.5f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = new Vector2(22f, 0f);
        dotRect.sizeDelta = new Vector2(14f, 14f);

        playerNameTexts[index] = CreateText(playerRows[index].transform, "Name" + index,
            16, TextAnchor.MiddleLeft, WhiteText, FontStyle.Bold);
        RectTransform nameRect = playerNameTexts[index].GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(0.55f, 1f);
        nameRect.offsetMin = new Vector2(42f, 0f);
        nameRect.offsetMax = new Vector2(0f, 0f);

        playerRoleTexts[index] = CreateText(playerRows[index].transform, "Role" + index,
            13, TextAnchor.MiddleCenter, SubText, FontStyle.Normal);
        RectTransform roleRect = playerRoleTexts[index].GetComponent<RectTransform>();
        roleRect.anchorMin = new Vector2(0.55f, 0f);
        roleRect.anchorMax = new Vector2(0.80f, 1f);
        roleRect.offsetMin = Vector2.zero;
        roleRect.offsetMax = Vector2.zero;

        playerPingTexts[index] = CreateText(playerRows[index].transform, "Ping" + index,
            13, TextAnchor.MiddleRight, SubText, FontStyle.Normal);
        RectTransform pingRect = playerPingTexts[index].GetComponent<RectTransform>();
        pingRect.anchorMin = new Vector2(0.80f, 0f);
        pingRect.anchorMax = new Vector2(1f, 1f);
        pingRect.offsetMin = Vector2.zero;
        pingRect.offsetMax = new Vector2(-12f, 0f);
    }

    /// <summary>
    /// Odświeża dane graczy wyświetlane na liście.
    /// Wyszukuje wszystkie aktywne obiekty <see cref="NetworkPlayer"/> na scenie
    /// i aktualizuje odpowiadające im wiersze w tabeli.
    /// </summary>
    /// <remarks>
    /// Dla każdego spawnerowanego gracza aktualizowane są:
    /// <list type="bullet">
    ///   <item><description>Kolor kropki identyfikacyjnej (na podstawie indeksu gracza)</description></item>
    ///   <item><description>Pseudonim (z dopiskiem "(TY)" dla lokalnego gracza)</description></item>
    ///   <item><description>Rola sieciowa (HOST z zielonym kolorem tekstu lub KLIENT)</description></item>
    ///   <item><description>Ping (przybliżona wartość lub "—" dla hosta)</description></item>
    /// </list>
    /// Tytuł panelu jest aktualizowany z bieżącą liczbą połączonych graczy (np. "GRACZE ONLINE — 3/4").
    /// </remarks>
    private void RefreshPlayerData()
    {
        NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < 4; i++)
        {
            if (i < players.Length && players[i] != null && players[i].IsSpawned)
            {
                NetworkPlayer np = players[i];
                playerRows[i].SetActive(true);

                int colorIdx = np.PlayerIndex % NetworkPlayer.PlayerColors.Length;
                playerColorDots[i].color = NetworkPlayer.PlayerColors[colorIdx];

                string displayName = np.PlayerName;
                if (np.IsOwner)
                {
                    displayName += "  (TY)";
                }
                playerNameTexts[i].text = displayName;

                bool isHost = np.OwnerClientId == 0;
                playerRoleTexts[i].text = isHost ? "HOST" : "KLIENT";
                playerRoleTexts[i].color = isHost
                    ? new Color(0.3f, 0.85f, 0.45f)
                    : SubText;

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    playerPingTexts[i].text = isHost ? "—" : "~20ms";
                }
                else
                {
                    playerPingTexts[i].text = isHost ? "—" : "~";
                }
            }
            else
            {
                playerRows[i].SetActive(false);
            }
        }

        int count = 0;
        foreach (NetworkPlayer p in players)
        {
            if (p != null && p.IsSpawned) count++;
        }
        titleText.text = "GRACZE ONLINE  —  " + count + "/4";
    }

    /// <summary>
    /// Tworzy obiekt panelu UI z komponentem Image i skonfigurowanym RectTransform.
    /// </summary>
    /// <param name="parent">Transform rodzica, pod którym panel zostanie utworzony.</param>
    /// <param name="name">Nazwa obiektu panelu w hierarchii.</param>
    /// <param name="anchorMin">Minimalny punkt zakotwiczenia RectTransform.</param>
    /// <param name="anchorMax">Maksymalny punkt zakotwiczenia RectTransform.</param>
    /// <param name="pivot">Punkt obrotu (pivot) RectTransform.</param>
    /// <param name="anchoredPosition">Pozycja zakotwiczona panelu.</param>
    /// <param name="size">Rozmiar panelu (sizeDelta).</param>
    /// <param name="color">Kolor tła panelu (Image).</param>
    /// <returns>Utworzony obiekt GameObject panelu.</returns>
    private GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = color;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return panel;
    }

    /// <summary>
    /// Tworzy ramkę (obramowanie) wokół panelu, składającą się z czterech cienkich paneli
    /// o grubości 2 pikseli umieszczonych na górze, dole, lewej i prawej stronie.
    /// </summary>
    /// <param name="parent">Transform rodzica (panel, wokół którego tworzona jest ramka).</param>
    /// <param name="borderColor">Kolor ramki.</param>
    private void CreatePanelBorder(Transform parent, Color borderColor)
    {
        float t = 2f;
        CreatePanel(parent, "BT", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, t), borderColor);
        CreatePanel(parent, "BB", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, t), borderColor);
        CreatePanel(parent, "BL", new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), Vector2.zero, new Vector2(t, 0f), borderColor);
        CreatePanel(parent, "BR", new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(1f, 0.5f), Vector2.zero, new Vector2(t, 0f), borderColor);
    }

    /// <summary>
    /// Tworzy element tekstowy UI z określonym stylem, używając buforowanej czcionki.
    /// </summary>
    /// <param name="parent">Transform rodzica dla tekstu.</param>
    /// <param name="name">Nazwa obiektu tekstowego w hierarchii.</param>
    /// <param name="size">Rozmiar czcionki w pikselach.</param>
    /// <param name="alignment">Wyrównanie tekstu (np. środek, lewo, prawo).</param>
    /// <param name="color">Kolor tekstu.</param>
    /// <param name="style">Styl czcionki (normalny, pogrubiony, kursywa).</param>
    /// <returns>Utworzony komponent Text.</returns>
    private Text CreateText(Transform parent, string name, int size,
        TextAnchor alignment, Color color, FontStyle style)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        Text text = textObj.AddComponent<Text>();
        text.font = cachedFont;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = style;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    /// <summary>
    /// Ustawia pozycję i rozmiar RectTransform danego komponentu,
    /// używając pojedynczego punktu zakotwiczenia.
    /// </summary>
    /// <param name="comp">Komponent, którego RectTransform ma zostać skonfigurowany.</param>
    /// <param name="anchor">Punkt zakotwiczenia (anchorMin i anchorMax ustawione na tę samą wartość).</param>
    /// <param name="pos">Pozycja zakotwiczona elementu.</param>
    /// <param name="size">Rozmiar elementu (sizeDelta).</param>
    private void PositionRect(Component comp, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        RectTransform rect = comp.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }
}
