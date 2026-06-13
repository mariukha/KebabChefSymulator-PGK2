/// \file KitchenOrderBoard.cs
/// \brief Plik zawierający klasę KitchenOrderBoard odpowiedzialną za
/// wyświetlanie tablicy zamówień w kuchni na monitorze 3D.
/// \details Klasa tworzy programowo model monitora 3D (z ramą, ekranem i
/// diodą statusu) oraz interfejs Canvas w przestrzeni świata (World Space),
/// na którym wyświetlane są szczegóły aktualnego zamówienia, lista składników,
/// statystyki sesji oraz pasek pilności.

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Klasa odpowiedzialna za tworzenie i aktualizowanie tablicy zamówień
/// wyświetlanej na monitorze 3D w scenie kuchni.
/// </summary>
/// <remarks>
/// Monitor składa się z fizycznych bloków 3D (rama, ekran, dioda statusu)
/// oraz Canvas w trybie World Space wyświetlającego informacje o zamówieniu.
/// Tablica jest aktualizowana co klatkę i wyświetla: nagłówek "LIVE ORDERS",
/// dane klienta i zamówienia, listę wymaganych składników, statystyki sesji
/// oraz animowany pasek pilności zmieniający kolor w zależności od pozostałego czasu.
/// Inicjalizacja odbywa się przez wywołanie metody <see cref="Initialize"/>.
/// </remarks>
public class KitchenOrderBoard : MonoBehaviour
{
    /// <summary>
    /// Tekst nagłówka tablicy zamówień (wyświetla "LIVE ORDERS").
    /// </summary>
    private Text headerText;

    /// <summary>
    /// Tekst metadanych zamówienia zawierający nazwę klienta, nazwę dania,
    /// pozostały czas i kwotę nagrody.
    /// </summary>
    private Text metaText;

    /// <summary>
    /// Tekst listy wymaganych składników z oznaczeniami wypunktowanymi.
    /// </summary>
    private Text ingredientsText;

    /// <summary>
    /// Tekst stopki wyświetlający statystyki sesji
    /// (liczba ukończonych i nieudanych zamówień).
    /// </summary>
    private Text footerText;

    /// <summary>
    /// Obraz paska pilności zamówienia, którego kolor zmienia się
    /// w zależności od proporcji pozostałego czasu do całkowitego.
    /// </summary>
    private Image urgencyBar;

    /// <summary>
    /// RectTransform paska pilności, używany do dynamicznej zmiany
    /// szerokości w zależności od postępu czasu zamówienia.
    /// </summary>
    private RectTransform urgencyBarRect;

    /// <summary>
    /// Buforowany shader używany do tworzenia materiałów bloków 3D monitora.
    /// Preferuje URP Lit, awaryjnie Standard.
    /// </summary>
    private Shader cachedLitShader;

    /// <summary>
    /// Inicjalizuje tablicę zamówień, tworząc wizualne elementy monitora
    /// (bloki 3D i Canvas z tekstami).
    /// </summary>
    public void Initialize()
    {
        CreateMonitorVisuals();
    }

    /// <summary>
    /// Metoda Unity wywoływana co klatkę.
    /// Odświeża zawartość tablicy zamówień aktualnymi danymi.
    /// </summary>
    private void Update()
    {
        RefreshBoard();
    }

