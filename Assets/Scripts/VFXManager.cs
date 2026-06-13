using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    private Material cachedParticleMaterial;
    private Material cachedSmokeMaterial;
    private Texture2D cachedParticleTexture;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    
    public void PlaySteamEffect(Vector3 worldPosition)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.Steam, worldPosition);
        PlaySteamEffectLocal(worldPosition);
    }


    public void StopSteamEffect(Vector3 worldPosition)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.StopSteam, worldPosition);
        StopSteamEffectLocal(worldPosition);
    }


    public void PlayDonerSmokeEffect(Vector3 worldPosition)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.DonerSmoke, worldPosition);
        PlayDonerSmokeEffectLocal(worldPosition);
    }


    public void StopDonerSmokeEffect(Vector3 worldPosition)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.StopDonerSmoke, worldPosition);
        StopDonerSmokeEffectLocal(worldPosition);
    }


    public void PlayChopEffect(Vector3 worldPosition, Color ingredientColor)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.Chop, worldPosition, ingredientColor);
        PlayChopEffectLocal(worldPosition, ingredientColor);
    }


    public void PlayPickupEffect(Vector3 worldPosition, Color tint)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.Pickup, worldPosition, tint);
        PlayPickupEffectLocal(worldPosition, tint);
    }


    public void PlayDropEffect(Vector3 worldPosition)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.Drop, worldPosition);
        PlayDropEffectLocal(worldPosition);
    }


    public void PlayReadyEffect(Vector3 worldPosition, Color tint)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.Ready, worldPosition, tint);
        PlayReadyEffectLocal(worldPosition, tint);
    }


    public void PlayWrapEffect(Vector3 worldPosition)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.Wrap, worldPosition);
        PlayWrapEffectLocal(worldPosition);
    }


    public void PlayUpgradeEffect(Vector3 worldPosition, Color accent)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.Upgrade, worldPosition, accent);
        PlayUpgradeEffectLocal(worldPosition, accent);
    }


    public void PlayMoneyEffect(Vector3 worldPosition)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.Money, worldPosition);
        PlayMoneyEffectLocal(worldPosition);
    }


    public void PlayDeliverySuccessEffect(Vector3 worldPosition)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.DeliverySuccess, worldPosition);
        PlayDeliverySuccessEffectLocal(worldPosition);
    }


    public void PlayDeliveryFailEffect(Vector3 worldPosition)
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.DeliveryFail, worldPosition);
        PlayDeliveryFailEffectLocal(worldPosition);
    }


    public void PlayTimeoutEffect()
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer && NetworkPlayer.LocalInstance != null)
            NetworkPlayer.LocalInstance.BroadcastVFX(NetworkVFXType.Timeout, Vector3.zero);
        PlayTimeoutEffectLocal();
    }

