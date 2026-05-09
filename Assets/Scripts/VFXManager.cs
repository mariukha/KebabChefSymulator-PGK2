using UnityEngine;

/// <summary>
/// Centralny manager efektów wizualnych (VFX).
/// Tworzy systemy cząsteczkowe programistycznie — bez prefabów.
/// Używany przez KitchenStation i KitchenHUD do wizualnego feedbacku.
/// 
/// Dostępne efekty:
///   - Para z grilla (Steam)             — biały dym unoszący się podczas pieczenia
///   - Krojenie warzyw (Chop)            — kolorowe odłamki przy desce do krojenia
///   - Zarobione pieniądze (Money)       — złote cząsteczki przy kasie/ekranie
///   - Udana dostawa (DeliverySuccess)   — zielony rozbłysk + cząsteczki
///   - Nieudana dostawa (DeliveryFail)   — czerwony rozbłysk
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    // =========================================================================
    //  PUBLIC API — inne skrypty wywołują te metody
    // =========================================================================

    /// <summary>
    /// Efekt pary/dymu nad stacją grillową.
    /// Wywoływany przez KitchenStation przy rozpoczęciu pieczenia.
    /// </summary>
    public void PlaySteamEffect(Vector3 worldPosition)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_Steam", worldPosition + Vector3.up * 1.4f);

        var main = ps.main;
        main.duration = 3f;
        main.loop = true;
        main.startLifetime = 1.8f;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startColor = new Color(0.85f, 0.88f, 0.92f, 0.25f);
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.08f;

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.35f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.9f, 0.92f, 0.95f), 0f),
                new GradientColorKey(new Color(0.8f, 0.82f, 0.85f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.3f, 0.15f),
                new GradientAlphaKey(0.25f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(1f, 1.5f)));

        ps.Play();
    }

    /// <summary>
    /// Zatrzymuje efekt pary na danej pozycji (szuka aktywnego systemu).
    /// </summary>
    public void StopSteamEffect(Vector3 worldPosition)
    {
        StopEffectNear("VFX_Steam", worldPosition, 2.0f);
    }

    /// <summary>
    /// Efekt krojenia — kolorowe odpryski warzyw.
    /// Wywoływany przy rozpoczęciu krojenia na desce.
    /// </summary>
    public void PlayChopEffect(Vector3 worldPosition, Color ingredientColor)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_Chop", worldPosition + Vector3.up * 1.2f);

        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = ingredientColor;
        main.maxParticles = 25;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.5f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 15, 25)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.12f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(ingredientColor, 0f),
                new GradientColorKey(ingredientColor * 0.7f, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = grad;

        ps.Play();
        Destroy(ps.gameObject, 1.5f);
    }

    /// <summary>
    /// Efekt zarobienia pieniędzy — złote cząsteczki wznoszące się.
    /// Wywoływany przy udanej dostawie kebaba.
    /// </summary>
    public void PlayMoneyEffect(Vector3 worldPosition)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_Money", worldPosition + Vector3.up * 1.0f);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
        main.startColor = new Color(1f, 0.85f, 0.2f, 0.9f);
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 18, 30)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.92f, 0.35f), 0f),
                new GradientColorKey(new Color(1f, 0.75f, 0.1f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.8f),
            new Keyframe(0.3f, 1.2f),
            new Keyframe(1f, 0.2f)));

        ps.Play();
        Destroy(ps.gameObject, 2.5f);
    }

    /// <summary>
    /// Zielony rozbłysk przy udanej dostawie zamówienia.
    /// </summary>
    public void PlayDeliverySuccessEffect(Vector3 worldPosition)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_DeliveryOK", worldPosition + Vector3.up * 1.5f);

        var main = ps.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new Color(0.2f, 0.9f, 0.35f, 0.85f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.4f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 30, 50)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.3f, 1f, 0.5f), 0f),
                new GradientColorKey(new Color(0.15f, 0.7f, 0.3f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = grad;

        ps.Play();
        Destroy(ps.gameObject, 2f);
    }

    /// <summary>
    /// Czerwony rozbłysk przy nieudanej dostawie.
    /// </summary>
    public void PlayDeliveryFailEffect(Vector3 worldPosition)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_DeliveryFail", worldPosition + Vector3.up * 1.5f);

        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = new Color(0.95f, 0.2f, 0.15f, 0.8f);
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 20, 30)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.3f, 0.2f), 0f),
                new GradientColorKey(new Color(0.6f, 0.1f, 0.08f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = grad;

        ps.Play();
        Destroy(ps.gameObject, 1.5f);
    }

    // =========================================================================
    //  INTERNAL HELPERS
    // =========================================================================

    /// <summary>
    /// Tworzy nowy GameObject z ParticleSystem i materiałem particle.
    /// Ustawia pozycję w przestrzeni świata.
    /// </summary>
    private ParticleSystem CreateParticleSystem(string effectName, Vector3 worldPosition)
    {
        GameObject effectObject = new GameObject(effectName);
        effectObject.transform.position = worldPosition;

        ParticleSystem ps = effectObject.AddComponent<ParticleSystem>();

        // Domyślny materiał cząsteczkowy (addytywny, miękki)
        ParticleSystemRenderer psRenderer = effectObject.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = CreateParticleMaterial();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        // Wyłącz domyślną emisję — każdy efekt konfiguruje własną
        var emission = ps.emission;
        emission.rateOverTime = 0f;

        return ps;
    }

    /// <summary>
    /// Tworzy materiał addytywny dla cząsteczek.
    /// Używa wbudowanego shadera Particles/Standard Unlit.
    /// </summary>
    private Material CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Particles/Additive");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        if (shader == null)
        {
            // Ostatni fallback — standardowy shader
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = Color.white;

        // Konfiguracja renderowania transparentnego
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f); // Transparent
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 1f); // Additive
        }

        material.renderQueue = 3100;
        return material;
    }

    /// <summary>
    /// Znajduje i zatrzymuje efekt cząsteczkowy o podanej nazwie w promieniu od pozycji.
    /// Używane do zatrzymywania pętlowych efektów (np. para z grilla).
    /// </summary>
    private void StopEffectNear(string effectName, Vector3 position, float maxDistance)
    {
        ParticleSystem[] allSystems = FindObjectsByType<ParticleSystem>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (ParticleSystem ps in allSystems)
        {
            if (ps == null || ps.gameObject == null)
            {
                continue;
            }

            if (ps.gameObject.name != effectName)
            {
                continue;
            }

            float distance = Vector3.Distance(ps.transform.position, position + Vector3.up * 1.4f);
            if (distance <= maxDistance)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(ps.gameObject, 2f);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
