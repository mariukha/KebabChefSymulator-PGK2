using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Linq;

public class KitchenGameBootstrap : MonoBehaviour
{
    private const int InteractableLayer = 6;
    private int stationIndexCounter = 0;
    private const string ModelPath = "Models/";
    private Shader cachedLitShader;
    private const float PlayerEyeHeight = 1.75f;
    private const float WorktopLocalY = 0.34f;
    private const float TableVisualSize = 2.35f;
    private const float TableDepthScale = 1.6f;
    private const float EntranceTableX = 2.75f;
    private const float EntranceTableZ = -4.58f;
    private const float EntranceTableTopY = 1.20f;
    private static readonly Vector3 PlayerSpawnPosition = new Vector3(0f, 0f, -1.9f);
    private static readonly Vector3 CustomerPosition = new Vector3(0f, 0f, -4.8f);
    private static readonly Vector3 CustomerLookTarget = new Vector3(0f, 1.55f, -4.8f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapScene()
    {
        if (FindFirstObjectByType<KitchenGameBootstrap>() != null)
        {
            return;
        }

        GameObject bootstrapper = new GameObject("KitchenBootstrap");
        bootstrapper.AddComponent<KitchenGameBootstrap>();
    }

    private Shader GetLitShader()
    {
        if (cachedLitShader != null) return cachedLitShader;
        cachedLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (cachedLitShader == null) cachedLitShader = Shader.Find("Standard");
        return cachedLitShader;
    }

    private void Start()
    {
        try { EnsureNetworkSetup(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] NetworkSetup failed: {e}"); }
        try { EnsureManagers(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] Managers failed: {e}"); }
        try { BuildEnvironmentIfNeeded(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] Environment failed: {e}"); }
        try { BuildKitchenIfNeeded(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] Kitchen failed: {e}"); }
        try { BuildOrderBoardIfNeeded(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] OrderBoard failed: {e}"); }
        try { ConfigureLighting(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] Lighting failed: {e}"); }
        try { EnsureEffects(); } catch (System.Exception e) { Debug.LogError($"[Bootstrap] Effects failed: {e}"); }

        try
        {
            bool networkActive = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (!networkActive)
            {

                if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject esObj = new GameObject("EventSystem");
                    esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }

                if (FindFirstObjectByType<MainMenuUI>() == null)
                {
                    new GameObject("MainMenuUI").AddComponent<MainMenuUI>();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Bootstrap] Post-init UI failed: {e}");
        }
    }

    private void EnsureNetworkSetup()
    {
        if (FindFirstObjectByType<NetworkSetup>() != null)
        {
            return;
        }

        GameObject networkObject = new GameObject("NetworkSetup");
        networkObject.AddComponent<NetworkSetup>();

        if (FindFirstObjectByType<LobbyUI>() == null)
        {
            GameObject lobbyObject = new GameObject("LobbyUI");
            lobbyObject.AddComponent<LobbyUI>();
        }
    }

    private void EnsureManagers()
    {
        GameObject managerObject = GameObject.Find("GameManager");
        if (managerObject == null)
        {
            managerObject = new GameObject("GameManager");
        }

        if (FindFirstObjectByType<EconomyManager>() == null)
        {
            managerObject.AddComponent<EconomyManager>();
        }

        OrderManager orderManager = FindFirstObjectByType<OrderManager>();
        if (orderManager == null)
        {
            orderManager = managerObject.AddComponent<OrderManager>();
        }

        if (FindFirstObjectByType<SaveManager>() == null)
        {
            managerObject.AddComponent<SaveManager>();
        }

        if (FindFirstObjectByType<GameSettingsManager>() == null)
        {
            managerObject.AddComponent<GameSettingsManager>();
        }

        if (FindFirstObjectByType<ShopManager>() == null)
        {
            managerObject.AddComponent<ShopManager>();
        }

        if (FindFirstObjectByType<VFXManager>() == null)
        {
            managerObject.AddComponent<VFXManager>();
        }

        if (FindFirstObjectByType<RelayManager>() == null)
        {
            managerObject.AddComponent<RelayManager>();
        }

        orderManager.InitializeCatalogIfNeeded();
    }

    private void EnsureEffects()
    {
        GameObject managerObject = GameObject.Find("GameManager");
        if (managerObject == null)
        {
            managerObject = new GameObject("GameManager");
        }

        if (FindFirstObjectByType<PostProcessSetup>() == null)
        {
            managerObject.AddComponent<PostProcessSetup>();
        }

        if (FindFirstObjectByType<AmbientParticles>() == null)
        {
            managerObject.AddComponent<AmbientParticles>();
        }

        if (FindFirstObjectByType<AudioManager>() == null)
        {
            managerObject.AddComponent<AudioManager>();
        }

        if (FindFirstObjectByType<ItemAnimator>() == null)
        {
            managerObject.AddComponent<ItemAnimator>();
        }

        if (FindFirstObjectByType<PauseMenuUI>() == null)
        {
            new GameObject("PauseMenuUI").AddComponent<PauseMenuUI>();
        }

        if (FindFirstObjectByType<LoadingScreen>() == null)
        {
            new GameObject("LoadingScreen").AddComponent<LoadingScreen>();
        }

        if (FindFirstObjectByType<AchievementPopup>() == null)
        {
            new GameObject("AchievementPopup").AddComponent<AchievementPopup>();
        }
    }

    private void BuildKitchenIfNeeded()
    {
        if (FindFirstObjectByType<KitchenStation>() != null)
        {
            return;
        }

        OrderManager orderManager = FindFirstObjectByType<OrderManager>();
        if (orderManager == null)
        {
            return;
        }

        Transform parent = new GameObject("RuntimeKitchen").transform;

        KitchenStation grillStation = CreateStation(parent, "Grill", KitchenStationType.Grill, new Vector3(-5.55f, 0.6f, 5.25f), new Color(0.3f, 0.3f, 0.35f), null, 4f);
        KitchenStation meatTrayStation = CreateStation(parent, "Mieso", KitchenStationType.IngredientSource, new Vector3(-5.55f, 0.6f, 3.95f), new Color(0.65f, 0.25f, 0.18f), orderManager.GetIngredientDefinition(IngredientKind.Meat), 0f);

        CreateStation(parent, "Pomidor", KitchenStationType.IngredientSource, new Vector3(-5.55f, 0.6f, 2.15f), new Color(0.86f, 0.2f, 0.2f), orderManager.GetIngredientDefinition(IngredientKind.Tomato), 0f);
        CreateStation(parent, "Cebula", KitchenStationType.IngredientSource, new Vector3(-5.55f, 0.6f, 0.85f), new Color(0.93f, 0.9f, 0.75f), orderManager.GetIngredientDefinition(IngredientKind.Onion), 0f);
        CreateStation(parent, "Salata", KitchenStationType.IngredientSource, new Vector3(-5.55f, 0.6f, -0.45f), new Color(0.35f, 0.7f, 0.25f), orderManager.GetIngredientDefinition(IngredientKind.Lettuce), 0f);

        CreateStation(parent, "Sos", KitchenStationType.IngredientSource, new Vector3(-5.55f, 0.6f, -1.75f), new Color(0.95f, 0.95f, 0.85f), orderManager.GetIngredientDefinition(IngredientKind.GarlicSauce), 0f);
        CreateStation(parent, "Lawasz", KitchenStationType.IngredientSource, new Vector3(-5.55f, 0.6f, -3.05f), new Color(0.86f, 0.74f, 0.5f), orderManager.GetIngredientDefinition(IngredientKind.Lavash), 0f);

        CreateStation(parent, "Deska", KitchenStationType.CuttingBoard, new Vector3(-5.55f, 0.6f, -4.35f), new Color(0.73f, 0.56f, 0.32f), null, 2.5f);
        CreateStation(parent, "Zwijanie", KitchenStationType.Assembly, new Vector3(-3.85f, 0.6f, 5.45f), new Color(0.65f, 0.5f, 0.28f), null, 0f);
        CreateStation(parent, "Wydanie", KitchenStationType.Delivery, new Vector3(EntranceTableX + 0.45f, 0.6f, EntranceTableZ + 0.25f), new Color(0.2f, 0.55f, 0.8f), null, 0f);

        if (grillStation != null && meatTrayStation != null)
        {
            grillStation.SetLinkedMeatTray(meatTrayStation);
            meatTrayStation.RefreshVisualState();
            grillStation.RefreshVisualState();
        }

        CreateCustomer(parent, CustomerPosition);
    }

    private void BuildEnvironmentIfNeeded()
    {
        if (GameObject.Find("RuntimeEnvironment") != null)
        {
            return;
        }

        Transform environmentRoot = new GameObject("RuntimeEnvironment").transform;

        CreateBlock(environmentRoot, "FloorBase", PrimitiveType.Cube, new Vector3(0f, -0.55f, 0f), new Vector3(14f, 1f, 14f), new Color(0.17f, 0.18f, 0.19f));
        CreateBlock(environmentRoot, "KitchenFloorInset", PrimitiveType.Cube, new Vector3(0f, -0.035f, 0f), new Vector3(12.6f, 0.03f, 12.2f), new Color(0.28f, 0.29f, 0.3f));
        CreateBlock(environmentRoot, "Ceiling", PrimitiveType.Cube, new Vector3(0f, 4.95f, 0f), new Vector3(13.6f, 0.18f, 13.6f), new Color(0.9f, 0.9f, 0.88f));

        CreateBlock(environmentRoot, "BackWall", PrimitiveType.Cube, new Vector3(0f, 2.5f, 6.8f), new Vector3(14f, 5f, 0.35f), new Color(0.84f, 0.83f, 0.8f));
        CreateBlock(environmentRoot, "LeftWall", PrimitiveType.Cube, new Vector3(-6.8f, 2.5f, 0f), new Vector3(0.35f, 5f, 14f), new Color(0.8f, 0.78f, 0.75f));
        CreateBlock(environmentRoot, "RightWall", PrimitiveType.Cube, new Vector3(6.8f, 2.5f, 0f), new Vector3(0.35f, 5f, 14f), new Color(0.8f, 0.78f, 0.75f));
        CreateBlock(environmentRoot, "FrontWall", PrimitiveType.Cube, new Vector3(0f, 2.5f, -5.65f), new Vector3(14f, 5f, 0.35f), new Color(0.82f, 0.8f, 0.77f));

        CreateBlock(environmentRoot, "BackWallPanel", PrimitiveType.Cube, new Vector3(0f, 1.4f, 6.6f), new Vector3(12.8f, 2.2f, 0.05f), new Color(0.73f, 0.74f, 0.76f));
        CreateBlock(environmentRoot, "LeftWallPanel", PrimitiveType.Cube, new Vector3(-6.6f, 1.4f, 0f), new Vector3(0.05f, 2.2f, 12.8f), new Color(0.72f, 0.73f, 0.75f));
        CreateBlock(environmentRoot, "RightWallPanel", PrimitiveType.Cube, new Vector3(6.6f, 1.4f, 0f), new Vector3(0.05f, 2.2f, 12.8f), new Color(0.72f, 0.73f, 0.75f));

        CreateBlock(environmentRoot, "CeilingTrimBack", PrimitiveType.Cube, new Vector3(0f, 4.77f, 6.52f), new Vector3(13f, 0.08f, 0.12f), new Color(0.62f, 0.61f, 0.58f));
        CreateBlock(environmentRoot, "CeilingTrimLeft", PrimitiveType.Cube, new Vector3(-6.52f, 4.77f, 0f), new Vector3(0.12f, 0.08f, 13f), new Color(0.62f, 0.61f, 0.58f));
        CreateBlock(environmentRoot, "CeilingTrimRight", PrimitiveType.Cube, new Vector3(6.52f, 4.77f, 0f), new Vector3(0.12f, 0.08f, 13f), new Color(0.62f, 0.61f, 0.58f));
        CreateCounter(environmentRoot, "LeftTableA", new Vector3(-5.55f, 0.25f, 4.6f), new Vector3(1.6f, 0.5f, 2.25f), false);
        CreateCounter(environmentRoot, "LeftTableB", new Vector3(-5.55f, 0.25f, 1.5f), new Vector3(1.6f, 0.5f, 2.25f), false);
        CreateCounter(environmentRoot, "LeftTableC", new Vector3(-5.55f, 0.25f, -1.1f), new Vector3(1.6f, 0.5f, 2.25f), false);
        CreateCounter(environmentRoot, "LeftTableD", new Vector3(-5.55f, 0.25f, -3.7f), new Vector3(1.6f, 0.5f, 2.25f), false);
        CreateCounter(environmentRoot, "BackTableA", new Vector3(-3.05f, 0.25f, 5.45f), new Vector3(2.25f, 0.5f, 1.6f), false);
        CreateCornerCounterBlockers(environmentRoot);
        CreateCounter(environmentRoot, "EntranceUtilityTableBlocker", new Vector3(EntranceTableX, 0.25f, EntranceTableZ), new Vector3(2.25f, 0.5f, 1.15f), false);

        CreateImportedEnvironmentDetails(environmentRoot);
    }

    private void BuildOrderBoardIfNeeded()
    {
        if (FindFirstObjectByType<KitchenOrderBoard>() != null)
        {
            return;
        }

        GameObject boardObject = new GameObject("KitchenOrderBoard");
        boardObject.transform.position = new Vector3(0f, 2.95f, 6.52f);
        boardObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        KitchenOrderBoard board = boardObject.AddComponent<KitchenOrderBoard>();
        board.Initialize();
    }

    private void ConfigureLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.74f, 0.76f, 0.8f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.38f, 0.34f);
        RenderSettings.ambientGroundColor = new Color(0.16f, 0.17f, 0.18f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.reflectionIntensity = 1f;
        RenderSettings.fog = false;

        Light directionalLight = FindDirectionalLight();
        if (directionalLight == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            directionalLight = lightObject.AddComponent<Light>();
        }

        directionalLight.type = LightType.Directional;
        directionalLight.transform.rotation = Quaternion.Euler(58f, -30f, 0f);
        directionalLight.color = new Color(1f, 0.96f, 0.88f);
        directionalLight.intensity = 1.2f;
        directionalLight.shadows = LightShadows.Soft;
        directionalLight.shadowStrength = 0.8f;
        directionalLight.shadowBias = 0.03f;
        directionalLight.shadowNormalBias = 0.35f;
        directionalLight.renderMode = LightRenderMode.ForcePixel;

        Transform lightingRoot = GameObject.Find("RuntimeLighting")?.transform;
        if (lightingRoot == null)
        {
            lightingRoot = new GameObject("RuntimeLighting").transform;
        }

        CreateSpotLight(
            lightingRoot,
            "PrepTaskLightLeft",
            new Vector3(-2.5f, 4.35f, 2.0f),
            new Vector3(90f, 0f, 0f),
            new Color(1f, 0.92f, 0.8f),
            6.5f,
            9.5f,
            88f);

        CreateSpotLight(
            lightingRoot,
            "PrepTaskLightRight",
            new Vector3(2.5f, 4.35f, 2.0f),
            new Vector3(90f, 0f, 0f),
            new Color(1f, 0.91f, 0.78f),
            6f,
            9.5f,
            88f);

        CreateSpotLight(
            lightingRoot,
            "AssemblyTaskLight",
            new Vector3(-3.2f, 4.25f, 5.1f),
            new Vector3(90f, 180f, 0f),
            new Color(1f, 0.93f, 0.82f),
            5.5f,
            8.5f,
            82f);
    }

    private KitchenStation CreateStation(
        Transform parent,
        string stationName,
        KitchenStationType stationType,
        Vector3 position,
        Color color,
        IngredientData sourceIngredient,
        float processingDuration)
    {
        GameObject stationObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stationObject.name = stationName + "_Station";
        stationObject.transform.SetParent(parent);
        stationObject.transform.position = position;
        stationObject.transform.localScale = new Vector3(1.35f, 1.2f, 1.35f);
        stationObject.layer = InteractableLayer;

        Renderer renderer = stationObject.GetComponent<Renderer>();
        renderer.material = new Material(GetLitShader());
        renderer.material.color = color;

        KitchenStation station = stationObject.AddComponent<KitchenStation>();
        station.Configure(stationName, stationType, sourceIngredient, processingDuration, renderer);

        NetworkKitchenStation netStation = stationObject.AddComponent<NetworkKitchenStation>();
        netStation.StationIndex = stationIndexCounter++;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = stationName + "_Marker";
        marker.transform.SetParent(stationObject.transform);
        marker.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        marker.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        marker.layer = InteractableLayer;

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            markerCollider.enabled = false;
        }

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        markerRenderer.material = new Material(GetLitShader());
        markerRenderer.material.color = sourceIngredient != null ? sourceIngredient.kolorDebug : color;

        CreateImportedStationDetails(stationObject.transform, stationName, stationType, sourceIngredient);
        renderer.enabled = false;
        markerRenderer.enabled = false;
        station.RefreshVisualState();
        return station;
    }

    private void CreateCustomer(Transform parent, Vector3 position)
    {
        Transform customerRoot = new GameObject("Customer").transform;
        customerRoot.SetParent(parent);
        customerRoot.position = position;

        GameObject customerModel = CreateImportedModel(customerRoot, "klient_idle", "CustomerVisual", Vector3.zero, new Vector3(0f, 0f, 0f), 1.95f);
        if (customerModel == null)
        {
            CreateBlock(customerRoot, "Body", PrimitiveType.Capsule, new Vector3(0f, 1f, 0f), new Vector3(0.9f, 1.8f, 0.9f), new Color(0.16f, 0.44f, 0.74f));
            CreateBlock(customerRoot, "Head", PrimitiveType.Sphere, new Vector3(0f, 2.25f, 0f), new Vector3(0.7f, 0.7f, 0.7f), new Color(0.92f, 0.78f, 0.63f));
        }
        else
        {
            customerModel.AddComponent<CustomerAnimator>();
        }
    }

    private void CreateCounter(Transform parent, string objectName, Vector3 position, Vector3 scale, bool visible = true)
    {
        Transform baseBlock = CreateBlock(parent, objectName, PrimitiveType.Cube, position, scale, new Color(0.36f, 0.31f, 0.27f));
        Transform topBlock = CreateBlock(parent, objectName + "_Top", PrimitiveType.Cube, position + new Vector3(0f, 0.31f, 0f), new Vector3(scale.x * 0.98f, 0.12f, scale.z * 0.98f), new Color(0.62f, 0.57f, 0.52f));

        if (!visible)
        {
            baseBlock.localPosition = new Vector3(position.x, 0.68f, position.z);
            baseBlock.localScale = new Vector3(scale.x, 1.36f, scale.z);
            SetRendererVisible(baseBlock, false);
            SetRendererVisible(topBlock, false);
        }
    }

    private void CreateCornerCounterBlockers(Transform parent)
    {
        Transform root = new GameObject("RightCornerCounterBlockers").transform;
        root.SetParent(parent);
        root.localPosition = Vector3.zero;

        CreateInvisibleCollider(root, "BackRun", new Vector3(5.05f, 0.62f, 5.3f), new Vector3(3.0f, 1.18f, 1.0f));
        CreateInvisibleCollider(root, "SideRun", new Vector3(5.95f, 0.62f, 4.15f), new Vector3(1.0f, 1.18f, 2.8f));
        CreateInvisibleCollider(root, "InnerCorner", new Vector3(5.45f, 0.62f, 4.75f), new Vector3(1.5f, 1.18f, 1.5f));
    }

    private void CreateInvisibleCollider(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale)
    {
        GameObject colliderObject = new GameObject(objectName);
        colliderObject.transform.SetParent(parent);
        colliderObject.transform.localPosition = localPosition;
        colliderObject.transform.localRotation = Quaternion.identity;

        BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
        collider.size = localScale;
    }

    private Light FindDirectionalLight()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light != null && light.type == LightType.Directional)
            {
                return light;
            }
        }

        return null;
    }

    private void CreateSpotLight(
        Transform parent,
        string lightName,
        Vector3 localPosition,
        Vector3 localRotation,
        Color color,
        float intensity,
        float range,
        float spotAngle)
    {
        Transform existing = parent.Find(lightName);
        GameObject lightObject = existing != null ? existing.gameObject : new GameObject(lightName);
        lightObject.transform.SetParent(parent);
        lightObject.transform.localPosition = localPosition;
        lightObject.transform.localRotation = Quaternion.Euler(localRotation);

        Light light = lightObject.GetComponent<Light>();
        if (light == null)
        {
            light = lightObject.AddComponent<Light>();
        }

        light.type = LightType.Spot;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.spotAngle = spotAngle;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.7f;
        light.shadowBias = 0.05f;
        light.shadowNormalBias = 0.4f;
        light.renderMode = LightRenderMode.ForcePixel;

        LampFlicker flicker = lightObject.GetComponent<LampFlicker>();
        if (flicker == null)
        {
            flicker = lightObject.AddComponent<LampFlicker>();
            flicker.Configure(light, intensity);
        }
    }

    private void CreateCeilingLamp(Transform parent, Vector3 position)
    {
        Transform lamp = CreateBlock(parent, "Lamp", PrimitiveType.Cylinder, position, new Vector3(0.2f, 0.15f, 0.2f), new Color(0.15f, 0.15f, 0.15f));
        CreateBlock(lamp, "LightCone", PrimitiveType.Sphere, new Vector3(0f, -0.45f, 0f), new Vector3(0.5f, 0.18f, 0.5f), new Color(1f, 0.93f, 0.68f));
    }

    private void CreateImportedEnvironmentDetails(Transform parent)
    {
        GameObject lampLeft = CreateImportedModel(parent, "lamp", "ImportedLampLeft", new Vector3(-2.5f, 4.45f, 2f), new Vector3(0f, 0f, 0f), 0.9f);
        if (lampLeft != null && lampLeft.GetComponent<LampEmissionPulse>() == null)
        {
            lampLeft.AddComponent<LampEmissionPulse>();
        }

        GameObject lampRight = CreateImportedModel(parent, "lamp", "ImportedLampRight", new Vector3(2.5f, 4.45f, 2f), new Vector3(0f, 0f, 0f), 0.9f);
        if (lampRight != null && lampRight.GetComponent<LampEmissionPulse>() == null)
        {
            lampRight.AddComponent<LampEmissionPulse>();
        }

        CreateImportedModel(parent, "wall_shelf", "ImportedBackShelves", new Vector3(3.35f, 2.05f, 6.35f), new Vector3(0f, 180f, 0f), 2.2f);
        CreateImportedModel(parent, "prep_table", "ImportedLeftTableA", new Vector3(-5.55f, 0f, 4.6f), new Vector3(0f, 90f, 0f), TableVisualSize, new Vector3(1f, 1f, TableDepthScale));
        CreateImportedModel(parent, "prep_table", "ImportedLeftTableB", new Vector3(-5.55f, 0f, 1.5f), new Vector3(0f, 90f, 0f), TableVisualSize, new Vector3(1f, 1f, TableDepthScale));
        CreateImportedModel(parent, "prep_table", "ImportedLeftTableC", new Vector3(-5.55f, 0f, -1.1f), new Vector3(0f, 90f, 0f), TableVisualSize, new Vector3(1f, 1f, TableDepthScale));
        CreateImportedModel(parent, "prep_table", "ImportedLeftTableD", new Vector3(-5.55f, 0f, -3.7f), new Vector3(0f, 90f, 0f), TableVisualSize, new Vector3(1f, 1f, TableDepthScale));
        CreateImportedModel(parent, "prep_table", "ImportedBackTableA", new Vector3(-3.05f, 0f, 5.45f), new Vector3(0f, 180f, 0f), TableVisualSize, new Vector3(1f, 1f, TableDepthScale));
        CreateImportedModel(parent, "corner_counter", "ImportedRightCornerCounter", new Vector3(5.25f, 0f, 4.9f), new Vector3(0f, 180f, 0f), 2.9f, new Vector3(1.15f, 1f, 1.15f));
        CreateImportedModel(parent, "utility_table", "ImportedEntranceUtilityTable", new Vector3(EntranceTableX, 0f, EntranceTableZ), new Vector3(0f, 180f, 0f), 2.25f, new Vector3(1.25f, 1f, 1f));
        CreateImportedModel(parent, "cash_register", "EntranceCashRegisterVisual", new Vector3(EntranceTableX - 0.45f, EntranceTableTopY, EntranceTableZ + 0.1f), new Vector3(0f, 0f, 0f), 0.8f);
        CreateDeliveryTrayDisplay(parent);
    }

    private void CreateDeliveryTrayDisplay(Transform parent)
    {
        Transform root = new GameObject("EntranceServingTrayDisplay").transform;
        root.SetParent(parent);
        root.localPosition = Vector3.zero;

        CreateImportedModel(root, "serving_tray", "EntranceServingTrayVisual", new Vector3(EntranceTableX + 0.52f, EntranceTableTopY + 0.02f, EntranceTableZ + 0.12f), new Vector3(0f, 180f, 0f), 1.2f);
        GameObject kebab = CreateImportedModel(root, "kebab_wrap", "EntranceServedKebabVisual", new Vector3(EntranceTableX + 0.52f, EntranceTableTopY + 0.1f, EntranceTableZ + 0.12f), new Vector3(0f, -18f, 0f), 0.5f);

        DeliveryTrayDisplay display = root.gameObject.AddComponent<DeliveryTrayDisplay>();
        display.Configure(kebab, 5f);
    }

    private void CreateImportedStationDetails(
        Transform parent,
        string stationName,
        KitchenStationType stationType,
        IngredientData sourceIngredient)
    {
        if (stationType == KitchenStationType.CuttingBoard)
        {
            CreateCuttingBoardDetails(parent);
            return;
        }

        if (stationType == KitchenStationType.Grill)
        {
            CreateImportedModel(parent, "doner_machine", "DonerGrillVisual", new Vector3(0f, WorktopLocalY, 0f), new Vector3(0f, 90f, 0f), 1.35f);
            return;
        }

        if (stationType == KitchenStationType.Delivery)
        {
            return;
        }

        if (stationType == KitchenStationType.Assembly)
        {
            Vector3 wrapBoardPosition = new Vector3(0.18f, WorktopLocalY, 0.04f);
            CreateImportedModel(parent, "cutting_board", "WrapBoardVisual", wrapBoardPosition, new Vector3(0f, -20f, 0f), 0.9f);
            CreateImportedModel(parent, "lavash", "LavashOnWrapStation", wrapBoardPosition + new Vector3(0f, 0.01f, -0.02f), new Vector3(0f, 12f, 0f), 0.55f);
            return;
        }

        if (stationType != KitchenStationType.IngredientSource)
        {
            return;
        }

        if (sourceIngredient != null && sourceIngredient.typSkladnika == IngredientKind.GarlicSauce)
        {
            CreateImportedModel(parent, "sauce_bottle", "SauceDispenserVisual", new Vector3(0f, WorktopLocalY, 0f), new Vector3(0f, 180f, 0f), 0.52f);
            return;
        }

        CreateImportedModel(parent, "ingredient_tray", stationName + "TrayVisual", new Vector3(0f, WorktopLocalY, 0f), new Vector3(0f, 0f, 0f), 0.85f);
        CreateIngredientVisual(parent, sourceIngredient);
    }

    private void CreateCuttingBoardDetails(Transform parent)
    {
        float boardY = WorktopLocalY + 0.1f;
        float knifeY = WorktopLocalY + 0.04f;

        CreateImportedModel(parent, "cutting_board", "CuttingBoardVisual", new Vector3(0f, WorktopLocalY, 0f), new Vector3(0f, 15f, 0f), 0.95f);
        CreateImportedModel(parent, "tomato_chopped", "CutTomatoVisualA", new Vector3(-0.2f, boardY, 0.1f), new Vector3(0f, -24f, 0f), 0.3f);
        CreateImportedModel(parent, "onion_chopped", "CutOnionVisualA", new Vector3(0.18f, boardY, -0.02f), new Vector3(0f, 12f, 0f), 0.3f);
        CreateImportedModel(parent, "chef_knife", "KnifeVisual", new Vector3(0.36f, knifeY, 0.46f), new Vector3(90f, 63f, 0f), 0.5f);
    }

    private void CreateIngredientVisual(Transform parent, IngredientData sourceIngredient)
    {
        if (sourceIngredient == null)
        {
            return;
        }

        float surfaceY = WorktopLocalY;

        switch (sourceIngredient.typSkladnika)
        {
            case IngredientKind.Meat:
                CreateImportedModel(parent, "meat_cooked", "MeatVisual", new Vector3(0f, surfaceY, 0f), new Vector3(0f, 0f, 0f), 0.55f);
                break;
            case IngredientKind.Tomato:
                surfaceY += 0.035f;
                CreateImportedModel(parent, "tomato_whole", "TomatoVisualA", new Vector3(-0.25f, surfaceY, -0.21f), new Vector3(0f, -11f, 0f), 0.25f);
                CreateImportedModel(parent, "tomato_whole", "TomatoVisualB", new Vector3(0.02f, surfaceY, -0.13f), new Vector3(0f, 37f, 0f), 0.22f);
                CreateImportedModel(parent, "tomato_whole", "TomatoVisualC", new Vector3(0.24f, surfaceY, -0.26f), new Vector3(0f, 94f, 0f), 0.24f);
                CreateImportedModel(parent, "tomato_whole", "TomatoVisualD", new Vector3(-0.07f, surfaceY, 0.12f), new Vector3(0f, -58f, 0f), 0.21f);
                CreateImportedModel(parent, "tomato_whole", "TomatoVisualE", new Vector3(0.2f, surfaceY, 0.07f), new Vector3(0f, 16f, 0f), 0.23f);
                break;
            case IngredientKind.Onion:
                surfaceY += 0.035f;
                CreateImportedModel(parent, "onion_whole", "OnionVisualA", new Vector3(-0.27f, surfaceY, -0.13f), new Vector3(0f, -31f, 0f), 0.25f);
                CreateImportedModel(parent, "onion_whole", "OnionVisualB", new Vector3(-0.03f, surfaceY, -0.22f), new Vector3(0f, 8f, 0f), 0.23f);
                CreateImportedModel(parent, "onion_whole", "OnionVisualC", new Vector3(0.22f, surfaceY, -0.04f), new Vector3(0f, 52f, 0f), 0.24f);
                CreateImportedModel(parent, "onion_whole", "OnionVisualD", new Vector3(-0.16f, surfaceY, 0.14f), new Vector3(0f, 89f, 0f), 0.22f);
                CreateImportedModel(parent, "onion_whole", "OnionVisualE", new Vector3(0.11f, surfaceY, 0.2f), new Vector3(0f, -48f, 0f), 0.24f);
                break;
            case IngredientKind.Lettuce:
                CreateImportedModel(parent, "lettuce_whole", "LettuceVisualA", new Vector3(-0.28f, surfaceY, -0.12f), new Vector3(0f, -10f, 0f), 0.34f);
                CreateImportedModel(parent, "lettuce_whole", "LettuceVisualB", new Vector3(0.18f, surfaceY, -0.03f), new Vector3(0f, 24f, 0f), 0.36f);
                CreateImportedModel(parent, "lettuce_whole", "LettuceVisualC", new Vector3(-0.36f, surfaceY, 0.24f), new Vector3(0f, 58f, 0f), 0.32f);
                break;
            case IngredientKind.Lavash:
                CreateLavashVisual(parent, surfaceY);
                break;
        }
    }

    private void CreateLavashVisual(Transform parent, float surfaceY)
    {
        float lavashY = surfaceY + 0.055f;
        CreateImportedModel(parent, "lavash", "LavashVisual", new Vector3(0f, lavashY, 0f), new Vector3(0f, 12f, 0f), 0.72f);
    }

    private GameObject CreateImportedModel(
        Transform parent,
        string resourceName,
        string objectName,
        Vector3 localPosition,
        Vector3 localRotation,
        float targetMaxSize)
    {
        return CreateImportedModel(parent, resourceName, objectName, localPosition, localRotation, targetMaxSize, Vector3.one);
    }

    private GameObject CreateImportedModel(
        Transform parent,
        string resourceName,
        string objectName,
        Vector3 localPosition,
        Vector3 localRotation,
        float targetMaxSize,
        Vector3 scaleMultiplier)
    {
        GameObject prefab = Resources.Load<GameObject>(ModelPath + resourceName);
        if (prefab == null)
        {
            return null;
        }

        GameObject model = Instantiate(prefab, parent);
        model.name = objectName;
        model.transform.localPosition = localPosition;
        model.transform.localRotation = Quaternion.Euler(localRotation);
        model.transform.localScale = Vector3.one;

        ScaleModelToSize(model.transform, targetMaxSize);
        model.transform.localScale = Vector3.Scale(model.transform.localScale, scaleMultiplier);
        AlignModelBottomToLocalY(model.transform, localPosition.y);
        ApplyFallbackMaterialsIfMissing(model, resourceName);
        DisableImportedColliders(model);
        return model;
    }

    private void AlignModelBottomToLocalY(Transform modelRoot, float targetBottomLocalY)
    {
        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0 || modelRoot.parent == null)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 worldBottom = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        float currentBottomLocalY = modelRoot.parent.InverseTransformPoint(worldBottom).y;
        Vector3 position = modelRoot.localPosition;
        position.y += targetBottomLocalY - currentBottomLocalY;
        modelRoot.localPosition = position;
    }

    private void ApplyFallbackMaterialsIfMissing(GameObject model, string resourceName)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (HasImportedMaterial(renderers[i]))
            {
                continue;
            }

            renderers[i].material = CreateImportedMaterial(resourceName, renderers[i].name, i);
        }
    }

    private bool HasImportedMaterial(Renderer renderer)
    {
        if (renderer == null || renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
        {
            return false;
        }

        foreach (Material material in renderer.sharedMaterials)
        {
            if (material == null)
            {
                continue;
            }

            if (HasMaterialTexture(material))
            {
                return true;
            }

            string materialName = material.name.ToLowerInvariant();
            if (!materialName.Contains("default") && !materialName.Contains("no name"))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasMaterialTexture(Material material)
    {
        if (material == null)
        {
            return false;
        }

        if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
        {
            return true;
        }

        if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
        {
            return true;
        }

        return material.mainTexture != null;
    }

    private Material CreateImportedMaterial(string resourceName, string rendererName, int index)
    {
        Material material = new Material(GetLitShader());
        Color color = GetImportedModelColor(resourceName, rendererName, index);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        float metallic = GetImportedModelMetallic(resourceName, rendererName);
        float smoothness = GetImportedModelSmoothness(resourceName, rendererName);

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", smoothness);
        }

        if (resourceName == "lamp" && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(1f, 0.78f, 0.35f) * 0.55f);
        }

        return material;
    }

    private Color GetImportedModelColor(string resourceName, string rendererName, int index)
    {
        string name = (resourceName + " " + rendererName).ToLowerInvariant();
        float shade = 1f - Mathf.Min(index, 4) * 0.045f;

        if (name.Contains("meat"))
        {
            return new Color(0.62f, 0.25f, 0.15f) * shade;
        }

        if (name.Contains("doner_machine"))
        {
            return new Color(0.42f, 0.43f, 0.42f) * shade;
        }

        if (name.Contains("chef_knife"))
        {
            return new Color(0.72f, 0.75f, 0.76f) * shade;
        }

        if (name.Contains("cash_register"))
        {
            return new Color(0.08f, 0.1f, 0.12f) * shade;
        }

        if (name.Contains("sauce_bottle"))
        {
            return new Color(0.9f, 0.82f, 0.62f) * shade;
        }

        if (name.Contains("cutting_board"))
        {
            return new Color(0.58f, 0.38f, 0.19f) * shade;
        }

        if (name.Contains("prep_table") || name.Contains("utility_table"))
        {
            return new Color(0.45f, 0.46f, 0.47f) * shade;
        }

        if (name.Contains("corner_counter"))
        {
            return new Color(0.5f, 0.51f, 0.52f) * shade;
        }

        if (name.Contains("ingredient_tray") || name.Contains("serving_tray"))
        {
            return new Color(0.55f, 0.56f, 0.54f) * shade;
        }

        if (name.Contains("wall_shelf"))
        {
            return new Color(0.5f, 0.5f, 0.52f) * shade;
        }

        if (name.Contains("lamp"))
        {
            return new Color(1f, 0.83f, 0.48f) * shade;
        }

        if (name.Contains("wall"))
        {
            return new Color(0.72f, 0.74f, 0.75f) * shade;
        }

        return new Color(0.62f, 0.62f, 0.6f) * shade;
    }

    private float GetImportedModelMetallic(string resourceName, string rendererName)
    {
        string name = (resourceName + " " + rendererName).ToLowerInvariant();
        if (name.Contains("chef_knife") ||
            name.Contains("doner_machine") ||
            name.Contains("cash_register") ||
            name.Contains("prep_table") ||
            name.Contains("utility_table") ||
            name.Contains("corner_counter") ||
            name.Contains("ingredient_tray") ||
            name.Contains("serving_tray") ||
            name.Contains("wall_shelf"))
        {
            return 0.45f;
        }

        return 0f;
    }

    private float GetImportedModelSmoothness(string resourceName, string rendererName)
    {
        string name = (resourceName + " " + rendererName).ToLowerInvariant();
        if (name.Contains("chef_knife") ||
            name.Contains("doner_machine") ||
            name.Contains("cash_register") ||
            name.Contains("prep_table") ||
            name.Contains("utility_table") ||
            name.Contains("corner_counter") ||
            name.Contains("ingredient_tray") ||
            name.Contains("serving_tray") ||
            name.Contains("wall_shelf"))
        {
            return 0.55f;
        }

        if (name.Contains("meat") || name.Contains("cutting_board"))
        {
            return 0.25f;
        }

        return 0.35f;
    }

    private void ScaleModelToSize(Transform modelRoot, float targetMaxSize)
    {
        if (targetMaxSize <= 0f)
        {
            return;
        }

        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float maxSize = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (maxSize <= 0.001f)
        {
            return;
        }

        modelRoot.localScale *= targetMaxSize / maxSize;
    }

    private void DisableImportedColliders(GameObject model)
    {
        foreach (Collider collider in model.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
    }

    private void SetRendererVisible(Transform root, bool visible)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = visible;
        }
    }

    private Transform CreateBlock(
        Transform parent,
        string objectName,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Color color)
    {
        GameObject block = GameObject.CreatePrimitive(primitiveType);
        block.name = objectName;
        block.transform.SetParent(parent);
        block.transform.localPosition = localPosition;
        block.transform.localScale = localScale;

        Renderer renderer = block.GetComponent<Renderer>();
        renderer.material = new Material(GetLitShader());
        renderer.material.color = color;
        return block.transform;
    }

    private void CreateLabel(Transform parent, string labelText, Vector3 localPosition)
    {
        GameObject label = new GameObject(labelText + "_Label");
        label.transform.SetParent(parent);
        label.transform.localPosition = localPosition;

        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = labelText;
        textMesh.characterSize = 0.02f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 24;
        textMesh.color = new Color(1f, 1f, 1f, 0.55f);

        label.AddComponent<BillboardLabel>();
    }
}

