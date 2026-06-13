using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Linq;

public class NetworkPlayer : NetworkBehaviour
{
    private const float EyeHeight = 1.75f;
    private const float StationSyncInterval = 0.15f;
    private const float EconomySyncInterval = 0.5f;
    private const float ShopSyncInterval = 1.0f;

    private static readonly Vector3[] SpawnPoints = new Vector3[]
    {
        new Vector3(0f, 0f, -1.9f),
        new Vector3(-2f, 0f, -1.9f),
        new Vector3(2f, 0f, -1.9f),
        new Vector3(0f, 0f, -0.5f)
    };

    public static readonly Color[] PlayerColors = new Color[]
    {
        new Color(0.2f, 0.6f, 0.9f),
        new Color(0.9f, 0.4f, 0.3f),
        new Color(0.3f, 0.8f, 0.4f),
        new Color(0.9f, 0.75f, 0.2f)
    };

    private NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        "Gracz",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<NetworkItemState> netHeldItem = new NetworkVariable<NetworkItemState>(
        NetworkItemState.Empty(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<int> playerIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Camera playerCamera;
    private GameObject remotePlayerVisual;
    private TextMesh nameLabel;
    private SimplePlayerController cachedController;
    private PlayerInteraction cachedInteraction;

    private GameObject heldItemVisual;
    private NetworkItemState lastVisualState;

    private NetworkKitchenStation[] cachedStations;
    private float cachedStationsTime;
    private float nextStationSyncTime;
    private float nextEconomySyncTime;
    private float nextShopSyncTime;
    private float nextOrderSyncTime;

    public Camera PlayerCamera => playerCamera;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            int index = (int)(OwnerClientId % (ulong)SpawnPoints.Length);
            playerIndex.Value = index;
            playerName.Value = new FixedString32Bytes("Gracz " + (index + 1));

            transform.position = SpawnPoints[index];
            transform.rotation = Quaternion.identity;
        }

        if (IsOwner)
        {
            SetupLocalPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }

        playerName.OnValueChanged += OnPlayerNameChanged;
        playerIndex.OnValueChanged += OnPlayerIndexChanged;
        netHeldItem.OnValueChanged += OnHeldItemChanged;
    }

