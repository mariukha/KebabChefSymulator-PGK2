using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// \file AchievementPopup.cs
/// \brief System osiągnięć z wyskakującymi powiadomieniami typu toast.
/// \details Śledzi kamienie milowe gracza (zrealizowane zamówienia, ulepszenia, serie sukcesów)
/// i wyświetla animowane powiadomienia w rogu ekranu. Osiągnięcia są odblokowywane
/// jednorazowo i kolejkowane do wyświetlenia w przypadku wielu jednoczesnych odblokować.
/// Implementuje wzorzec Singleton, zapewniając jedną globalną instancję systemu osiągnięć.
/// </summary>
public class AchievementPopup : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singletona systemu osiągnięć.
    /// Umożliwia globalny dostęp do wyświetlania powiadomień o osiągnięciach.
    /// </summary>
    public static AchievementPopup Instance { get; private set; }

    /// <summary>
    /// Canvas używany do renderowania powiadomień o osiągnięciach.
    /// Wyświetlany jest w trybie Screen Space Overlay z wysokim porządkiem sortowania.
    /// </summary>
    private Canvas achievementCanvas;

    /// <summary>
    /// Buforowana czcionka używana do renderowania tekstu w powiadomieniach.
    /// Ładowana jest z wbudowanych zasobów Unity.
    /// </summary>
    private Font cachedFont;

    /// <summary>
    /// Zbiór identyfikatorów już odblokowanych osiągnięć.
    /// Zapobiega wielokrotnemu odblokowania tego samego osiągnięcia.
    /// </summary>
    private readonly HashSet<string> unlocked = new HashSet<string>();

    /// <summary>
    /// Kolejka oczekujących powiadomień do wyświetlenia.
    /// Każdy element to tablica dwuelementowa: [tytuł, opis].
    /// </summary>
    private readonly Queue<string[]> pendingPopups = new Queue<string[]>();

    /// <summary>
    /// Aktualnie wyświetlany obiekt powiadomienia (popup).
    /// Wartość null oznacza brak aktywnego powiadomienia.
    /// </summary>
    private GameObject activePopup;

    /// <summary>
    /// Licznik czasu pozostałego do zamknięcia aktywnego powiadomienia (w sekundach).
    /// </summary>
    private float popupTimer;

    /// <summary>
    /// Czas wyświetlania pojedynczego powiadomienia o osiągnięciu (w sekundach).
    /// </summary>
    private const float PopupDuration = 3.5f;

    /// <summary>
    /// Szybkość animacji wsuwania powiadomienia na ekran.
    /// Wyższa wartość oznacza szybsze wsunięcie.
    /// </summary>
    private const float SlideSpeed = 6f;

    /// <summary>
    /// Ostatnia zapamiętana liczba zrealizowanych zamówień.
    /// Służy do wykrywania nowych ukończonych zamówień w kolejnych klatkach.
    /// </summary>
    private int lastCompleted;

    /// <summary>
    /// Ostatnia zapamiętana liczba nieudanych zamówień.
    /// Służy do wykrywania nowych porażek i resetowania serii sukcesów.
    /// </summary>
    private int lastFailed;

    /// <summary>
    /// Licznik kolejnych sukcesów bez porażki.
    /// Resetowany przy każdym nieudanym zamówieniu.
    /// </summary>
    private int consecutiveSuccess;

    /// <summary>
    /// Ostatnia zapamiętana łączna liczba zakupionych ulepszeń.
    /// Służy do wykrywania nowych zakupów ulepszeń.
    /// </summary>
    private int lastUpgradeCount;

    /// <summary>
    /// Inicjalizuje Singleton systemu osiągnięć i ładuje czcionkę.
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
    /// Tworzy Canvas do wyświetlania powiadomień o osiągnięciach.
    /// </summary>
    private void Start()
    {
        CreateCanvas();
    }

    /// <summary>
    /// Sprawdza kamienie milowe i aktualizuje stan wyświetlanych powiadomień w każdej klatce.
    /// </summary>
    private void Update()
    {
        CheckMilestones();
        UpdatePopup();
    }

    /// <summary>
    /// Sprawdza postęp gracza i próbuje odblokować odpowiednie osiągnięcia.
    /// Monitoruje liczbę zrealizowanych zamówień, serie sukcesów, zakupione ulepszenia
    /// oraz czy wszystkie ulepszenia osiągnęły maksymalny poziom.
    /// </summary>
    private void CheckMilestones()
    {
        OrderManager om = OrderManager.Instance;
        if (om == null) return;

        int completed = om.CompletedOrders;
        int failed = om.FailedOrders;

        if (completed > lastCompleted)
        {
            consecutiveSuccess += (completed - lastCompleted);

            if (completed >= 1) TryUnlock("first_kebab", "Pierwszy Kebab!", "Zrealizowales pierwsze zamowienie");
            if (completed >= 10) TryUnlock("experienced", "Doswiadczony Kucharz", "10 zrealizowanych zamowien");
            if (completed >= 25) TryUnlock("master", "Mistrz Kebaba", "25 zrealizowanych zamowien");
            if (completed >= 50) TryUnlock("legend", "Legenda Kuchni", "50 zrealizowanych zamowien");
            if (consecutiveSuccess >= 5) TryUnlock("perfectionist", "Perfekcjonista", "5 zamowien z rzedu bez bledu");
            if (consecutiveSuccess >= 10) TryUnlock("flawless", "Bezbladny", "10 zamowien z rzedu bez bledu");
        }

        if (failed > lastFailed)
        {
            consecutiveSuccess = 0;
        }

        lastCompleted = completed;
        lastFailed = failed;

        ShopManager sm = ShopManager.Instance;
        if (sm != null)
        {
            int totalUpgrades = sm.TotalUpgradesPurchased;

            if (totalUpgrades > lastUpgradeCount && totalUpgrades >= 1)
            {
                TryUnlock("investor", "Inwestor", "Kupiono pierwsze ulepszenie");
            }

            var allDefs = sm.GetAllDefinitions();
            if (allDefs != null && allDefs.Count > 0)
            {
                int maxedCount = 0;
                foreach (var def in allDefs)
                {
                    if (def != null && sm.GetUpgradeLevel(def.type) >= def.maxLevel)
                    {
                        maxedCount++;
                    }
                }

                if (maxedCount >= allDefs.Count)
                {
                    TryUnlock("empire", "Imperium Kebabowe", "Wszystkie ulepszenia na maksimum!");
                }
            }

            lastUpgradeCount = totalUpgrades;
        }
    }

    /// <summary>
    /// Próbuje odblokować osiągnięcie o podanym identyfikatorze.
    /// Jeśli osiągnięcie nie było jeszcze odblokowane, dodaje je do kolejki powiadomień.
    /// </summary>
    /// <param name="id">Unikalny identyfikator osiągnięcia.</param>
    /// <param name="title">Tytuł osiągnięcia wyświetlany w powiadomieniu.</param>
    /// <param name="description">Opis osiągnięcia wyświetlany pod tytułem.</param>
    private void TryUnlock(string id, string title, string description)
    {
        if (unlocked.Contains(id)) return;
        unlocked.Add(id);
        pendingPopups.Enqueue(new[] { title, description });
    }

    /// <summary>
    /// Aktualizuje stan aktywnego powiadomienia — animuje wsuwanie, wygaszanie
    /// i usuwa powiadomienie po upływie czasu. Jeśli nie ma aktywnego powiadomienia,
    /// pobiera następne z kolejki oczekujących.
    /// </summary>
    private void UpdatePopup()
    {

        if (activePopup != null)
        {
            popupTimer -= Time.deltaTime;

            RectTransform rt = activePopup.GetComponent<RectTransform>();
            if (rt != null)
            {
                float targetX = -20f;
                float currentX = rt.anchoredPosition.x;
                rt.anchoredPosition = new Vector2(
                    Mathf.Lerp(currentX, targetX, Time.deltaTime * SlideSpeed),
                    rt.anchoredPosition.y);
            }

            if (popupTimer < 0.5f)
            {
                CanvasGroup cg = activePopup.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = Mathf.Clamp01(popupTimer / 0.5f);
                }
            }

            if (popupTimer <= 0f)
            {
                Destroy(activePopup);
                activePopup = null;
            }

            return;
        }

        if (pendingPopups.Count > 0)
        {
            string[] data = pendingPopups.Dequeue();
            ShowPopup(data[0], data[1]);
        }
    }

    /// <summary>
    /// Tworzy i wyświetla wizualne powiadomienie o osiągnięciu na ekranie.
    /// Powiadomienie składa się z tła, złotego akcentu, tytułu i opisu.
    /// Wsuwa się z prawej strony ekranu i odtwarza dźwięk dzwonka.
    /// </summary>
    /// <param name="title">Tytuł osiągnięcia do wyświetlenia.</param>
    /// <param name="description">Opis osiągnięcia do wyświetlenia.</param>
    private void ShowPopup(string title, string description)
    {
        if (achievementCanvas == null) return;

        GameObject popup = new GameObject("AchievementPopup");
        popup.transform.SetParent(achievementCanvas.transform, false);

        Image bg = popup.AddComponent<Image>();
        bg.color = new Color(0.025f, 0.03f, 0.05f, 0.95f);

        RectTransform rt = popup.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(320f, 80f);
        rt.sizeDelta = new Vector2(300f, 70f);

        CanvasGroup cg = popup.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        GameObject accent = new GameObject("Accent");
        accent.transform.SetParent(popup.transform, false);
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = new Color(0.875f, 0.725f, 0.32f);
        RectTransform ar = accent.GetComponent<RectTransform>();
        ar.anchorMin = new Vector2(0f, 0f);
        ar.anchorMax = new Vector2(0f, 1f);
        ar.pivot = new Vector2(0f, 0.5f);
        ar.anchoredPosition = Vector2.zero;
        ar.sizeDelta = new Vector2(4f, 0f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(popup.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = cachedFont;
        titleText.fontSize = 13;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(0.875f, 0.78f, 0.52f);
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.text = title;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform trt = titleObj.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.5f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(14f, 0f);
        trt.offsetMax = new Vector2(-8f, -5f);

        GameObject descObj = new GameObject("Desc");
        descObj.transform.SetParent(popup.transform, false);
        Text descText = descObj.AddComponent<Text>();
        descText.font = cachedFont;
        descText.fontSize = 10;
        descText.color = new Color(0.55f, 0.58f, 0.64f);
        descText.alignment = TextAnchor.MiddleLeft;
        descText.text = description;
        descText.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform drt = descObj.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0f, 0f);
        drt.anchorMax = new Vector2(1f, 0.5f);
        drt.offsetMin = new Vector2(14f, 5f);
        drt.offsetMax = new Vector2(-8f, 0f);

        activePopup = popup;
        popupTimer = PopupDuration;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayNewOrderSound();
        }
    }

    /// <summary>
    /// Tworzy dedykowany Canvas do wyświetlania powiadomień o osiągnięciach.
    /// Canvas jest ustawiony w trybie Screen Space Overlay z wysokim porządkiem sortowania (160),
    /// aby powiadomienia były widoczne ponad większością elementów UI.
    /// </summary>
    private void CreateCanvas()
    {
        GameObject obj = new GameObject("AchievementCanvas");
        obj.transform.SetParent(transform, false);
        achievementCanvas = obj.AddComponent<Canvas>();
        achievementCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        achievementCanvas.sortingOrder = 160;
        CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
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