public class BillboardLabel : MonoBehaviour
{
    private Camera targetCamera;

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        transform.forward = targetCamera.transform.forward;
    }
}

public class DeliveryTrayDisplay : MonoBehaviour
{
    private static DeliveryTrayDisplay activeDisplay;

    [SerializeField] private GameObject servedKebab;
    [SerializeField] private float visibleDuration = 5f;

    private float hideAtTime;

    public static void ShowServedKebab()
    {
        if (activeDisplay != null)
        {
            activeDisplay.Show();
        }
    }

    public void Configure(GameObject servedKebab, float visibleDuration)
    {
        activeDisplay = this;
        this.servedKebab = servedKebab;
        this.visibleDuration = visibleDuration;
        SetKebabVisible(false);
    }

    private void Update()
    {
        if (servedKebab == null || !servedKebab.activeSelf || Time.time < hideAtTime)
        {
            return;
        }

        SetKebabVisible(false);
    }

    private void OnDestroy()
    {
        if (activeDisplay == this)
        {
            activeDisplay = null;
        }
    }

    private void Show()
    {
        hideAtTime = Time.time + visibleDuration;
        SetKebabVisible(true);
    }

    private void SetKebabVisible(bool visible)
    {
        if (servedKebab != null)
        {
            servedKebab.SetActive(visible);
        }
    }
}

