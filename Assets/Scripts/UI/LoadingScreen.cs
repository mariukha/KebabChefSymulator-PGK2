using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// \file LoadingScreen.cs
/// \brief Ekran ładowania z efektem zanikania przy starcie gry.
/// \details Wyświetla nakładkę przejściową, która zanika z czarnego do przezroczystego
/// w czasie 1.8 sekundy. Tworzy płynne przejście zamiast natychmiastowego wyświetlenia
/// sceny kuchni. Wyświetla również losową wskazówkę dla gracza podczas ładowania.
/// Używa Time.unscaledDeltaTime, dzięki czemu animacja działa niezależnie od Time.timeScale.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    /// <summary>
    /// Komponent Image służący jako nakładka zanikania (fade overlay).
    /// Pokrywa cały ekran ciemnym kolorem, który stopniowo staje się przezroczysty.
    /// </summary>
    private Image fadeOverlay;

    /// <summary>
    /// Komponent Text wyświetlający losową wskazówkę dla gracza podczas ładowania.
    /// </summary>
    private Text loadingText;

    /// <summary>
    /// Licznik czasu pozostałego do zakończenia animacji zanikania (w sekundach).
    /// </summary>
    private float fadeTimer;

    /// <summary>
    /// Całkowity czas trwania animacji zanikania (w sekundach).
    /// </summary>
    private float fadeDuration = 1.8f;

    /// <summary>
    /// Flaga określająca, czy animacja zanikania jest aktualnie w trakcie.
    /// Po zakończeniu zanikania ustawiana na false, a nakładka jest dezaktywowana.
    /// </summary>
    private bool isFading = true;

    /// <summary>
    /// Tablica losowych wskazówek wyświetlanych na ekranie ładowania.
    /// Przy każdym uruchomieniu gry losowo wybierana jest jedna wskazówka.
    /// </summary>
    private static readonly string[] Tips =
    {
        "Nacisnij E aby pobrac skladnik ze stanowiska",
        "Kebab potrzebuje lawasza jako bazy!",
        "Nacisnij B aby otworzyc sklep z ulepszeniami",
        "Zrealizuj zamowienia szybciej zeby zarobic wiecej",
        "Nie zapomnij pokroic skladnikow na desce!",
        "Kazdy skladnik musi byc najpierw przetworzony",
        "Nacisnij Q aby wyrzucic trzymany przedmiot",
        "Nacisnij TAB aby zobaczyc liste graczy"
    };

    /// <summary>
    /// Tworzy nakładkę ekranu ładowania i inicjalizuje licznik zanikania.
    /// </summary>
    private void Start()
    {
        CreateOverlay();
        fadeTimer = fadeDuration;
    }

    /// <summary>
    /// Aktualizuje animację zanikania w każdej klatce.
    /// Zmniejsza przezroczystość nakładki proporcjonalnie do upływającego czasu.
    /// Po zakończeniu zanikania dezaktywuje obiekt nakładki.
    /// </summary>
    /// <remarks>
    /// Używa Time.unscaledDeltaTime zamiast Time.deltaTime, aby animacja
    /// działała poprawnie nawet gdy gra jest wstrzymana (Time.timeScale = 0).
    /// </remarks>
    private void Update()
    {
        if (!isFading || fadeOverlay == null) return;

        fadeTimer -= Time.unscaledDeltaTime;

        if (fadeTimer > 0f)
        {
            float alpha = Mathf.Clamp01(fadeTimer / fadeDuration);
            fadeOverlay.color = new Color(0.012f, 0.014f, 0.022f, alpha);
            if (loadingText != null)
            {
                loadingText.color = new Color(0.55f, 0.58f, 0.64f, alpha * 0.7f);
            }
        }
        else
        {
            isFading = false;
            if (fadeOverlay != null) fadeOverlay.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Tworzy pełnoekranową nakładkę z tekstem wskazówki.
    /// Szuka istniejącego Canvas w trybie Screen Space Overlay lub tworzy nowy.
    /// Nakładka ma ciemny kolor tła i wyświetla losowo wybraną wskazówkę
    /// na dole ekranu.
    /// </summary>
    private void CreateOverlay()
    {

        Canvas canvas = null;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.sortingOrder < 200)
            {
                canvas = c;
                break;
            }
        }

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("LoadingCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 190;
            canvasObj.AddComponent<CanvasScaler>();
        }

        GameObject overlayObj = new GameObject("LoadingOverlay");
        overlayObj.transform.SetParent(canvas.transform, false);
        fadeOverlay = overlayObj.AddComponent<Image>();
        fadeOverlay.color = new Color(0.012f, 0.014f, 0.022f, 1f);
        RectTransform rect = overlayObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GameObject textObj = new GameObject("LoadingTip");
        textObj.transform.SetParent(overlayObj.transform, false);
        loadingText = textObj.AddComponent<Text>();
        loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (loadingText.font == null) loadingText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        loadingText.fontSize = 11;
        loadingText.alignment = TextAnchor.LowerCenter;
        loadingText.color = new Color(0.55f, 0.58f, 0.64f, 0.7f);
        loadingText.text = Tips[Random.Range(0, Tips.Length)];
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0f);
        textRect.anchorMax = new Vector2(0.5f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = new Vector2(0f, 40f);
        textRect.sizeDelta = new Vector2(600f, 30f);
    }
}
