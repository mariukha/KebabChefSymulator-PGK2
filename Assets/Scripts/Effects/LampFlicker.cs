/// \file LampFlicker.cs
/// \brief Plik zawierający klasy LampFlicker i LampEmissionPulse — system realistycznego migotania lamp.
/// \details Implementuje subtelne migotanie oświetlenia kuchennego dla ciepłej, naturalnej atmosfery.
/// LampFlicker steruje intensywnością źródła światła, a LampEmissionPulse pulsacją emisji rendererów.

using UnityEngine;

/// <summary>
/// Dodaje subtelne migotanie do lamp kuchennych, tworząc ciepłą, naturalną atmosferę.
/// Symuluje realistyczne zachowanie lampy z powolną oscylacją intensywności
/// oraz sporadycznymi mikromigotaniami (chwilowe spadki jasności).
/// </summary>
/// <remarks>
/// Migotanie opiera się na złożeniu dwóch fal sinusoidalnych o różnych częstotliwościach
/// (stosunek 1:1.7), co tworzy nieregularny, organiczny wzorzec. Mikromigotania to losowe,
/// krótkie (0.08s) spadki intensywności występujące z konfigurowalnym prawdopodobieństwem.
/// Przesunięcie fazowe jest losowane przy starcie, dzięki czemu wiele lamp
/// migocze niesynchronicznie.
/// </remarks>
public class LampFlicker : MonoBehaviour
{
    /// <summary>
    /// Bazowa intensywność światła, wokół której oscyluje migotanie.
    /// Domyślna wartość 5.5 odpowiada typowemu oświetleniu kuchennemu.
    /// </summary>
    [SerializeField] private float baseIntensity = 5.5f;

    /// <summary>
    /// Amplituda oscylacji intensywności światła.
    /// Określa maksymalne odchylenie jasności od wartości bazowej w obu kierunkach.
    /// </summary>
    [SerializeField] private float flickerAmplitude = 0.25f;

    /// <summary>
    /// Prędkość głównej oscylacji migotania (w radianach na sekundę, pomnożona przez Time.time).
    /// Wyższe wartości powodują szybsze migotanie.
    /// </summary>
    [SerializeField] private float flickerSpeed = 2.2f;

    /// <summary>
    /// Prawdopodobieństwo wystąpienia mikromigotania w każdej klatce.
    /// Wartość 0.008 oznacza około 0.8% szans na mikromigotanie w każdej klatce.
    /// </summary>
    [SerializeField] private float microFlickerChance = 0.008f;

    /// <summary>
    /// Wielkość spadku intensywności podczas mikromigotania.
    /// Wartość jest odejmowana od bieżącej intensywności, tworząc chwilowe przyciemnienie.
    /// </summary>
    [SerializeField] private float microFlickerDrop = 0.4f;

    /// <summary>
    /// Referencja do komponentu Light, którego intensywność jest animowana.
    /// Może być ustawiona ręcznie przez <see cref="Configure"/> lub automatycznie pobrana w Start.
    /// </summary>
    private Light targetLight;

    /// <summary>
    /// Losowe przesunięcie fazowe oscylacji, zapewniające niesynchroniczne migotanie
    /// różnych lamp na scenie. Losowane w zakresie [0, 2π].
    /// </summary>
    private float phaseOffset;

    /// <summary>
    /// Pozostały czas trwania bieżącego mikromigotania (w sekundach).
    /// Wartość > 0 oznacza aktywne mikromigotanie z malejącą intensywnością.
    /// </summary>
    private float microFlickerTimer;

    /// <summary>
    /// Konfiguruje migotanie lampy programowo z zewnętrznego kodu.
    /// Ustawia docelowe źródło światła i bazową intensywność, oraz losuje przesunięcie fazowe.
    /// </summary>
    /// <param name="light">Komponent Light, którego intensywność ma być animowana.</param>
    /// <param name="baseIntensity">Bazowa intensywność światła (punkt centralny oscylacji).</param>
    public void Configure(Light light, float baseIntensity)
    {
        targetLight = light;
        this.baseIntensity = baseIntensity;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    /// <summary>
    /// Metoda startowa Unity wywoływana przy pierwszej klatce.
    /// Automatycznie pobiera komponent Light z bieżącego obiektu, jeśli nie został
    /// skonfigurowany wcześniej przez <see cref="Configure"/>. Odczytuje bazową intensywność
    /// z komponentu Light i losuje przesunięcie fazowe.
    /// </summary>
    private void Start()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }

