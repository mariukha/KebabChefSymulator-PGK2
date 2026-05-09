using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// NetworkBehaviour wrapper for each connected player.
/// Handles owner-specific setup (camera, input) and remote player visualization.
/// Synchronizes held item state and player name across the network.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    private const float EyeHeight = 1.75f;

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

    public Camera PlayerCamera => playerCamera;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            int index = (int)(OwnerClientId % (ulong)SpawnPoints.Length);
            playerIndex.Value = index;
            // Default name — will be overridden by client's SetPlayerNameServerRpc
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
    }

    private void Start()
    {
        // Fallback: If not spawned correctly by NGO (e.g. dynamic prefab without hash),
        // we still need to initialize the local player so the game is playable.
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

        if (IsOwner && playerCamera != null)
        {
            Destroy(playerCamera.gameObject);
        }
    }

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

        // Configure controller
        cachedController = GetComponent<SimplePlayerController>();
        if (cachedController != null)
        {
            cachedController.playerCamera = playerCamera;
            cachedController.enabled = true;
        }

        // Configure interaction
        cachedInteraction = GetComponent<PlayerInteraction>();
        if (cachedInteraction != null)
        {
            cachedInteraction.playerCamera = playerCamera;
            cachedInteraction.interactableLayer = 1 << 6;
            cachedInteraction.interactionDistance = 5.5f;
            cachedInteraction.enabled = true;
        }

        // Ensure HUD exists
        if (FindFirstObjectByType<KitchenHUD>() == null)
        {
            GameObject hudObject = new GameObject("KitchenHUD");
            hudObject.AddComponent<KitchenHUD>();
        }

        if (FindFirstObjectByType<ShopUI>() == null)
        {
            GameObject shopUiObject = new GameObject("ShopUI");
            shopUiObject.AddComponent<ShopUI>();
        }

        if (FindFirstObjectByType<PlayerListUI>() == null)
        {
            GameObject playerListObject = new GameObject("PlayerListUI");
            playerListObject.AddComponent<PlayerListUI>();
        }

        // Lock cursor for FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Send player's chosen nickname to the server
        string nickname = LobbyUI.LocalPlayerNickname;
        if (IsSpawned)
        {
            SetPlayerNameServerRpc(nickname);
        }
        else
        {
            // Defer nickname send until spawned (can happen on client)
            pendingNickname = nickname;
        }

        // Look at customer area
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

        // Disable input components for remote players
        SimplePlayerController controller = GetComponent<SimplePlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        PlayerInteraction interaction = GetComponent<PlayerInteraction>();
        if (interaction != null)
        {
            interaction.enabled = false;
        }

        // Destroy any cameras that might have been created
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            Destroy(cam.gameObject);
        }

        // Create visible body for remote player
        CreateRemoteVisual();

        Debug.Log("[NetworkPlayer] Zdalny gracz skonfigurowany: " + OwnerClientId);
    }

    private void CreateRemoteVisual()
    {
        if (remotePlayerVisual != null)
        {
            return;
        }

        int colorIndex = playerIndex.Value % PlayerColors.Length;
        Color bodyColor = PlayerColors[colorIndex];

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        // Body capsule
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "RemoteBody";
        body.transform.SetParent(transform);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Renderer bodyRenderer = body.GetComponent<Renderer>();
        bodyRenderer.material = new Material(shader);
        bodyRenderer.material.color = bodyColor;

        Collider bodyCollider = body.GetComponent<Collider>();
        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        // Head sphere
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "RemoteHead";
        head.transform.SetParent(transform);
        head.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        Renderer headRenderer = head.GetComponent<Renderer>();
        headRenderer.material = new Material(shader);
        headRenderer.material.color = new Color(0.92f, 0.78f, 0.63f);

        Collider headCollider = head.GetComponent<Collider>();
        if (headCollider != null)
        {
            headCollider.enabled = false;
        }

        // Name label
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

    private void OnPlayerNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        if (nameLabel != null)
        {
            nameLabel.text = newValue.ToString();
        }
    }

    private string pendingNickname;
    private float nextHeldItemSyncTime;

    private void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        // Send deferred nickname if it was pending
        if (pendingNickname != null && IsOwner && IsSpawned)
        {
            SetPlayerNameServerRpc(pendingNickname);
            pendingNickname = null;
        }

        // Throttled held item sync: owner -> server -> all (max every 0.15s to reduce traffic)
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
    }

    [ServerRpc]
    private void UpdateHeldItemServerRpc(NetworkItemState newState)
    {
        netHeldItem.Value = newState;
    }

    /// <summary>
    /// Klient wysyła swój wybrany nick do serwera.
    /// Serwer zapisuje go w NetworkVariable, co propaguje do wszystkich klientów.
    /// </summary>
    [ServerRpc]
    public void SetPlayerNameServerRpc(string requestedName)
    {
        if (string.IsNullOrWhiteSpace(requestedName))
        {
            requestedName = "Gracz";
        }

        if (requestedName.Length > 20)
        {
            requestedName = requestedName.Substring(0, 20);
        }

        playerName.Value = new FixedString32Bytes(requestedName);
    }

    /// <summary>
    /// Publiczny dostęp do aktualnego nicku gracza.
    /// Używany przez PlayerListUI do wyświetlania listy graczy.
    /// </summary>
    public string PlayerName => playerName.Value.ToString();

    /// <summary>
    /// Publiczny dostęp do indeksu gracza (określa kolor i spawn point).
    /// </summary>
    public int PlayerIndex => playerIndex.Value;

    /// <summary>
    /// When playerIndex changes (e.g. from server initial assignment),
    /// update remote visual color so late-joining clients see correct colors.
    /// </summary>
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
}
