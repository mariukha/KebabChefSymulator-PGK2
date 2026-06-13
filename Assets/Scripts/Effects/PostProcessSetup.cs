using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Creates a URP post-processing Volume at runtime with Bloom, Vignette,
/// Color Adjustments, Tonemapping, and Film Grain for a warm, cinematic kitchen feel.
/// Attach to any persistent GameObject or let KitchenGameBootstrap create it.
/// </summary>
public class PostProcessSetup : MonoBehaviour
{
    public static PostProcessSetup Instance { get; private set; }

    private Volume volume;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SetupVolume();
    }

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
    /// Apply a brief intensity boost (e.g. on successful delivery).
    /// </summary>
    public void PulseBloom(float extraIntensity = 0.5f, float duration = 0.4f)
    {
        StartCoroutine(BloomPulseCoroutine(extraIntensity, duration));
    }

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