        if (targetLight != null)
        {
            baseIntensity = targetLight.intensity;
        }

        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    /// <summary>
    /// Metoda Unity wywoływana co klatkę.
    /// Oblicza nową intensywność światła na podstawie złożenia dwóch fal sinusoidalnych
    /// (o stosunku częstotliwości 1:1.7 i wagach 0.6:0.4). Obsługuje także mikromigotania —
    /// losowe, krótkie spadki jasności. Intensywność nigdy nie spada poniżej 0.1.
    /// </summary>
    private void Update()
    {
        if (targetLight == null)
        {
            return;
        }

        float wave1 = Mathf.Sin(Time.time * flickerSpeed + phaseOffset);
        float wave2 = Mathf.Sin(Time.time * flickerSpeed * 1.7f + phaseOffset * 0.5f);
        float combinedWave = (wave1 * 0.6f + wave2 * 0.4f) * flickerAmplitude;

        float intensity = baseIntensity + combinedWave;

        if (microFlickerTimer > 0f)
        {
            microFlickerTimer -= Time.deltaTime;
            float flickerProgress = microFlickerTimer / 0.08f;
            intensity -= microFlickerDrop * flickerProgress;
        }
        else if (Random.value < microFlickerChance)
        {
            microFlickerTimer = 0.08f;
        }

        targetLight.intensity = Mathf.Max(intensity, 0.1f);
    }
}

/// <summary>
/// Dodaje ciepłą pulsację emisji do rendererów lamp, sprawiając że świecą rytmicznie.
/// Działa w tandemie z <see cref="LampFlicker"/> dla spójnego efektu oświetleniowego.
/// </summary>
/// <remarks>
/// Pulsacja emisji jest realizowana przez sinusoidalną interpolację koloru emisji
/// między minimalną a maksymalną intensywnością. Przesunięcie fazowe jest losowane
/// przy starcie, zapewniając niesynchroniczne pulsowanie różnych lamp.
/// Modyfikacje wykonywane są przez <see cref="MaterialPropertyBlock"/>,
/// co nie wpływa na współdzielone materiały.
/// </remarks>
public class LampEmissionPulse : MonoBehaviour
{
    /// <summary>
    /// Prędkość pulsacji emisji (w radianach na sekundę, pomnożona przez Time.time).
    /// Wyższe wartości powodują szybszą pulsację.
    /// </summary>
    [SerializeField] private float pulseSpeed = 1.8f;

    /// <summary>
    /// Minimalna intensywność emisji (najciemniejszy punkt pulsacji).
    /// </summary>
    [SerializeField] private float minEmission = 0.3f;

    /// <summary>
    /// Maksymalna intensywność emisji (najjaśniejszy punkt pulsacji).
    /// </summary>
    [SerializeField] private float maxEmission = 0.65f;

    /// <summary>
    /// Bazowy kolor emisji lampy — ciepły, złoto-pomarańczowy odcień
    /// nawiązujący do tradycyjnego oświetlenia kuchni kebabowej.
    /// </summary>
    [SerializeField] private Color emissionColor = new Color(1f, 0.78f, 0.35f);

    /// <summary>
    /// Tablica rendererów lampy i jej obiektów podrzędnych.
    /// Emisja jest stosowana na wszystkich znalezionych rendererach.
    /// </summary>
    private Renderer[] targetRenderers;

    /// <summary>
    /// Losowe przesunięcie fazowe pulsacji, zapewniające niesynchroniczne pulsowanie
    /// różnych lamp na scenie. Losowane w zakresie [0, 2π].
    /// </summary>
    private float phaseOffset;

    /// <summary>
    /// Blok właściwości materiału używany do modyfikacji koloru emisji rendererów
    /// bez tworzenia nowych instancji materiałów.
    /// </summary>
    private MaterialPropertyBlock propBlock;

    /// <summary>
    /// Metoda startowa Unity wywoływana przy pierwszej klatce.
    /// Pobiera wszystkie renderery z bieżącego obiektu i jego dzieci,
    /// losuje przesunięcie fazowe i inicjalizuje blok właściwości materiału.
    /// </summary>
    private void Start()
    {
        targetRenderers = GetComponentsInChildren<Renderer>();
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        propBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Metoda Unity wywoływana co klatkę.
    /// Oblicza bieżącą intensywność emisji na podstawie sinusoidy znormalizowanej do zakresu [0,1],
    /// interpoluje między minimalną a maksymalną emisją, mnoży przez kolor bazowy
    /// i stosuje wynik na wszystkich rendererach lampy.
    /// </summary>
    private void Update()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            return;
        }

        float t = (Mathf.Sin(Time.time * pulseSpeed + phaseOffset) + 1f) * 0.5f;
        float emissionIntensity = Mathf.Lerp(minEmission, maxEmission, t);
        Color finalEmission = emissionColor * emissionIntensity;

        foreach (Renderer rend in targetRenderers)
        {
            if (rend == null)
            {
                continue;
            }

            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor("_EmissionColor", finalEmission);
            rend.SetPropertyBlock(propBlock);
        }
    }
}