    /// <summary>
    /// Tworzy kompletną wizualizację monitora: bloki 3D (obudowa, rama, ekran,
    /// krawędź, dioda) oraz Canvas World Space z elementami tekstowymi i paskiem pilności.
    /// </summary>
    /// <remarks>
    /// Monitor ma wymiary około 3.5 x 2.05 jednostek i składa się z wielu warstw:
    /// - MonitorBack: główna obudowa w ciemnym kolorze,
    /// - MonitorFrame: rama w nieco jaśniejszym odcieniu,
    /// - ScreenGlow: powierzchnia ekranu z ciemnym odcieniem niebieskiego,
    /// - MonitorShadowLip: dolna krawędź cienia,
    /// - StatusLight: zielona dioda statusu w prawym dolnym rogu.
    ///
    /// Canvas jest skalowany do 0.00245 i obrócony o 180° wokół osi Y,
    /// aby tekst był widoczny od przodu monitora.
    /// </remarks>
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
            new Vector2(-640f, -304f),
            new Vector2(1280f, 26f),
            new Color(0.2f, 0.8f, 0.4f, 1f));
        urgencyBarRect = urgencyBar.GetComponent<RectTransform>();
        urgencyBarRect.pivot = new Vector2(0f, 0.5f);

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

    /// <summary>
    /// Odświeża zawartość tablicy zamówień danymi z <see cref="OrderManager"/>.
    /// Aktualizuje nagłówek, metadane zamówienia, listę składników,
    /// stopkę ze statystykami oraz pasek pilności.
    /// </summary>
    /// <remarks>
    /// Gdy nie ma aktywnego zamówienia, wyświetla komunikat "No active order"
    /// i ustawia pasek pilności na pełny (zielony).
    /// Gdy zamówienie jest aktywne, wyświetla szczegółowe informacje
    /// o kliencie, daniu, nagrodzie i wymaganych składnikach.
    /// </remarks>
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
                SetUrgencyProgress(1f);
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
            metaText.text = "TIME     " + timeRemaining + " s\n";
            ingredientsText.text = orderDesc;
        }

        footerText.text =
            "Completed " + OrderManager.Instance.CompletedOrders +
            "   |   Failed " + OrderManager.Instance.FailedOrders;

        if (urgencyBar != null)
        {
            float fullTime = order != null ? order.czasNaRealizacje : 120f;
            float ratio = fullTime <= 0.01f ? 1f : Mathf.Clamp01(OrderManager.Instance.RemainingOrderTime / fullTime);
            urgencyBar.color = GetUrgencyColor(OrderManager.Instance.RemainingOrderTime, fullTime);
            SetUrgencyProgress(ratio);
        }
    }

    /// <summary>
    /// Ustawia postęp paska pilności poprzez zmianę jego szerokości.
    /// </summary>
    /// <param name="ratio">Proporcja postępu w zakresie 0-1, gdzie 1 oznacza pełny pasek.</param>
    private void SetUrgencyProgress(float ratio)
    {
        if (urgencyBarRect == null)
        {
            return;
        }

        urgencyBarRect.sizeDelta = new Vector2(1280f * Mathf.Clamp01(ratio), 26f);
    }

    /// <summary>
    /// Oblicza kolor paska pilności na podstawie pozostałego czasu zamówienia.
    /// </summary>
    /// <param name="remainingTime">Pozostały czas zamówienia w sekundach.</param>
    /// <param name="fullTime">Całkowity czas przeznaczony na zamówienie w sekundach.</param>
    /// <returns>
    /// Kolor paska pilności:
    /// - Zielony gdy pozostało ponad 50% czasu,
    /// - Żółty gdy pozostało 25-50% czasu,
    /// - Czerwony gdy pozostało mniej niż 25% czasu.
    /// </returns>
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

    /// <summary>
    /// Tworzy blok 3D (sześcian) jako element fizyczny monitora.
    /// </summary>
    /// <param name="objectName">Nazwa obiektu bloku.</param>
    /// <param name="localPosition">Lokalna pozycja bloku względem rodzica.</param>
    /// <param name="localScale">Lokalna skala bloku (wymiary).</param>
    /// <param name="color">Kolor materiału bloku.</param>
    /// <remarks>
    /// Używa buforowanego shadera URP Lit (lub Standard jako awaryjnego).
    /// Tworzony jest nowy materiał z podanym kolorem.
    /// </remarks>
    private void CreateBlock(string objectName, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = objectName;
        block.transform.SetParent(transform, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = localScale;

        Renderer renderer = block.GetComponent<Renderer>();
        if (cachedLitShader == null)
        {
            cachedLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (cachedLitShader == null) cachedLitShader = Shader.Find("Standard");
        }

        renderer.material = new Material(cachedLitShader);
        renderer.material.color = color;
    }

    /// <summary>
    /// Tworzy panel UI z obrazem tła na podanej pozycji i o podanym rozmiarze.
    /// Element jest zakotwiczony centralnie.
    /// </summary>
    /// <param name="parent">Transformata rodzica.</param>
    /// <param name="objectName">Nazwa obiektu panelu.</param>
    /// <param name="anchoredPosition">Pozycja zakotwiczona panelu.</param>
    /// <param name="size">Rozmiar panelu w pikselach.</param>
    /// <param name="color">Kolor tła panelu.</param>
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

    /// <summary>
    /// Tworzy element obrazu UI i zwraca jego komponent <see cref="Image"/>.
    /// Podobna do <see cref="CreatePanel"/>, ale zwraca referencję do komponentu Image.
    /// </summary>
    /// <param name="parent">Transformata rodzica.</param>
    /// <param name="objectName">Nazwa obiektu obrazu.</param>
    /// <param name="anchoredPosition">Pozycja zakotwiczona obrazu.</param>
    /// <param name="size">Rozmiar obrazu w pikselach.</param>
    /// <param name="color">Kolor obrazu.</param>
    /// <returns>Komponent Image utworzonego elementu.</returns>
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

    /// <summary>
    /// Tworzy element tekstowy UI z określoną czcionką, rozmiarem, stylem,
    /// wyrównaniem, pozycją i kolorem. Zakotwiczony w lewym górnym rogu.
    /// </summary>
    /// <param name="parent">Transformata rodzica.</param>
    /// <param name="objectName">Nazwa obiektu tekstowego.</param>
    /// <param name="font">Czcionka do użycia.</param>
    /// <param name="fontSize">Rozmiar czcionki w pikselach.</param>
    /// <param name="fontStyle">Styl czcionki (normalny, pogrubiony itp.).</param>
    /// <param name="alignment">Wyrównanie tekstu.</param>
    /// <param name="anchoredPosition">Pozycja zakotwiczona elementu.</param>
    /// <param name="size">Rozmiar elementu tekstowego.</param>
    /// <param name="color">Kolor tekstu.</param>
    /// <returns>Komponent Text utworzonego elementu tekstowego.</returns>
    /// <remarks>
    /// Tekst jest konfigurowany z zawijaniem poziomym i przepełnieniem pionowym,
    /// co pozwala na wyświetlanie wieloliniowych treści.
    /// </remarks>
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