    private void Start()
    {

        if (!IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            if (cachedController == null && cachedInteraction == null)
            {
                SetupLocalPlayer();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        playerName.OnValueChanged -= OnPlayerNameChanged;
        playerIndex.OnValueChanged -= OnPlayerIndexChanged;
        netHeldItem.OnValueChanged -= OnHeldItemChanged;

        if (IsOwner && playerCamera != null)
        {
            Destroy(playerCamera.gameObject);
        }

        if (heldItemVisual != null)
        {
            Destroy(heldItemVisual);
        }
    }

    private bool isLocalPlayerSetup = false;

    private void SetupLocalPlayer()
    {
        if (isLocalPlayerSetup) return;
        isLocalPlayerSetup = true;

        gameObject.name = "Player_Local";

        // Destroy existing scene camera so we don't have duplicates
        Camera existingMain = Camera.main;
        if (existingMain != null && existingMain.GetComponentInParent<NetworkPlayer>() == null)
        {
            Destroy(existingMain.gameObject);
        }

        GameObject cameraObject = new GameObject("PlayerCamera");
        cameraObject.transform.SetParent(transform);
        cameraObject.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
        cameraObject.transform.localRotation = Quaternion.identity;
        playerCamera = cameraObject.AddComponent<Camera>();
        playerCamera.tag = "MainCamera";
        playerCamera.nearClipPlane = 0.1f;
        playerCamera.farClipPlane = 100f;

        // Transfer AudioListener to the new player camera.
        // Destroy() is deferred, so the old AudioListener would still be found by FindFirstObjectByType.
        // We must remove it immediately before adding the new one.
        AudioListener oldListener = existingMain != null ? existingMain.GetComponent<AudioListener>() : null;
        if (oldListener != null)
        {
            DestroyImmediate(oldListener);
        }
        cameraObject.AddComponent<AudioListener>();

        UnityEngine.Rendering.Universal.UniversalAdditionalCameraData cameraData =
            cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = UnityEngine.Rendering.Universal.AntialiasingQuality.Medium;

        cachedController = GetComponent<SimplePlayerController>();
        if (cachedController != null)
        {
            cachedController.playerCamera = playerCamera;
            cachedController.enabled = true;
        }

        CameraEffects camFx = cameraObject.GetComponent<CameraEffects>();
        if (camFx == null)
        {
            camFx = cameraObject.AddComponent<CameraEffects>();
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            camFx.SetTrackedController(cc);
        }

        cachedInteraction = GetComponent<PlayerInteraction>();
        if (cachedInteraction != null)
        {
            cachedInteraction.playerCamera = playerCamera;
            cachedInteraction.interactableLayer = 1 << 6;
            cachedInteraction.interactionDistance = 5.5f;
            cachedInteraction.enabled = true;
        }

        if (FindFirstObjectByType<KitchenHUD>() == null)
        {
            new GameObject("KitchenHUD").AddComponent<KitchenHUD>();
        }
        if (FindFirstObjectByType<InteractionHighlight>() == null)
        {
            new GameObject("InteractionHighlight").AddComponent<InteractionHighlight>();
        }
        if (FindFirstObjectByType<ShopUI>() == null)
        {
            new GameObject("ShopUI").AddComponent<ShopUI>();
        }
        if (FindFirstObjectByType<PlayerListUI>() == null)
        {
            new GameObject("PlayerListUI").AddComponent<PlayerListUI>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        string nickname = LobbyUI.LocalPlayerNickname;
        if (IsSpawned)
        {
            SetPlayerNameServerRpc(nickname);
        }
        else
        {
            pendingNickname = nickname;
        }

        Vector3 customerLookTarget = new Vector3(0f, 1.55f, -4.8f);
        if (cachedController != null)
        {
            cachedController.SetInitialLookTarget(customerLookTarget);
            cachedController.SetLookAt(customerLookTarget);
        }

        Debug.Log("[NetworkPlayer] Lokalny gracz skonfigurowany. Nick: " + nickname);
    }

    private void SetupRemotePlayer()
    {
        gameObject.name = "Player_Remote_" + OwnerClientId;

        SimplePlayerController controller = GetComponent<SimplePlayerController>();
        if (controller != null) controller.enabled = false;

        PlayerInteraction interaction = GetComponent<PlayerInteraction>();
        if (interaction != null) interaction.enabled = false;

        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null) Destroy(cam.gameObject);

        CreateRemoteVisual();
        Debug.Log("[NetworkPlayer] Zdalny gracz skonfigurowany: " + OwnerClientId);
    }

    private void CreateRemoteVisual()
    {
        if (remotePlayerVisual != null) return;

        GameObject root = new GameObject("RemoteChef");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;

        GameObject fbxPrefab = Resources.Load<GameObject>("Models/Gracz_Idle");
        if (fbxPrefab != null)
        {
            GameObject model = Instantiate(fbxPrefab, root.transform);
            model.name = "HumanModel";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            int colorIndex = playerIndex.Value % PlayerColors.Length;
            Color bodyColor = PlayerColors[colorIndex];
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material playerMat = new Material(shader) { color = bodyColor };

            foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
            {
                if (r.sharedMaterials == null || r.sharedMaterials.Length == 0 || (r.sharedMaterials[0] != null && r.sharedMaterials[0].name.Contains("Default")))
                {
                    r.material = playerMat;
                }
            }

            RemotePlayerAnimator animator = model.AddComponent<RemotePlayerAnimator>();

            AnimationClip[] idles = Resources.LoadAll<AnimationClip>("Models/Gracz_Idle");
            AnimationClip[] walks = Resources.LoadAll<AnimationClip>("Models/Gracz_Walk");

            if (idles != null && idles.Length > 0) animator.idleClip = idles.FirstOrDefault(c => !c.name.StartsWith("__preview")) ?? idles.FirstOrDefault();
            if (walks != null && walks.Length > 0) animator.walkClip = walks.FirstOrDefault(c => !c.name.StartsWith("__preview")) ?? walks.FirstOrDefault();

            animator.Initialize();

            Debug.Log($"[NetworkPlayer] FBX loaded. Idle clips: {idles?.Length}, Walk clips: {walks?.Length}. Selected Idle: {animator.idleClip?.name}");
        }
        else
        {

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Torso";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.42f, 0.5f, 0.3f);
            DisableCollider(body);

            root.AddComponent<RemotePlayerAnimator>();
        }

        GameObject labelObject = new GameObject("NameLabel");
        labelObject.transform.SetParent(root.transform);
        labelObject.transform.localPosition = new Vector3(0f, 2.3f, 0f);
        nameLabel = labelObject.AddComponent<TextMesh>();
        nameLabel.text = playerName.Value.ToString();
        nameLabel.characterSize = 0.04f;
        nameLabel.anchor = TextAnchor.MiddleCenter;
        nameLabel.alignment = TextAlignment.Center;
        nameLabel.fontSize = 18;
        nameLabel.color = new Color(1f, 1f, 1f, 0.85f);
        labelObject.AddComponent<BillboardLabel>();

        remotePlayerVisual = root;
    }

    private void DisableCollider(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void UpdateHeldItemVisual(NetworkItemState itemState)
    {

        if (heldItemVisual != null)
        {
            if (!itemState.exists ||
                itemState.ingredientKind != lastVisualState.ingredientKind ||
                itemState.state != lastVisualState.state ||
                itemState.isDish != lastVisualState.isDish)
            {
                Destroy(heldItemVisual);
                heldItemVisual = null;
            }
        }

        lastVisualState = itemState;

        if (!itemState.exists) return;
        if (heldItemVisual != null) return;

        bool isDish = itemState.isDish;

        if (!IsOwner)
        {
            Vector3 localPos = new Vector3(0.25f, 1.2f, 0.35f);
            Vector3 localRot = isDish ? new Vector3(0f, 0f, 90f) : Vector3.zero;
            float modelSize = isDish ? 0.3f : 0.2f;

            heldItemVisual = KitchenItemVisualFactory.CreateItemVisual(
                itemState.ingredientKind, itemState.state, isDish,
                transform, localPos, localRot, modelSize);
        }
    }

    private void OnHeldItemChanged(NetworkItemState oldValue, NetworkItemState newValue)
    {
        UpdateHeldItemVisual(newValue);
    }

    private string pendingNickname;
    private float nextHeldItemSyncTime;

    private void Update()
    {
        if (!IsSpawned) return;

        if (pendingNickname != null && IsOwner)
        {
            SetPlayerNameServerRpc(pendingNickname);
            pendingNickname = null;
        }

        if (IsOwner && cachedInteraction != null && Time.time >= nextHeldItemSyncTime)
        {
            NetworkItemState currentState = NetworkItemState.FromKitchenItem(cachedInteraction.HeldItem);
            if (currentState.exists != netHeldItem.Value.exists ||
                currentState.ingredientKind != netHeldItem.Value.ingredientKind ||
                currentState.state != netHeldItem.Value.state ||
                currentState.isDish != netHeldItem.Value.isDish)
            {
                UpdateHeldItemServerRpc(currentState);
                nextHeldItemSyncTime = Time.time + 0.15f;
            }
        }

        if (IsServer && IsOwner)
        {
            BroadcastStationStates();
            BroadcastEconomy();
            BroadcastShopUpgrades();
            BroadcastOrders();
        }
    }

    private void BroadcastStationStates()
    {
        if (Time.time < nextStationSyncTime) return;
        nextStationSyncTime = Time.time + StationSyncInterval;

        if (cachedStations == null || Time.time - cachedStationsTime > 5f)
        {
            cachedStations = FindObjectsByType<NetworkKitchenStation>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            cachedStationsTime = Time.time;
        }

        for (int i = 0; i < cachedStations.Length; i++)
        {
            if (cachedStations[i] == null) continue;
            if (!cachedStations[i].IsStateDirty()) continue;

            StationStateSnapshot snapshot = cachedStations[i].CaptureSnapshot();
            SyncStationStateClientRpc(snapshot);
        }
    }

    [ClientRpc]
    private void SyncStationStateClientRpc(StationStateSnapshot snapshot)
    {

        if (IsServer) return;

        if (cachedStations == null || Time.time - cachedStationsTime > 5f)
        {
            cachedStations = FindObjectsByType<NetworkKitchenStation>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            cachedStationsTime = Time.time;
        }

        foreach (NetworkKitchenStation station in cachedStations)
        {
            if (station != null && station.StationIndex == snapshot.stationIndex)
            {
                station.ApplySnapshot(snapshot);
                break;
            }
        }
    }

    private void BroadcastEconomy()
    {
        if (Time.time < nextEconomySyncTime) return;
        nextEconomySyncTime = Time.time + EconomySyncInterval;

        if (EconomyManager.Instance != null)
        {
            SyncEconomyClientRpc(EconomyManager.Instance.CurrentBalance, EconomyManager.Instance.TotalEarned);
        }
    }

    [ClientRpc]
    private void SyncEconomyClientRpc(float balance, float totalEarned)
    {
        if (IsServer) return;
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.SetBalanceFromNetwork(balance, totalEarned);
        }
    }

    private void BroadcastOrders()
    {
        if (Time.time < nextOrderSyncTime) return;
        nextOrderSyncTime = Time.time + 0.25f;

        if (OrderManager.Instance != null)
        {
            string desc = OrderManager.Instance.ActiveOrderDescription ?? "";
            float time = OrderManager.Instance.RemainingOrderTime;
            int comp = OrderManager.Instance.CompletedOrders;
            int fail = OrderManager.Instance.FailedOrders;

            SyncOrdersClientRpc(desc, time, comp, fail);
        }
    }

    [ClientRpc]
    private void SyncOrdersClientRpc(string desc, float time, int comp, int fail)
    {
        if (IsServer) return;
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.SyncNetworkState(desc, time, comp, fail);
        }
    }

    private void BroadcastShopUpgrades()
    {
        if (Time.time < nextShopSyncTime) return;
        nextShopSyncTime = Time.time + ShopSyncInterval;

        if (ShopManager.Instance == null) return;

        int grillLvl = ShopManager.Instance.GetUpgradeLevel(UpgradeType.GrillSpeed);
        int cutLvl = ShopManager.Instance.GetUpgradeLevel(UpgradeType.CuttingSpeed);
        int rewardLvl = ShopManager.Instance.GetUpgradeLevel(UpgradeType.RewardBonus);
        int timeLvl = ShopManager.Instance.GetUpgradeLevel(UpgradeType.OrderTime);
        int meatLvl = ShopManager.Instance.GetUpgradeLevel(UpgradeType.MeatBatchSize);

        SyncShopUpgradesClientRpc(grillLvl, cutLvl, rewardLvl, timeLvl, meatLvl);
    }

    [ClientRpc]
    private void SyncShopUpgradesClientRpc(int grillLvl, int cutLvl, int rewardLvl, int timeLvl, int meatLvl)
    {
        if (IsServer) return;
        if (ShopManager.Instance == null) return;

        ShopManager.Instance.SetUpgradeLevel(UpgradeType.GrillSpeed, grillLvl);
        ShopManager.Instance.SetUpgradeLevel(UpgradeType.CuttingSpeed, cutLvl);
        ShopManager.Instance.SetUpgradeLevel(UpgradeType.RewardBonus, rewardLvl);
        ShopManager.Instance.SetUpgradeLevel(UpgradeType.OrderTime, timeLvl);
        ShopManager.Instance.SetUpgradeLevel(UpgradeType.MeatBatchSize, meatLvl);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PurchaseUpgradeServerRpc(int upgradeTypeInt, ServerRpcParams rpcParams = default)
    {
        UpgradeType type = (UpgradeType)upgradeTypeInt;
        bool success = false;

        if (ShopManager.Instance != null)
        {
            success = ShopManager.Instance.TryPurchaseUpgrade(type);
        }

        if (success)
        {
            nextShopSyncTime = 0f;
            nextEconomySyncTime = 0f;
            PurchaseResultClientRpc(true, upgradeTypeInt);
        }
        else
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            ClientRpcParams targetClient = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { senderClientId } }
            };
            PurchaseResultClientRpc(false, upgradeTypeInt, targetClient);
        }
    }

    [ClientRpc]
    private void PurchaseResultClientRpc(bool success, int upgradeTypeInt, ClientRpcParams clientRpcParams = default)
    {
        ShopUI shopUI = FindFirstObjectByType<ShopUI>();
        if (shopUI != null)
        {
            shopUI.HandlePurchaseResult(success, (UpgradeType)upgradeTypeInt);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void InteractWithStationServerRpc(int stationIndex, NetworkItemState heldItem, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (cachedStations == null || Time.time - cachedStationsTime > 5f)
        {
            cachedStations = FindObjectsByType<NetworkKitchenStation>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            cachedStationsTime = Time.time;
        }

        NetworkKitchenStation targetStation = null;
        foreach (NetworkKitchenStation station in cachedStations)
        {
            if (station != null && station.StationIndex == stationIndex)
            {
                targetStation = station;
                break;
            }
        }

        if (targetStation == null)
        {
            Debug.LogWarning("[NetworkPlayer] Station not found: " + stationIndex);
            return;
        }

        NetworkPlayer requestingPlayer = FindNetworkPlayerByClientId(senderClientId);
        if (requestingPlayer == null) return;

        PlayerInteraction interaction = requestingPlayer.GetComponent<PlayerInteraction>();
        if (interaction == null) return;

        KitchenItem serverItem = requestingPlayer.netHeldItem.Value.ToKitchenItem();
        interaction.ClearHeldItem();
        if (serverItem != null)
        {
            interaction.TryReceiveItem(serverItem);
        }

        targetStation.ServerInteract(interaction);

        NetworkItemState updatedHeld = NetworkItemState.FromKitchenItem(interaction.HeldItem);
        requestingPlayer.netHeldItem.Value = updatedHeld;

        string feedback = interaction.FeedbackMessage;

        ClientRpcParams targetClient = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { senderClientId } }
        };

        SyncInteractionResultClientRpc(updatedHeld, feedback, targetClient);
    }

    [ClientRpc]
    private void SyncInteractionResultClientRpc(NetworkItemState itemState, string feedback, ClientRpcParams clientRpcParams = default)
    {
        PlayerInteraction localInteraction = FindLocalPlayerInteraction();
        if (localInteraction == null) return;

        localInteraction.ClearHeldItem();
        KitchenItem newItem = itemState.ToKitchenItem();
        if (newItem != null)
        {
            localInteraction.TryReceiveItem(newItem);
        }

        if (!string.IsNullOrEmpty(feedback))
        {
            localInteraction.SetFeedback(feedback);
        }
    }

    [ServerRpc]
    private void UpdateHeldItemServerRpc(NetworkItemState newState)
    {
        netHeldItem.Value = newState;
    }

    [ServerRpc]
    public void SetPlayerNameServerRpc(string requestedName)
    {
        requestedName = SanitizePlayerName(requestedName);
        playerName.Value = new FixedString32Bytes(requestedName);
    }

    private static string SanitizePlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Gracz";

        name = System.Text.RegularExpressions.Regex.Replace(name, "<.*?>", string.Empty);

        var sb = new System.Text.StringBuilder(name.Length);
        foreach (char c in name)
        {
            if (!char.IsControl(c)) sb.Append(c);
        }
        name = sb.ToString().Trim();

        if (string.IsNullOrWhiteSpace(name)) return "Gracz";
        if (name.Length > 20) name = name.Substring(0, 20);
        return name;
    }

    private void OnPlayerNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        if (nameLabel != null) nameLabel.text = newValue.ToString();
    }

    private void OnPlayerIndexChanged(int oldValue, int newValue)
    {
        if (!IsOwner && remotePlayerVisual != null)
        {
            int colorIndex = newValue % PlayerColors.Length;
            Renderer bodyRenderer = remotePlayerVisual.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = PlayerColors[colorIndex];
            }
        }
    }

    public string PlayerName => playerName.Value.ToString();
    public int PlayerIndex => playerIndex.Value;

    private static NetworkPlayer FindNetworkPlayerByClientId(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return null;

        NetworkClient client;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out client)) return null;
        if (client.PlayerObject == null) return null;

        return client.PlayerObject.GetComponent<NetworkPlayer>();
    }

    private static PlayerInteraction FindLocalPlayerInteraction()
    {
        NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (NetworkPlayer player in players)
        {
            if (player.IsOwner)
            {
                return player.GetComponent<PlayerInteraction>();
            }
        }
        return null;
    }

    public static NetworkPlayer FindLocalPlayer()
    {
        NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (NetworkPlayer player in players)
        {
            if (player.IsOwner)
            {
                return player;
            }
        }
        return null;
    }
}