public void PlaySteamEffectLocal(Vector3 worldPosition)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_Steam", worldPosition + Vector3.up * 1.25f);

        var main = ps.main;
        main.duration = 4f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.16f, 0.42f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.56f);
        main.startColor = new Color(0.82f, 0.86f, 0.9f, 0.20f);
        main.maxParticles = 90;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.09f;

        var emission = ps.emission;
        emission.rateOverTime = 18f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.38f;
        shape.randomDirectionAmount = 0.16f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

        ApplyColorGradient(ps,
            new Color(0.92f, 0.95f, 1f),
            new Color(0.56f, 0.60f, 0.64f),
            0f, 0.28f, 0f);
        ApplySizeCurve(ps,
            new Keyframe(0f, 0.45f),
            new Keyframe(0.28f, 1.0f),
            new Keyframe(1f, 1.85f));
        ApplyNoise(ps, 0.09f, 0.55f);

        ps.Play();
    }

    public void StopSteamEffectLocal(Vector3 worldPosition)
    {
        StopEffectNear("VFX_Steam", worldPosition, 2.0f);
    }

    public void PlayDonerSmokeEffectLocal(Vector3 worldPosition)
    {
        Vector3 basePosition = worldPosition + Vector3.up * 1.08f;
        ParticleSystem smoke = CreateParticleSystem("VFX_DonerSmoke", basePosition, true);

        var main = smoke.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 3.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.025f, 0.12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.26f, 0.68f);
        main.startColor = new Color(0.34f, 0.34f, 0.34f, 0.10f);
        main.maxParticles = 42;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.035f;

        var emission = smoke.emission;
        emission.rateOverTime = 5f;

        var shape = smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.17f;
        shape.randomDirectionAmount = 0.32f;

        var velocity = smoke.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.11f, 0.25f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

        var rotation = smoke.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-0.28f, 0.28f);

        ApplyColorGradient(smoke,
            new Color(0.48f, 0.48f, 0.47f),
            new Color(0.20f, 0.20f, 0.20f),
            0f, 0.10f, 0f);
        ApplySizeCurve(smoke,
            new Keyframe(0f, 0.20f),
            new Keyframe(0.28f, 0.70f),
            new Keyframe(0.72f, 1.05f),
            new Keyframe(1f, 1.25f));
        ApplyNoise(smoke, 0.11f, 0.34f);

        smoke.Play();

        ParticleSystem oilVapor = CreateParticleSystem("VFX_DonerOilyVapor", basePosition + Vector3.up * 0.08f);
        var vaporMain = oilVapor.main;
        vaporMain.duration = 5f;
        vaporMain.loop = true;
        vaporMain.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        vaporMain.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        vaporMain.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.045f);
        vaporMain.startColor = new Color(0.78f, 0.72f, 0.62f, 0.14f);
        vaporMain.maxParticles = 14;
        vaporMain.simulationSpace = ParticleSystemSimulationSpace.World;
        vaporMain.gravityModifier = -0.08f;

        var vaporEmission = oilVapor.emission;
        vaporEmission.rateOverTime = 1.5f;

        var vaporShape = oilVapor.shape;
        vaporShape.shapeType = ParticleSystemShapeType.Circle;
        vaporShape.radius = 0.12f;
        vaporShape.randomDirectionAmount = 0.22f;

        var vaporVelocity = oilVapor.velocityOverLifetime;
        vaporVelocity.enabled = true;
        vaporVelocity.x = new ParticleSystem.MinMaxCurve(-0.035f, 0.035f);
        vaporVelocity.y = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        vaporVelocity.z = new ParticleSystem.MinMaxCurve(-0.025f, 0.025f);

        ApplyColorGradient(oilVapor,
            new Color(0.78f, 0.75f, 0.68f),
            new Color(0.34f, 0.32f, 0.30f),
            0f, 0.10f, 0f);
        ApplySizeCurve(oilVapor,
            new Keyframe(0f, 0.35f),
            new Keyframe(0.4f, 1f),
            new Keyframe(1f, 0.1f));
        ApplyNoise(oilVapor, 0.04f, 0.75f);

        oilVapor.Play();
    }

    public void StopDonerSmokeEffectLocal(Vector3 worldPosition)
    {
        StopEffectNear("VFX_DonerSmoke", worldPosition, 2.1f);
        StopEffectNear("VFX_DonerOilyVapor", worldPosition, 2.1f);
    }

    public void PlayChopEffectLocal(Vector3 worldPosition, Color ingredientColor)
    {
        ParticleSystem bits = CreateParticleSystem("VFX_ChopBits", worldPosition + Vector3.up * 0.92f);

        var main = bits.main;
        main.duration = 0.42f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.26f, 0.62f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 2.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.052f);
        main.startColor = WithAlpha(ingredientColor, 0.9f);
        main.maxParticles = 34;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.25f;

        ConfigureBurst(bits, 14, 26);

        var shape = bits.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.13f;
        shape.randomDirectionAmount = 0.35f;

        ApplyColorGradient(bits, ingredientColor, ingredientColor * 0.68f, 0.9f, 0.65f, 0f);
        ApplySizeCurve(bits,
            new Keyframe(0f, 0.75f),
            new Keyframe(0.2f, 1.15f),
            new Keyframe(1f, 0.18f));

        bits.Play();
        Destroy(bits.gameObject, 1.4f);

        ParticleSystem slash = CreateParticleSystem("VFX_ChopFlash", worldPosition + Vector3.up * 0.98f);
        var slashMain = slash.main;
        slashMain.duration = 0.12f;
        slashMain.loop = false;
        slashMain.startLifetime = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
        slashMain.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
        slashMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
        slashMain.startColor = new Color(1f, 0.92f, 0.72f, 0.65f);
        slashMain.maxParticles = 12;
        slashMain.simulationSpace = ParticleSystemSimulationSpace.World;

        ConfigureBurst(slash, 6, 10);
        var slashShape = slash.shape;
        slashShape.shapeType = ParticleSystemShapeType.Circle;
        slashShape.radius = 0.10f;
        ApplyColorGradient(slash, new Color(1f, 0.95f, 0.72f), new Color(1f, 0.62f, 0.24f), 0.65f, 0.32f, 0f);
        ApplySizeCurve(slash, new Keyframe(0f, 1f), new Keyframe(1f, 0.05f));

        slash.Play();
        Destroy(slash.gameObject, 0.45f);
    }

    public void PlayPickupEffectLocal(Vector3 worldPosition, Color tint)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_Pickup", worldPosition + Vector3.up * 0.66f);

        var main = ps.main;
        main.duration = 0.22f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.20f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.24f, 0.82f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
        main.startColor = WithAlpha(Color.Lerp(tint, Color.white, 0.28f), 0.75f);
        main.maxParticles = 18;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.18f;

        ConfigureBurst(ps, 8, 14);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.10f;
        shape.randomDirectionAmount = 0.4f;

        ApplyColorGradient(ps, Color.Lerp(tint, Color.white, 0.35f), tint * 0.85f, 0.75f, 0.38f, 0f);
        ApplySizeCurve(ps, new Keyframe(0f, 0.55f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.12f));
        ps.Play();
        Destroy(ps.gameObject, 0.8f);
    }

    public void PlayDropEffectLocal(Vector3 worldPosition)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_DropDust", worldPosition + Vector3.up * 0.22f);

        var main = ps.main;
        main.duration = 0.24f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.72f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
        main.startColor = new Color(0.55f, 0.50f, 0.43f, 0.35f);
        main.maxParticles = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.18f;

        ConfigureBurst(ps, 9, 16);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.16f;
        shape.randomDirectionAmount = 0.5f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ApplyColorGradient(ps, new Color(0.58f, 0.52f, 0.44f), new Color(0.26f, 0.24f, 0.22f), 0.38f, 0.22f, 0f);
        ApplySizeCurve(ps, new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.35f));
        ps.Play();
        Destroy(ps.gameObject, 1.0f);
    }

    public void PlayReadyEffectLocal(Vector3 worldPosition, Color tint)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_Ready", worldPosition + Vector3.up * 0.95f);

        var main = ps.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.88f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.65f, 1.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
        main.startColor = WithAlpha(Color.Lerp(tint, new Color(1f, 0.84f, 0.34f), 0.45f), 0.85f);
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.12f;

        ConfigureBurst(ps, 14, 24);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.22f;
        shape.randomDirectionAmount = 0.28f;

        ApplyColorGradient(ps,
            Color.Lerp(tint, new Color(1f, 0.88f, 0.35f), 0.55f),
            new Color(0.35f, 0.95f, 0.45f),
            0.85f, 0.55f, 0f);
        ApplySizeCurve(ps,
            new Keyframe(0f, 0.35f),
            new Keyframe(0.18f, 1.25f),
            new Keyframe(1f, 0.10f));

        ps.Play();
        Destroy(ps.gameObject, 1.4f);
    }

    public void PlayWrapEffectLocal(Vector3 worldPosition)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_Wrap", worldPosition + Vector3.up * 0.9f);

        var main = ps.main;
        main.duration = 0.32f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.62f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
        main.startColor = new Color(0.95f, 0.77f, 0.48f, 0.72f);
        main.maxParticles = 28;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.04f;

        ConfigureBurst(ps, 14, 22);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.28f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-2.8f, 2.8f);

        ApplyColorGradient(ps, new Color(1f, 0.84f, 0.50f), new Color(0.78f, 0.52f, 0.28f), 0.75f, 0.4f, 0f);
        ApplySizeCurve(ps, new Keyframe(0f, 0.65f), new Keyframe(0.25f, 1.2f), new Keyframe(1f, 0.08f));
        ps.Play();
        Destroy(ps.gameObject, 1.2f);
    }

    public void PlayUpgradeEffectLocal(Vector3 worldPosition, Color accent)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_Upgrade", worldPosition);

        var main = ps.main;
        main.duration = 0.55f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.05f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.13f);
        main.startColor = WithAlpha(Color.Lerp(accent, new Color(1f, 0.82f, 0.28f), 0.45f), 0.95f);
        main.maxParticles = 52;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.2f;

        ConfigureBurst(ps, 26, 42);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.28f;
        shape.randomDirectionAmount = 0.4f;

        ApplyColorGradient(ps,
            Color.Lerp(accent, Color.white, 0.35f),
            new Color(1f, 0.68f, 0.22f),
            0.95f, 0.62f, 0f);
        ApplySizeCurve(ps,
            new Keyframe(0f, 0.25f),
            new Keyframe(0.18f, 1.35f),
            new Keyframe(1f, 0.1f));
        ApplyNoise(ps, 0.08f, 1.2f);

        ps.Play();
        Destroy(ps.gameObject, 1.8f);
    }

    public void PlayMoneyEffectLocal(Vector3 worldPosition)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_Money", worldPosition + Vector3.up * 1.05f);

        var main = ps.main;
        main.duration = 0.45f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.72f, 1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 2.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.12f);
        main.startColor = new Color(1f, 0.82f, 0.18f, 0.92f);
        main.maxParticles = 42;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.24f;

        ConfigureBurst(ps, 22, 34);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.24f;
        shape.randomDirectionAmount = 0.34f;

        ApplyColorGradient(ps,
            new Color(1f, 0.92f, 0.35f),
            new Color(1f, 0.58f, 0.06f),
            0.92f, 0.76f, 0f);
        ApplySizeCurve(ps,
            new Keyframe(0f, 0.45f),
            new Keyframe(0.25f, 1.25f),
            new Keyframe(1f, 0.1f));
        ApplyNoise(ps, 0.05f, 1.4f);

        ps.Play();
        Destroy(ps.gameObject, 2.0f);
    }

    public void PlayDeliverySuccessEffectLocal(Vector3 worldPosition)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_DeliveryOK", worldPosition + Vector3.up * 1.28f);

        var main = ps.main;
        main.duration = 0.42f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.52f, 1.05f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.15f, 2.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
        main.startColor = new Color(0.22f, 0.9f, 0.42f, 0.85f);
        main.maxParticles = 58;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.15f;

        ConfigureBurst(ps, 30, 48);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.32f;
        shape.randomDirectionAmount = 0.46f;

        ApplyColorGradient(ps,
            new Color(0.46f, 1f, 0.58f),
            new Color(0.08f, 0.62f, 0.25f),
            0.88f, 0.55f, 0f);
        ApplySizeCurve(ps,
            new Keyframe(0f, 0.32f),
            new Keyframe(0.2f, 1.2f),
            new Keyframe(1f, 0.12f));

        ps.Play();
        Destroy(ps.gameObject, 1.8f);
    }

    public void PlayDeliveryFailEffectLocal(Vector3 worldPosition)
    {
        ParticleSystem ps = CreateParticleSystem("VFX_DeliveryFail", worldPosition + Vector3.up * 1.12f);

        var main = ps.main;
        main.duration = 0.38f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.38f, 0.82f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.52f, 1.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.15f);
        main.startColor = new Color(0.95f, 0.18f, 0.12f, 0.78f);
        main.maxParticles = 38;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.3f;

        ConfigureBurst(ps, 20, 32);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.22f;
        shape.randomDirectionAmount = 0.5f;

        ApplyColorGradient(ps,
            new Color(1f, 0.28f, 0.18f),
            new Color(0.34f, 0.04f, 0.03f),
            0.82f, 0.55f, 0f);
        ApplySizeCurve(ps,
            new Keyframe(0f, 0.5f),
            new Keyframe(0.2f, 1.35f),
            new Keyframe(1f, 0.18f));
        ApplyNoise(ps, 0.06f, 1.0f);

        ps.Play();
        Destroy(ps.gameObject, 1.4f);
    }

    public void PlayTimeoutEffectLocal()
    {
        Vector3 position = GetCameraFacingPosition(2.2f, -0.12f);
        PlayDeliveryFailEffect(position);
    }

    public Vector3 GetCameraFacingPosition(float distance = 2.0f, float verticalOffset = -0.1f)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return Vector3.up;
        }

        return camera.transform.position + camera.transform.forward * distance + Vector3.up * verticalOffset;
    }

    private ParticleSystem CreateParticleSystem(string effectName, Vector3 worldPosition)
    {
        return CreateParticleSystem(effectName, worldPosition, false);
    }

    private ParticleSystem CreateParticleSystem(string effectName, Vector3 worldPosition, bool useSmokeMaterial)
    {
        GameObject effectObject = new GameObject(effectName);
        effectObject.transform.position = worldPosition;

        ParticleSystem ps = effectObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystemRenderer psRenderer = effectObject.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = useSmokeMaterial ? CreateSmokeMaterial() : CreateParticleMaterial();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.minParticleSize = 0.001f;
        psRenderer.maxParticleSize = 0.28f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        return ps;
    }

    private void ConfigureBurst(ParticleSystem ps, short minCount, short maxCount)
    {
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, minCount, maxCount)
        });
    }

    private void ApplyColorGradient(ParticleSystem ps, Color start, Color end, float startAlpha, float midAlpha, float endAlpha)
    {
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(start, 0f),
                new GradientColorKey(Color.Lerp(start, end, 0.55f), 0.55f),
                new GradientColorKey(end, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(startAlpha, 0f),
                new GradientAlphaKey(midAlpha, 0.38f),
                new GradientAlphaKey(endAlpha, 1f)
            });

        colorOverLifetime.color = gradient;
    }

    private void ApplySizeCurve(ParticleSystem ps, params Keyframe[] keys)
    {
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));
    }

    private void ApplyNoise(ParticleSystem ps, float strength, float frequency)
    {
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = strength;
        noise.frequency = frequency;
        noise.scrollSpeed = 0.18f;
    }

    private Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    private Material CreateParticleMaterial()
    {
        if (cachedParticleMaterial != null)
        {
            return cachedParticleMaterial;
        }

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Particles/Additive");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        cachedParticleMaterial = new Material(shader);
        cachedParticleMaterial.color = Color.white;

        Texture2D texture = CreateSoftParticleTexture();
        if (cachedParticleMaterial.HasProperty("_MainTex"))
        {
            cachedParticleMaterial.SetTexture("_MainTex", texture);
        }

        if (cachedParticleMaterial.HasProperty("_BaseMap"))
        {
            cachedParticleMaterial.SetTexture("_BaseMap", texture);
        }

        if (cachedParticleMaterial.HasProperty("_Surface"))
        {
            cachedParticleMaterial.SetFloat("_Surface", 1f);
        }

        if (cachedParticleMaterial.HasProperty("_Blend"))
        {
            cachedParticleMaterial.SetFloat("_Blend", 1f);
        }

        cachedParticleMaterial.renderQueue = 3100;
        return cachedParticleMaterial;
    }

    private Material CreateSmokeMaterial()
    {
        if (cachedSmokeMaterial != null)
        {
            return cachedSmokeMaterial;
        }

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        cachedSmokeMaterial = new Material(shader);
        cachedSmokeMaterial.color = Color.white;

        Texture2D texture = CreateSoftParticleTexture();
        if (cachedSmokeMaterial.HasProperty("_MainTex"))
        {
            cachedSmokeMaterial.SetTexture("_MainTex", texture);
        }

        if (cachedSmokeMaterial.HasProperty("_BaseMap"))
        {
            cachedSmokeMaterial.SetTexture("_BaseMap", texture);
        }

        if (cachedSmokeMaterial.HasProperty("_Surface"))
        {
            cachedSmokeMaterial.SetFloat("_Surface", 1f);
        }

        if (cachedSmokeMaterial.HasProperty("_Blend"))
        {
            cachedSmokeMaterial.SetFloat("_Blend", 0f);
        }

        if (cachedSmokeMaterial.HasProperty("_Mode"))
        {
            cachedSmokeMaterial.SetFloat("_Mode", 2f);
        }

        cachedSmokeMaterial.renderQueue = 3000;
        return cachedSmokeMaterial;
    }

    private Texture2D CreateSoftParticleTexture()
    {
        if (cachedParticleTexture != null)
        {
            return cachedParticleTexture;
        }

        const int size = 64;
        cachedParticleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        cachedParticleTexture.name = "ProceduralSoftParticle";
        cachedParticleTexture.wrapMode = TextureWrapMode.Clamp;
        cachedParticleTexture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                alpha *= alpha;
                cachedParticleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        cachedParticleTexture.Apply();
        return cachedParticleTexture;
    }

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

            float distance = Vector3.Distance(ps.transform.position, position + Vector3.up * 1.25f);
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
