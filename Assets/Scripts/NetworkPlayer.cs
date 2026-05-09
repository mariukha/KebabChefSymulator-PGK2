using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// NetworkBehaviour wrapper for each connected player.
/// Handles owner-specific setup (camera, input) and remote player visualization.
/// Also acts as the central hub for ALL network sync:
///   - Station states (server -> clients via ClientRpc)
///   - Economy / Shop upgrades (server -> clients)
///   - Held item visuals (via NetworkVariable)
///   - Interaction routing (client -> server via ServerRpc)
/// This is the ONLY NetworkObject type in the game (besides NetworkManager).
/// </summary>
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

    private static readonly Color[] PlayerColors = new Color[]
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

    // Held item visual
    private GameObject heldItemVisual;
    private NetworkItemState lastVisualState;

    // Station sync state (server-side)
    private NetworkKitchenStation[] cachedStations;
    private float nextStationSyncTime;
    private float nextEconomySyncTime;
    private float nextShopSyncTime;

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
            SetupLocalPlayer();
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

    // =========================================================================
    //  LOCAL PLAYER SETUP
    // =========================================================================

    private bool isLocalPlayerSetup = false;

    private void SetupLocalPlayer()
    {
        if (isLocalPlayerSetup) return;
        isLocalPlayerSetup = true;

        gameObject.name = "Player_Local";

        // Create camera
        GameObject cameraObject = new GameObject("PlayerCamera");
        cameraObject.transform.SetParent(transform);
        cameraObject.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
        cameraObject.transform.localRotation = Quaternion.identity;
        playerCamera = cameraObject.AddComponent<Camera>();
        playerCamera.tag = "MainCamera";
        playerCamera.nearClipPlane = 0.1f;
        playerCamera.farClipPlane = 100f;

        cachedController = GetComponent<SimplePlayerController>();
        if (cachedController != null)
        {
            cachedController.playerCamera = playerCamera;
            cachedController.enabled = true;
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

    // =========================================================================
    //  REMOTE PLAYER VISUAL
    // =========================================================================

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

        int colorIndex = playerIndex.Value % PlayerColors.Length;
        Color bodyColor = PlayerColors[colorIndex];

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "RemoteBody";
        body.transform.SetParent(transform);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Renderer bodyRenderer = body.GetComponent<Renderer>();
        bodyRenderer.material = new Material(shader);
        bodyRenderer.material.color = bodyColor;
        Collider bodyCollider = body.GetComponent<Collider>();
        if (bodyCollider != null) bodyCollider.enabled = false;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "RemoteHead";
        head.transform.SetParent(transform);
        head.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        Renderer headRenderer = head.GetComponent<Renderer>();
        headRenderer.material = new Material(shader);
        headRenderer.material.color = new Color(0.92f, 0.78f, 0.63f);
        Collider headCollider = head.GetComponent<Collider>();
        if (headCollider != null) headCollider.enabled = false;

        GameObject labelObject = new GameObject("NameLabel");
        labelObject.transform.SetParent(transform);
        labelObject.transform.localPosition = new Vector3(0f, 2.1f, 0f);
        nameLabel = labelObject.AddComponent<TextMesh>();
        nameLabel.text = playerName.Value.ToString();
        nameLabel.characterSize = 0.04f;
        nameLabel.anchor = TextAnchor.MiddleCenter;
        nameLabel.alignment = TextAlignment.Center;
        nameLabel.fontSize = 18;
        nameLabel.color = new Color(1f, 1f, 1f, 0.85f);
        labelObject.AddComponent<BillboardLabel>();

        remotePlayerVisual = body;
    }

    // =========================================================================
    //  HELD ITEM VISUAL (visible to all players)
    // =========================================================================

    /// <summary>
    /// Creates or updates the 3D visual for the held item using real GLB models.
    /// For LOCAL player: attached to camera (visible in front of face).
    /// For REMOTE player: attached to body (visible to others).
    /// </summary>
    private void UpdateHeldItemVisual(NetworkItemState itemState)
    {
        // Destroy old visual if item changed or disappeared
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
        if (heldItemVisual != null) return; // already showing correct item

        bool isDish = itemState.isDish;

        if (IsOwner && playerCamera != null)
        {
            // Local player: model attached to camera, bottom-right of view
            Vector3 localPos = new Vector3(0.3f, -0.25f, 0.5f);
            Vector3 localRot = isDish ? new Vector3(0f, 0f, 90f) : new Vector3(15f, 25f, 0f);
            float modelSize = isDish ? 0.25f : 0.18f;

            heldItemVisual = KitchenItemVisualFactory.CreateItemVisual(
                itemState.ingredientKind, itemState.state, isDish,
                playerCamera.transform, localPos, localRot, modelSize);

            if (heldItemVisual != null)
            {
                HeldItemBob bob = heldItemVisual.AddComponent<HeldItemBob>();
                bob.amplitude = 0.008f;
                bob.speed = 2.5f;
            }
        }
        else
        {
            // Remote player: model at hand height
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

    // =========================================================================
    //  UPDATE LOOP
    // =========================================================================

    private string pendingNickname;
    private float nextHeldItemSyncTime;

    private void Update()
    {
        if (!IsSpawned) return;

        // Send deferred nickname
        if (pendingNickname != null && IsOwner)
        {
            SetPlayerNameServerRpc(pendingNickname);
            pendingNickname = null;
        }

        // Held item sync (owner -> server)
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

        // SERVER: broadcast station states + economy + shop to all clients
        if (IsServer && IsOwner)
        {
            BroadcastStationStates();
            BroadcastEconomy();
            BroadcastShopUpgrades();
        }
    }

    // =========================================================================
    //  STATION STATE SYNC (Server -> All Clients via ClientRpc)
    // =========================================================================

    private void BroadcastStationStates()
    {
        if (Time.time < nextStationSyncTime) return;
        nextStationSyncTime = Time.time + StationSyncInterval;

        if (cachedStations == null)
        {
            cachedStations = FindObjectsByType<NetworkKitchenStation>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        }

        // Send dirty stations
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
        // Server/host already has the correct state
        if (IsServer) return;

        // Find matching station on client by index
        if (cachedStations == null)
        {
            cachedStations = FindObjectsByType<NetworkKitchenStation>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
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

    // =========================================================================
    //  ECONOMY SYNC
    // =========================================================================

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

    // =========================================================================
    //  SHOP UPGRADE SYNC (Server -> All Clients)
    // =========================================================================

    private void BroadcastShopUpgrades()
    {
        if (Time.time < nextShopSyncTime) return;
        nextShopSyncTime = Time.time + ShopSyncInterval;

        if (ShopManager.Instance == null) return;

        // Pack all 5 upgrade levels into one RPC
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

    /// <summary>
    /// Client requests to purchase an upgrade. Server processes it and broadcasts result.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void PurchaseUpgradeServerRpc(int upgradeTypeInt, ServerRpcParams rpcParams = default)
    {
        UpgradeType type = (UpgradeType)upgradeTypeInt;
        bool success = false;

        if (ShopManager.Instance != null)
        {
            success = ShopManager.Instance.TryPurchaseUpgrade(type);
        }

        // Send result back to requesting client
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        ClientRpcParams targetClient = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { senderClientId } }
        };

        PurchaseResultClientRpc(success, upgradeTypeInt, targetClient);

        // Force immediate shop/economy sync to all clients
        if (success)
        {
            nextShopSyncTime = 0f;
            nextEconomySyncTime = 0f;
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

    // =========================================================================
    //  STATION INTERACTION (Client -> Server -> Client)
    // =========================================================================

    /// <summary>
    /// Client sends interaction request for a station identified by index.
    /// Server processes the interaction and sends back results.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void InteractWithStationServerRpc(int stationIndex, NetworkItemState heldItem, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        // Find the station on the server
        if (cachedStations == null)
        {
            cachedStations = FindObjectsByType<NetworkKitchenStation>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
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

        // Find the requesting player's PlayerInteraction on the server
        NetworkPlayer requestingPlayer = FindNetworkPlayerByClientId(senderClientId);
        if (requestingPlayer == null) return;

        PlayerInteraction interaction = requestingPlayer.GetComponent<PlayerInteraction>();
        if (interaction == null) return;

        // Sync held item from client -> server
        interaction.ClearHeldItem();
        KitchenItem clientItem = heldItem.ToKitchenItem();
        if (clientItem != null)
        {
            interaction.TryReceiveItem(clientItem);
        }

        // Process the interaction on the server
        targetStation.ServerInteract(interaction);

        // Send back results to the requesting client
        NetworkItemState updatedHeld = NetworkItemState.FromKitchenItem(interaction.HeldItem);
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

        // Update held item
        localInteraction.ClearHeldItem();
        KitchenItem newItem = itemState.ToKitchenItem();
        if (newItem != null)
        {
            localInteraction.TryReceiveItem(newItem);
        }

        // Show feedback
        if (!string.IsNullOrEmpty(feedback))
        {
            localInteraction.SetFeedback(feedback);
        }
    }

    // =========================================================================
    //  PLAYER RPCs
    // =========================================================================

    [ServerRpc]
    private void UpdateHeldItemServerRpc(NetworkItemState newState)
    {
        netHeldItem.Value = newState;
    }

    [ServerRpc]
    public void SetPlayerNameServerRpc(string requestedName)
    {
        if (string.IsNullOrWhiteSpace(requestedName)) requestedName = "Gracz";
        if (requestedName.Length > 20) requestedName = requestedName.Substring(0, 20);
        playerName.Value = new FixedString32Bytes(requestedName);
    }

    // =========================================================================
    //  VALUE CHANGED HANDLERS
    // =========================================================================

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

    // =========================================================================
    //  PUBLIC ACCESSORS
    // =========================================================================

    public string PlayerName => playerName.Value.ToString();
    public int PlayerIndex => playerIndex.Value;

    // =========================================================================
    //  HELPERS
    // =========================================================================

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

    /// <summary>
    /// Finds the local player's NetworkPlayer instance.
    /// </summary>
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

/// <summary>
/// Simple bobbing animation for held item visual.
/// </summary>
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
