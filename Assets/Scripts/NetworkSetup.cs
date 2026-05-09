using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Programmatically creates and configures the NetworkManager with UnityTransport.
/// Handles player prefab registration and connection lifecycle using INetworkPrefabInstanceHandler.
/// Created by KitchenGameBootstrap before any network operations.
/// </summary>
using System.Reflection;
public class NetworkSetup : MonoBehaviour
{
    public static NetworkSetup Instance { get; private set; }

    private const ushort DefaultPort = 7777;
    private const int MaxPlayers = 4;
    private const uint PlayerPrefabHash = 1234567890u;

    private GameObject playerPrefabInstance;
    private bool isInitialized;

    public bool IsNetworkActive => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
    public bool IsClient => NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;
    public bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
    public int ConnectedPlayerCount => NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsList.Count : 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Initialize();
            return;
        }

        Destroy(gameObject);
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        EnsureNetworkManager();
    }

    private class PlayerPrefabHandler : INetworkPrefabInstanceHandler
    {
        private GameObject prefab;

        public PlayerPrefabHandler(GameObject prefab)
        {
            this.prefab = prefab;
        }

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            GameObject go = Object.Instantiate(prefab, position, rotation);
            go.SetActive(true);
            NetworkObject no = go.GetComponent<NetworkObject>();
            
            // Assign the same fake hash so the client matches it properly internally if needed
            var prop = typeof(NetworkObject).GetProperty("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                prop.SetValue(no, PlayerPrefabHash);
            }

            return no;
        }

        public void Destroy(NetworkObject networkObject)
        {
            Object.Destroy(networkObject.gameObject);
        }
    }

    private void EnsureNetworkManager()
    {
        if (NetworkManager.Singleton != null)
        {
            return;
        }

        NetworkManager networkManager = gameObject.AddComponent<NetworkManager>();

        UnityTransport transport = gameObject.AddComponent<UnityTransport>();
        transport.ConnectionData.Address = "127.0.0.1";
        transport.ConnectionData.Port = DefaultPort;
        transport.ConnectionData.ServerListenAddress = "0.0.0.0";

        // Long timeouts: server always accepts connections, no time limit on joining
        transport.DisconnectTimeoutMS = 300000;  // 5 minutes before considering client dead
        transport.ConnectTimeoutMS = 30000;       // 30 seconds to establish connection

        networkManager.NetworkConfig = new NetworkConfig();
        networkManager.NetworkConfig.NetworkTransport = transport;

        playerPrefabInstance = CreatePlayerPrefab();
        // We set PlayerPrefab to null and handle spawning manually to avoid auto-spawn failures
        networkManager.NetworkConfig.PlayerPrefab = null;
        networkManager.NetworkConfig.ConnectionApproval = false;

        // CRITICAL: Disable scene management — we have one scene, no need for scene sync.
        // Without this, NGO throws 'Scene Hash does not exist in HashToBuildIndex table'
        // because dynamically loaded scenes aren't in the Build Settings scene list.
        networkManager.NetworkConfig.EnableSceneManagement = false;

        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;

        Debug.Log("[NetworkSetup] NetworkManager skonfigurowany. Port: " + DefaultPort);
    }

    private GameObject CreatePlayerPrefab()
    {
        GameObject prefab = new GameObject("NetworkPlayerPrefab");
        prefab.SetActive(false);

        NetworkObject networkObject = prefab.AddComponent<NetworkObject>();

        NetworkPlayer networkPlayer = prefab.AddComponent<NetworkPlayer>();

        CharacterController controller = prefab.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.radius = 0.35f;
        controller.stepOffset = 0.06f;
        controller.slopeLimit = 45f;

        prefab.AddComponent<SimplePlayerController>();
        prefab.AddComponent<PlayerInteraction>();

        // NetworkTransform syncs position/rotation from server to clients.
        // Without this, client would stay at (0,0,0) even though server moved them to spawn point.
        NetworkTransform netTransform = prefab.AddComponent<NetworkTransform>();
        // Only server sets authoritative position (server-auth movement)
        netTransform.SyncPositionX = true;
        netTransform.SyncPositionY = true;
        netTransform.SyncPositionZ = true;
        netTransform.SyncRotAngleY = true;
        netTransform.SyncRotAngleX = false;
        netTransform.SyncRotAngleZ = false;

        DontDestroyOnLoad(prefab);
        return prefab;
    }

    /// <summary>
    /// Rejestruje handler prefabów graczy w NetworkManager.
    /// Wywoływane przed StartHost/StartClient (zarówno bezpośrednio, jak i przez RelayManager).
    /// </summary>
    public void RegisterPrefabHandler()
    {
        if (NetworkManager.Singleton != null && playerPrefabInstance != null)
        {
            // Remove previous handler if exists, then re-add
            try { NetworkManager.Singleton.PrefabHandler.RemoveHandler(PlayerPrefabHash); } catch { }
            NetworkManager.Singleton.PrefabHandler.AddHandler(PlayerPrefabHash, new PlayerPrefabHandler(playerPrefabInstance));
        }
    }

    public bool StartHost(string address = "0.0.0.0")
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkSetup] NetworkManager nie istnieje.");
            return false;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = "127.0.0.1";
            transport.ConnectionData.ServerListenAddress = address;
            transport.ConnectionData.Port = DefaultPort;
        }

        RegisterPrefabHandler();

        bool result = NetworkManager.Singleton.StartHost();
        Debug.Log("[NetworkSetup] StartHost: " + (result ? "OK" : "FAIL"));

        return result;
    }

    public bool StartClient(string ipAddress)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkSetup] NetworkManager nie istnieje.");
            return false;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = ipAddress;
            transport.ConnectionData.Port = DefaultPort;
        }

        RegisterPrefabHandler();

        bool result = NetworkManager.Singleton.StartClient();
        Debug.Log("[NetworkSetup] StartClient -> " + ipAddress + ": " + (result ? "OK" : "FAIL"));
        return result;
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("[NetworkSetup] Rozlaczono.");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("[NetworkSetup] Klient polaczony: " + clientId +
            " (lacznie: " + ConnectedPlayerCount + ")");

        if (NetworkManager.Singleton.IsServer)
        {
            // Server spawns the player object for the connected client
            GameObject playerInstance = Instantiate(playerPrefabInstance);
            playerInstance.SetActive(true);
            NetworkObject no = playerInstance.GetComponent<NetworkObject>();
            
            // Assign the custom hash via reflection before spawning
            var prop = typeof(NetworkObject).GetProperty("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                prop.SetValue(no, PlayerPrefabHash);
            }
            
            no.SpawnAsPlayerObject(clientId);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log("[NetworkSetup] Klient rozlaczony: " + clientId);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (playerPrefabInstance != null)
        {
            Destroy(playerPrefabInstance);
        }
    }
}