public class CustomerAnimator : MonoBehaviour
{
    private UnityEngine.Playables.PlayableGraph graph;
    private UnityEngine.Animations.AnimationClipPlayable idlePlayable;
    private float clipLength;

    private void Start()
    {
        AnimationClip[] idles = Resources.LoadAll<AnimationClip>("Models/klient_idle");
        if (idles != null && idles.Length > 0)
        {
            AnimationClip idleClip = idles.FirstOrDefault(c => !c.name.StartsWith("__preview")) ?? idles.FirstOrDefault();
            if (idleClip != null)
            {
                clipLength = idleClip.length;
                Animator animator = GetComponent<Animator>();
                if (animator == null) animator = gameObject.AddComponent<Animator>();

                graph = UnityEngine.Playables.PlayableGraph.Create("CustomerAnimGraph");
                graph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.GameTime);

                var output = UnityEngine.Animations.AnimationPlayableOutput.Create(graph, "Animation", animator);
                idlePlayable = UnityEngine.Animations.AnimationClipPlayable.Create(graph, idleClip);
                output.SetSourcePlayable(idlePlayable);

                graph.Play();
            }
        }
    }

    private void Update()
    {
        if (graph.IsValid() && idlePlayable.IsValid() && clipLength > 0f)
        {
            if (idlePlayable.GetTime() >= clipLength)
            {
                idlePlayable.SetTime(idlePlayable.GetTime() % clipLength);
            }
        }
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
