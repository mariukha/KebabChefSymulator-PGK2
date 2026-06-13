/// \file PostProcessSetup.cs
/// \brief Plik zawierający klasę PostProcessSetup — konfiguracja post-processingu URP w czasie wykonania.
/// \details Tworzy globalny Volume URP z efektami Bloom, Vignette, Color Adjustments, Tonemapping,
/// Film Grain i Chromatic Aberration dla ciepłej, kinowej atmosfery kuchni kebabowej.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tworzy Volume post-processingu URP w czasie wykonania z efektami Bloom, Vignette,
/// Color Adjustments, Tonemapping i Film Grain dla ciepłej, kinowej atmosfery kuchni.
/// Dołączyć do dowolnego trwałego obiektu gry lub pozwolić KitchenGameBootstrap go stworzyć.
/// </summary>
/// <remarks>
/// Klasa implementuje wzorzec Singleton. Volume jest tworzony jako globalny (isGlobal = true)
/// z priorytetem 10, nadpisując domyślne ustawienia renderowania. Profil Volume
/// tworzony jest proceduralnie (ScriptableObject.CreateInstance), bez potrzeby
/// przygotowywania zasobów w edytorze.
/// Dostępna jest metoda <see cref="PulseBloom"/> do chwilowego wzmocnienia efektu Bloom
/// (np. przy udanym dostarczeniu zamówienia).
/// </remarks>
public class PostProcessSetup : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singletona klasy <see cref="PostProcessSetup"/>.
    /// Umożliwia globalny dostęp do konfiguracji post-processingu.
    /// </summary>
    public static PostProcessSetup Instance { get; private set; }

    /// <summary>
    /// Referencja do komponentu Volume post-processingu URP.
    /// Tworzony proceduralnie w metodzie <see cref="SetupVolume"/>.
    /// </summary>
    private Volume volume;

    /// <summary>
    /// Metoda inicjalizacyjna Unity wywoływana przy tworzeniu obiektu.
    /// Implementuje wzorzec Singleton — ustawia instancję lub niszczy duplikat.
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Metoda startowa Unity wywoływana przy pierwszej klatce.
    /// Inicjalizuje Volume post-processingu z pełną konfiguracją efektów.
    /// </summary>
    private void Start()
    {
        SetupVolume();
    }

    /// <summary>
    /// Tworzy i konfiguruje globalny Volume post-processingu URP z następującymi efektami:
    /// <list type="bullet">
    /// <item><description>Bloom — delikatna poświata z ciepłym odcieniem (próg 0.92, intensywność 0.32)</description></item>
    /// <item><description>Vignette — przyciemnienie krawędzi ekranu (intensywność 0.15)</description></item>
    /// <item><description>Color Adjustments — korekcja kolorów z lekkim ociepleniem i zwiększeniem kontrastu</description></item>
    /// <item><description>Tonemapping — mapowanie tonalne ACES dla kinowego wyglądu</description></item>
    /// <item><description>Film Grain — subtelne ziarno filmowe dla tekstury obrazu</description></item>
    /// <item><description>Chromatic Aberration — delikatna aberracja chromatyczna na krawędziach</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Metoda nie wykonuje nic, jeśli Volume został już wcześniej utworzony.
    /// Po utworzeniu loguje informację diagnostyczną do konsoli Unity.
    /// </remarks>
    private void SetupVolume()
    {
        if (volume != null)
        {
            return;
        }

        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.92f);
        bloom.intensity.Override(0.32f);
        bloom.scatter.Override(0.62f);
        bloom.tint.Override(new Color(1f, 0.93f, 0.8f));

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.15f);
        vignette.smoothness.Override(0.48f);
        vignette.color.Override(new Color(0.04f, 0.02f, 0.01f));

        ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.postExposure.Override(0.18f);
        colorAdj.contrast.Override(7f);
        colorAdj.saturation.Override(6f);
        colorAdj.colorFilter.Override(new Color(1f, 0.98f, 0.95f));

        Tonemapping tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.ACES);

        FilmGrain filmGrain = profile.Add<FilmGrain>(true);
        filmGrain.type.Override(FilmGrainLookup.Thin1);
        filmGrain.intensity.Override(0.06f);
        filmGrain.response.Override(0.45f);

        ChromaticAberration chromaticAberration = profile.Add<ChromaticAberration>(true);
        chromaticAberration.intensity.Override(0.025f);

        Debug.Log("[PostProcessSetup] URP Volume created with Bloom, Vignette, Color Adjustments, Tonemapping, Film Grain, Chromatic Aberration.");
    }

    /// <summary>
    /// Wyzwala chwilowe wzmocnienie intensywności efektu Bloom (pulsacja).
    /// Intensywność Bloom rośnie do wartości szczytowej, a następnie płynnie wraca do wartości bazowej.
    /// Przydatne np. przy udanym dostarczeniu zamówienia, aby wzmocnić wrażenie nagrody.
    /// </summary>
    /// <param name="extraIntensity">Dodatkowa intensywność Bloom ponad wartość bazową (domyślnie 0.5).</param>
    /// <param name="duration">Czas trwania pulsacji w sekundach (domyślnie 0.4s).</param>
    public void PulseBloom(float extraIntensity = 0.5f, float duration = 0.4f)
    {
        StartCoroutine(BloomPulseCoroutine(extraIntensity, duration));
    }

    /// <summary>
    /// Korutyna obsługująca pulsację efektu Bloom.
    /// Intensywność zmienia się od wartości szczytowej (bazowa + extra) z powrotem do bazowej
    /// z krzywą wygaszania kwadratowego (ease-out), tworząc naturalne zanikanie efektu.
    /// </summary>
    /// <param name="extraIntensity">Dodatkowa intensywność Bloom ponad wartość bazową.</param>
    /// <param name="duration">Czas trwania pulsacji w sekundach.</param>
    /// <returns>Enumerator korutyny.</returns>
    private System.Collections.IEnumerator BloomPulseCoroutine(float extraIntensity, float duration)
    {
        if (volume == null || volume.profile == null)
        {
            yield break;
        }

        if (!volume.profile.TryGet(out Bloom bloom))
        {
            yield break;
        }

        float baseIntensity = 0.32f;
        float peakIntensity = baseIntensity + extraIntensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float ease = 1f - (t * t);
            bloom.intensity.Override(Mathf.Lerp(baseIntensity, peakIntensity, ease));
            yield return null;
        }

        bloom.intensity.Override(baseIntensity);
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
