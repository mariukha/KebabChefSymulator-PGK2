using UnityEngine;

/// <summary>
/// Spawns subtle floating dust particles in the kitchen for ambient atmosphere.
/// Particles drift slowly through the air, catching light for a warm, lived-in feel.
/// </summary>
public class AmbientParticles : MonoBehaviour
{
    public static AmbientParticles Instance { get; private set; }

    private ParticleSystem dustSystem;

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
        CreateDustParticles();
    }

    private void CreateDustParticles()
    {
        GameObject dustObject = new GameObject("AmbientDust");
        dustObject.transform.SetParent(transform);
        dustObject.transform.localPosition = new Vector3(0f, 2.5f, 0f);

        dustSystem = dustObject.AddComponent<ParticleSystem>();
        dustSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = dustSystem.main;
        main.duration = 10f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 14f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.06f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.025f);
        main.startColor = new Color(1f, 0.95f, 0.85f, 0.18f);
        main.maxParticles = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.005f;

        var emission = dustSystem.emission;
        emission.rateOverTime = 8f;

        var shape = dustSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(10f, 4f, 10f);

        var velocityOverLifetime = dustSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.01f, 0.02f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);

        var colorOverLifetime = dustSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.96f, 0.88f), 0f),
                new GradientColorKey(new Color(1f, 0.94f, 0.82f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.18f, 0.2f),
                new GradientAlphaKey(0.22f, 0.5f),
                new GradientAlphaKey(0.15f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = dustSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.6f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0.4f)));

        var rotationOverLifetime = dustSystem.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

        ParticleSystemRenderer psRenderer = dustObject.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = CreateDustMaterial();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.minParticleSize = 0.001f;
        psRenderer.maxParticleSize = 0.008f;

        dustSystem.Play();
    }

    private Material CreateDustMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = new Color(1f, 0.97f, 0.9f, 0.25f);

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 1f);
        }

        material.renderQueue = 3050;
        return material;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
