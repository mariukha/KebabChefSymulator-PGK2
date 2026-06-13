using UnityEngine;

/// <summary>
/// Adds subtle flickering to kitchen lights for a warm, natural ambiance.
/// Simulates realistic lamp behavior with slow intensity oscillation and
/// occasional micro-flickers.
/// </summary>
public class LampFlicker : MonoBehaviour
{
    [SerializeField] private float baseIntensity = 5.5f;
    [SerializeField] private float flickerAmplitude = 0.25f;
    [SerializeField] private float flickerSpeed = 2.2f;
    [SerializeField] private float microFlickerChance = 0.008f;
    [SerializeField] private float microFlickerDrop = 0.4f;

    private Light targetLight;
    private float phaseOffset;
    private float microFlickerTimer;

    public void Configure(Light light, float baseIntensity)
    {
        targetLight = light;
        this.baseIntensity = baseIntensity;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

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
/// Adds warm emission pulse to lamp renderers, making them glow rhythmically.
/// Works in tandem with LampFlicker for a cohesive lighting effect.
/// </summary>
public class LampEmissionPulse : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 1.8f;
    [SerializeField] private float minEmission = 0.3f;
    [SerializeField] private float maxEmission = 0.65f;
    [SerializeField] private Color emissionColor = new Color(1f, 0.78f, 0.35f);

    private Renderer[] targetRenderers;
    private float phaseOffset;
    private MaterialPropertyBlock propBlock;

    private void Start()
    {
        targetRenderers = GetComponentsInChildren<Renderer>();
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        propBlock = new MaterialPropertyBlock();
    }

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
