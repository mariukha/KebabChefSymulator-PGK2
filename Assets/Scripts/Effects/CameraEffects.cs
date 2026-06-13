/// \file CameraEffects.cs
/// \brief Plik zawierający klasę CameraEffects — zestaw efektów kamery gracza.
/// \details Implementuje kołysanie głowy podczas chodzenia, trzęsienie ekranu przy zdarzeniach,
/// kolorowy błysk nakładki ekranowej oraz efekt lądowania (dip po skoku).
/// Należy dołączyć do obiektu kamery gracza.

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zestaw efektów kamery: kołysanie głowy podczas chodzenia, trzęsienie ekranu przy zdarzeniach,
/// kolorowa nakładka błyskowa na ekranie oraz efekt lądowania po skoku.
/// Dołączyć do obiektu kamery gracza.
/// </summary>
/// <remarks>
/// Klasa implementuje wzorzec Singleton. Efekty obliczane są w LateUpdate, aby nadpisać
/// ewentualne wcześniejsze zmiany pozycji kamery. Wszystkie przesunięcia (bob, shake, landing)
/// są sumowane i stosowane jako offset od bazowej pozycji lokalnej kamery.
/// Nakładka błyskowa (flash overlay) tworzona jest dynamicznie na istniejącym Canvas ScreenSpaceOverlay.
/// </remarks>
public class CameraEffects : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singletona klasy <see cref="CameraEffects"/>.
    /// Umożliwia globalny dostęp do efektów kamery z dowolnego miejsca w kodzie.
    /// </summary>
    public static CameraEffects Instance { get; private set; }

    /// <summary>
    /// Częstotliwość kołysania głowy podczas chodzenia (w Hz).
    /// Wyższe wartości powodują szybsze kołysanie.
    /// </summary>
    [Header("Head Bob")]
    public float bobFrequency = 8f;

    /// <summary>
    /// Amplituda pionowego kołysania głowy (w jednostkach świata).
    /// Kontroluje wysokość ruchu kamery w górę i w dół podczas chodzenia.
    /// </summary>
    public float bobAmplitudeY = 0.035f;

    /// <summary>
    /// Amplituda poziomego kołysania głowy (w jednostkach świata).
    /// Kontroluje ruch kamery w lewo i w prawo podczas chodzenia (połowa częstotliwości pionowej).
    /// </summary>
    public float bobAmplitudeX = 0.015f;

    /// <summary>
    /// Aktualna intensywność trzęsienia ekranu.
    /// Wartość maleje w czasie, aż trzęsienie się zakończy.
    /// </summary>
    [Header("Screen Shake")]
    private float shakeIntensity;

    /// <summary>
    /// Całkowity czas trwania bieżącego trzęsienia ekranu (w sekundach).
    /// Używany do obliczania postępu zanikania trzęsienia.
    /// </summary>
    private float shakeDuration;

    /// <summary>
    /// Pozostały czas bieżącego trzęsienia ekranu (w sekundach).
    /// Odliczany w każdej klatce — gdy osiągnie 0, trzęsienie się kończy.
    /// </summary>
    private float shakeTimer;

    /// <summary>
    /// Referencja do komponentu Image używanego jako pełnoekranowa nakładka błyskowa.
    /// Tworzona dynamicznie w metodzie <see cref="CreateFlashOverlay"/>.
    /// </summary>
    [Header("Screen Flash")]
    private Image flashOverlay;

    /// <summary>
    /// Pozostały czas wyświetlania bieżącego błysku ekranowego (w sekundach).
    /// </summary>
    private float flashTimer;

    /// <summary>
    /// Całkowity czas trwania bieżącego błysku ekranowego (w sekundach).
    /// </summary>
    private float flashDuration;

    /// <summary>
    /// Kolor bieżącego błysku ekranowego (np. zielony = sukces, czerwony = porażka).
    /// </summary>
    private Color flashColor;

    /// <summary>
    /// Flaga określająca, czy gracz stał na ziemi w poprzedniej klatce.
    /// Używana do wykrywania momentu lądowania po skoku.
    /// </summary>
    [Header("Landing Bob")]
    private bool wasGrounded = true;

    /// <summary>
    /// Pozostały czas animacji efektu lądowania (w sekundach).
    /// Ustawiany na 0.2s po wykryciu lądowania.
    /// </summary>
    private float landingBobTimer;

    /// <summary>
    /// Bazowa pozycja lokalna kamery zapamiętana przy inicjalizacji.
    /// Wszystkie efekty kamery są stosowane jako offset od tej pozycji.
    /// </summary>
    private Vector3 baseLocalPosition;

    /// <summary>
    /// Akumulator czasu kołysania głowy (fazy sinusoidy).
    /// Rośnie proporcjonalnie do czasu i częstotliwości kołysania.
    /// </summary>
    private float bobTimer;

    /// <summary>
    /// Referencja do śledzionego kontrolera postaci gracza.
    /// Używana do odczytu prędkości ruchu i stanu przyziemienia.
    /// </summary>
    private CharacterController trackedController;

    /// <summary>
    /// Metoda inicjalizacyjna Unity wywoływana przy tworzeniu obiektu.
    /// Implementuje wzorzec Singleton i zapamiętuje bazową pozycję lokalną kamery.
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        baseLocalPosition = transform.localPosition;
    }

    /// <summary>
    /// Metoda startowa Unity wywoływana przy pierwszej klatce.
    /// Tworzy nakładkę błyskową (flash overlay) na istniejącym Canvas.
    /// </summary>
    private void Start()
    {
        CreateFlashOverlay();
    }

    /// <summary>
    /// Ustawia śledzony kontroler postaci, z którego odczytywane są dane ruchu.
    /// Wywoływać z klasy SimplePlayerController, aby dostarczyć dane o prędkości i stanie gracza.
    /// </summary>
    /// <param name="controller">Kontroler postaci gracza do śledzenia.</param>
    public void SetTrackedController(CharacterController controller)
    {
        trackedController = controller;
    }

    /// <summary>
    /// Wyzwala efekt trzęsienia ekranu o podanej intensywności i czasie trwania.
    /// Przydatne przy zdarzeniach takich jak dostarczenie zamówienia lub krojenie składnika.
    /// </summary>
    /// <param name="intensity">Maksymalna intensywność trzęsienia (amplituda losowego przesunięcia).</param>
    /// <param name="duration">Czas trwania trzęsienia w sekundach (intensywność zanika liniowo).</param>
    public void ShakeCamera(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    /// <summary>
    /// Wyzwala błysk ekranu z podanym kolorem nakładki.
    /// Zielony = sukces dostarczenia, czerwony = porażka.
    /// Nakładka stopniowo zanika od przezroczystości 25% do pełnej przezroczystości.
    /// </summary>
    /// <param name="color">Kolor błysku ekranowego.</param>
    /// <param name="duration">Czas trwania błysku w sekundach (domyślnie 0.3s).</param>
    public void FlashScreen(Color color, float duration = 0.3f)
    {
        flashColor = color;
        flashDuration = duration;
        flashTimer = duration;
    }

    /// <summary>
    /// Metoda Unity wywoływana po wszystkich metodach Update.
    /// Oblicza sumaryczny offset kamery z trzech źródeł (kołysanie, trzęsienie, lądowanie)
    /// i stosuje go do pozycji lokalnej. Aktualizuje także nakładkę błyskową.
    /// </summary>
    private void LateUpdate()
    {
        Vector3 offset = Vector3.zero;

        offset += CalculateHeadBob();
        offset += CalculateShake();
        offset += CalculateLandingBob();

        transform.localPosition = baseLocalPosition + offset;

        UpdateFlashOverlay();
    }

    /// <summary>
    /// Oblicza przesunięcie kamery wynikające z kołysania głowy podczas chodzenia.
    /// Kołysanie aktywuje się tylko gdy prędkość pozioma postaci przekracza 0.5 jednostki/s.
    /// Używa sinusoidy z różnymi częstotliwościami dla osi X (połowa) i Y.
    /// </summary>
    /// <returns>Wektor przesunięcia kamery w osiach X i Y (Z zawsze 0).</returns>
    private Vector3 CalculateHeadBob()
    {
        if (trackedController == null)
        {
            return Vector3.zero;
        }

        Vector3 velocity = trackedController.velocity;
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

        if (horizontalSpeed < 0.5f)
        {

            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 4f);
            return Vector3.zero;
        }

        bobTimer += Time.deltaTime * bobFrequency;

        float bobY = Mathf.Sin(bobTimer) * bobAmplitudeY;
        float bobX = Mathf.Sin(bobTimer * 0.5f) * bobAmplitudeX;

        return new Vector3(bobX, bobY, 0f);
    }

    /// <summary>
    /// Oblicza przesunięcie kamery wynikające z trzęsienia ekranu.
    /// Intensywność trzęsienia maleje liniowo w czasie. Losowe przesunięcia
    /// generowane są w osiach X i Y.
    /// </summary>
    /// <returns>Wektor losowego przesunięcia kamery z zanikającą intensywnością.</returns>
    private Vector3 CalculateShake()
    {
        if (shakeTimer <= 0f)
        {
            return Vector3.zero;
        }

        shakeTimer -= Time.deltaTime;
        float progress = shakeTimer / shakeDuration;
        float decayingIntensity = shakeIntensity * progress;

        return new Vector3(
            Random.Range(-1f, 1f) * decayingIntensity,
            Random.Range(-1f, 1f) * decayingIntensity,
            0f);
    }

    /// <summary>
    /// Oblicza przesunięcie kamery wynikające z efektu lądowania po skoku.
    /// Wykrywa moment przejścia z stanu "w powietrzu" do "na ziemi" i wyzwala
    /// krótki ruch kamery w dół (dip), symulujący wstrząs przy lądowaniu.
    /// </summary>
    /// <returns>Wektor przesunięcia kamery w dół (oś Y), zanikający sinusoidalnie w ciągu 0.2s.</returns>
    private Vector3 CalculateLandingBob()
    {
        if (trackedController != null)
        {
            bool grounded = trackedController.isGrounded;
            if (grounded && !wasGrounded)
            {
                landingBobTimer = 0.2f;
            }

            wasGrounded = grounded;
        }

        if (landingBobTimer <= 0f)
        {
            return Vector3.zero;
        }

        landingBobTimer -= Time.deltaTime;
        float t = landingBobTimer / 0.2f;
        float dip = Mathf.Sin(t * Mathf.PI) * 0.06f;
        return new Vector3(0f, -dip, 0f);
    }

    /// <summary>
    /// Tworzy pełnoekranową nakładkę obrazu (Image) do efektu błysku ekranowego.
    /// Wyszukuje istniejący Canvas typu ScreenSpaceOverlay i dodaje do niego
    /// rozciągnięty na cały ekran obiekt Image z wyłączonym raycastem.
    /// </summary>
    /// <remarks>
    /// Jeśli nie zostanie znaleziony odpowiedni Canvas, metoda kończy się bez tworzenia nakładki
    /// i efekty błysku ekranowego nie będą działać.
    /// </remarks>
    private void CreateFlashOverlay()
    {

        Canvas canvas = null;
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas c in allCanvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = c;
                break;
            }
        }

        if (canvas == null)
        {
            return;
        }

        GameObject flashObj = new GameObject("ScreenFlash");
        flashObj.transform.SetParent(canvas.transform, false);

        flashOverlay = flashObj.AddComponent<Image>();
        flashOverlay.color = new Color(0f, 0f, 0f, 0f);
        flashOverlay.raycastTarget = false;

        RectTransform rect = flashObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Aktualizuje przezroczystość nakładki błyskowej ekranowej.
    /// Przezroczystość maleje od 25% do 0% w trakcie trwania błysku.
    /// Po zakończeniu błysku nakładka staje się w pełni przezroczysta.
    /// </summary>
    private void UpdateFlashOverlay()
    {
        if (flashOverlay == null || flashTimer <= 0f)
        {
            return;
        }

        flashTimer -= Time.deltaTime;
        float alpha = Mathf.Clamp01(flashTimer / flashDuration) * 0.25f;
        flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);

        if (flashTimer <= 0f)
        {
            flashOverlay.color = new Color(0f, 0f, 0f, 0f);
        }
    }

    /// <summary>
    /// Metoda Unity wywoływana przy niszczeniu obiektu.
    /// Czyści statyczną referencję Singletona, aby uniknąć wiszących wskaźników.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