public class HeldItemBob : MonoBehaviour
{
    public float amplitude = 0.01f;
    public float speed = 2.5f;
    private Vector3 basePosition;

    private void Start()
    {
        basePosition = transform.localPosition;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * amplitude;
        transform.localPosition = basePosition + new Vector3(0f, offset, 0f);
    }
}

/// <summary>
/// Remote player animation using PlayableGraph.
/// </summary>
public class RemotePlayerAnimator : MonoBehaviour
{
    public AnimationClip idleClip;
    public AnimationClip walkClip;

    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable walkPlayable;

    private float idleLength;
    private float walkLength;

    private Vector3 lastPosition;
    private float speedSmoothed;

    public void Initialize()
    {
        lastPosition = transform.position;

        if (idleClip != null && walkClip != null)
        {
            idleLength = idleClip.length;
            walkLength = walkClip.length;

            Animator animator = GetComponent<Animator>();
            if (animator == null) animator = gameObject.AddComponent<Animator>();

            graph = PlayableGraph.Create("RemotePlayerAnimGraph");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            output.SetSourcePlayable(mixer);

            idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
            walkPlayable = AnimationClipPlayable.Create(graph, walkClip);

            graph.Connect(idlePlayable, 0, mixer, 0);
            graph.Connect(walkPlayable, 0, mixer, 1);

            mixer.SetInputWeight(0, 1.0f);
            mixer.SetInputWeight(1, 0.0f);

            graph.Play();
        }
    }

    private void Update()
    {
        float delta = (transform.position - lastPosition).magnitude;
        float speed = delta / Mathf.Max(Time.deltaTime, 0.001f);
        lastPosition = transform.position;

        float targetWeight = Mathf.Clamp01(speed / 2.5f);
        speedSmoothed = Mathf.Lerp(speedSmoothed, targetWeight, Time.deltaTime * 10f);

        if (graph.IsValid() && mixer.IsValid())
        {
            mixer.SetInputWeight(0, 1.0f - speedSmoothed);
            mixer.SetInputWeight(1, speedSmoothed);

            if (idlePlayable.IsValid() && idleLength > 0f)
            {
                if (idlePlayable.GetTime() >= idleLength)
                {
                    idlePlayable.SetTime(idlePlayable.GetTime() % idleLength);
                }
            }

            if (walkPlayable.IsValid() && walkLength > 0f)
            {
                if (walkPlayable.GetTime() >= walkLength)
                {
                    walkPlayable.SetTime(walkPlayable.GetTime() % walkLength);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
