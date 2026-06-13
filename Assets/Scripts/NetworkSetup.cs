/// \file NetworkSetup.cs
/// \brief Plik zawierający klasę NetworkSetup odpowiedzialną za konfigurację i zarządzanie siecią.
/// \details Definiuje logikę inicjalizacji NetworkManager, tworzenia prefabu gracza,
/// uruchamiania hosta/klienta, obsługi połączeń i rozłączeń graczy
/// oraz zarządzania hashami identyfikacyjnymi obiektów sieciowych.

using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

using System.Reflection;

/// <summary>
/// Klasa singleton odpowiedzialna za konfigurację sieci w grze wieloosobowej.
/// </summary>
/// <remarks>
/// Zarządza cyklem życia <see cref="NetworkManager"/>:
/// <list type="bullet">
///   <item>Tworzenie i konfiguracja NetworkManager oraz UnityTransport</item>
///   <item>Dynamiczne tworzenie prefabu gracza z wymaganymi komponentami</item>
///   <item>Uruchamianie trybu hosta lub klienta</item>
///   <item>Obsługa zdarzeń połączenia i rozłączenia graczy</item>
///   <item>Spawnowanie obiektów graczy na serwerze</item>
/// </list>
/// Używa wzorca singleton z <see cref="Instance"/>.
/// </remarks>
public class NetworkSetup : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja singletona klasy <see cref="NetworkSetup"/>.
    /// </summary>
    /// <value>Jedyna aktywna instancja <see cref="NetworkSetup"/> lub <c>null</c>.</value>
    public static NetworkSetup Instance { get; private set; }

    /// <summary>
    /// Domyślny port sieciowy do nasłuchiwania i łączenia.
    /// </summary>
    private const ushort DefaultPort = 7777;

    /// <summary>
    /// Maksymalna liczba graczy w sesji.
    /// </summary>
    private const int MaxPlayers = 4;

    /// <summary>
    /// Stały hash identyfikacyjny prefabu gracza sieciowego.
    /// </summary>
    /// <remarks>
    /// Używany do rejestracji i rozpoznawania prefabu gracza
    /// przez system prefabów sieciowych Netcode.
    /// </remarks>
    private const uint PlayerPrefabHash = 1234567890u;

    /// <summary>
    /// Instancja dynamicznie utworzonego prefabu gracza.
    /// </summary>
    private GameObject playerPrefabInstance;

    /// <summary>
    /// Flaga określająca, czy konfiguracja sieci została już zainicjalizowana.
    /// </summary>
    private bool isInitialized;

    /// <summary>
    /// Właściwość sprawdzająca, czy sieć jest aktywna (NetworkManager nasłuchuje).
    /// </summary>
    /// <value><c>true</c> jeśli sieć jest aktywna; w przeciwnym razie <c>false</c>.</value>
    public bool IsNetworkActive => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    /// <summary>
    /// Właściwość sprawdzająca, czy lokalna instancja jest hostem (serwerem i klientem jednocześnie).
    /// </summary>
    /// <value><c>true</c> jeśli jest hostem; w przeciwnym razie <c>false</c>.</value>
    public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

    /// <summary>
    /// Właściwość sprawdzająca, czy lokalna instancja jest klientem.
    /// </summary>
    /// <value><c>true</c> jeśli jest klientem; w przeciwnym razie <c>false</c>.</value>
    public bool IsClient => NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;

    /// <summary>
    /// Właściwość sprawdzająca, czy lokalna instancja jest serwerem.
    /// </summary>
    /// <value><c>true</c> jeśli jest serwerem; w przeciwnym razie <c>false</c>.</value>
    public bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

    /// <summary>
    /// Właściwość zwracająca liczbę aktualnie połączonych graczy.
    /// </summary>
    /// <value>Liczba połączonych klientów lub 0 jeśli NetworkManager nie istnieje.</value>
    public int ConnectedPlayerCount => NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsList.Count : 0;

    /// <summary>
    /// Metoda Awake wywoływana przy tworzeniu obiektu.
    /// Implementuje wzorzec singleton — niszczy duplikaty.
    /// </summary>
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

    /// <summary>
    /// Inicjalizuje konfigurację sieci, jeśli nie została jeszcze zainicjalizowana.
    /// </summary>
    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        EnsureNetworkManager();
    }

    /// <summary>
    /// Wewnętrzna klasa obsługująca instancjonowanie i niszczenie prefabów graczy sieciowych.
    /// </summary>
    /// <remarks>
    /// Implementuje <see cref="INetworkPrefabInstanceHandler"/> do dynamicznego tworzenia
    /// obiektów graczy z odpowiednim hashem identyfikacyjnym.
    /// Używana przez system prefabów Netcode zamiast standardowego mechanizmu prefabów.
    /// </remarks>
    private class PlayerPrefabHandler : INetworkPrefabInstanceHandler
    {
        /// <summary>
        /// Referencja do prefabu gracza używanego do tworzenia instancji.
        /// </summary>
        private GameObject prefab;

        /// <summary>
        /// Konstruktor przyjmujący prefab gracza.
        /// </summary>
        /// <param name="prefab">Prefab gracza do instancjonowania.</param>
        public PlayerPrefabHandler(GameObject prefab)
        {
            this.prefab = prefab;
        }

        /// <summary>
        /// Tworzy nową instancję obiektu gracza sieciowego.
        /// </summary>
        /// <param name="ownerClientId">Identyfikator klienta, który będzie właścicielem obiektu.</param>
        /// <param name="position">Pozycja początkowa obiektu.</param>
        /// <param name="rotation">Rotacja początkowa obiektu.</param>
        /// <returns>Komponent <see cref="NetworkObject"/> nowo utworzonego obiektu gracza.</returns>
        /// <remarks>
        /// Ustawia hash identyfikacyjny obiektu za pomocą refleksji,
        /// aby zapewnić zgodność z systemem prefabów Netcode.
        /// </remarks>
        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            GameObject go = Object.Instantiate(prefab, position, rotation);
            go.SetActive(true);
            NetworkObject no = go.GetComponent<NetworkObject>();

            var prop = typeof(NetworkObject).GetProperty("GlobalObjectIdHash", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                prop.SetValue(no, PlayerPrefabHash);
            }

            return no;
        }

        /// <summary>
        /// Niszczy obiekt gracza sieciowego.
        /// </summary>
        /// <param name="networkObject">Obiekt sieciowy do zniszczenia.</param>
        public void Destroy(NetworkObject networkObject)
        {
            Object.Destroy(networkObject.gameObject);
        }
    }

    /// <summary>
    /// Upewnia się, że istnieje instancja <see cref="NetworkManager"/> i konfiguruje ją.
    /// </summary>
    /// <remarks>
    /// Tworzy <see cref="NetworkManager"/> i <see cref="UnityTransport"/> jako komponenty
    /// na tym samym obiekcie gry. Konfiguruje parametry transportu (adres, port, timeouty),
    /// tworzy prefab gracza i subskrybuje zdarzenia połączenia/rozłączenia.
    /// </remarks>
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

        transport.DisconnectTimeoutMS = 300000;
        transport.ConnectTimeoutMS = 30000;

        networkManager.NetworkConfig = new NetworkConfig();
        networkManager.NetworkConfig.NetworkTransport = transport;

        playerPrefabInstance = CreatePlayerPrefab();

        networkManager.NetworkConfig.PlayerPrefab = null;
        networkManager.NetworkConfig.ConnectionApproval = false;

        networkManager.NetworkConfig.EnableSceneManagement = false;

        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;

        Debug.Log("[NetworkSetup] NetworkManager skonfigurowany. Port: " + DefaultPort);
    }

    /// <summary>
    /// Tworzy dynamicznie prefab gracza sieciowego z wymaganymi komponentami.
    /// </summary>
    /// <returns>Utworzony obiekt prefabu gracza (nieaktywny, chroniony przed zniszczeniem przy zmianie sceny).</returns>
    /// <remarks>
    /// Dodaje następujące komponenty:
    /// <list type="bullet">
    ///   <item><see cref="NetworkObject"/> — identyfikacja obiektu sieciowego</item>
    ///   <item><see cref="NetworkPlayer"/> — logika gracza sieciowego</item>
    ///   <item><see cref="CharacterController"/> — fizyka ruchu postaci</item>
    ///   <item><see cref="SimplePlayerController"/> — kontroler ruchu</item>
    ///   <item><see cref="PlayerInteraction"/> — obsługa interakcji</item>
    ///   <item><see cref="ClientNetworkTransform"/> — synchronizacja transformacji po stronie klienta</item>
    /// </list>
    /// </remarks>
    private GameObject CreatePlayerPrefab()
    {
        GameObject prefab = new GameObject("NetworkPlayerPrefab");
        prefab.SetActive(false);

        NetworkObject networkObject = prefab.AddComponent<NetworkObject>();
        SetGlobalObjectIdHash(networkObject, PlayerPrefabHash);

        NetworkPlayer networkPlayer = prefab.AddComponent<NetworkPlayer>();

        CharacterController controller = prefab.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.radius = 0.35f;
        controller.stepOffset = 0.06f;
        controller.slopeLimit = 45f;

        prefab.AddComponent<SimplePlayerController>();
        prefab.AddComponent<PlayerInteraction>();

        ClientNetworkTransform netTransform = prefab.AddComponent<ClientNetworkTransform>();
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
    /// Rejestruje niestandardowy handler prefabu gracza w systemie prefabów Netcode.
    /// </summary>
    /// <remarks>
    /// Najpierw próbuje usunąć istniejący handler (ignorując błędy),
    /// a następnie dodaje nowy <see cref="PlayerPrefabHandler"/>.
    /// Powinno być wywoływane przed uruchomieniem hosta lub klienta.
    /// </remarks>
    public void RegisterPrefabHandler()
    {
        if (NetworkManager.Singleton != null && playerPrefabInstance != null)
        {

            try { NetworkManager.Singleton.PrefabHandler.RemoveHandler(PlayerPrefabHash); } catch { }
            NetworkManager.Singleton.PrefabHandler.AddHandler(PlayerPrefabHash, new PlayerPrefabHandler(playerPrefabInstance));
        }
    }

    /// <summary>
    /// Uruchamia tryb hosta (serwer + klient) na podanym adresie.
    /// </summary>
    /// <param name="address">Adres nasłuchiwania serwera. Domyślnie "0.0.0.0" (wszystkie interfejsy).</param>
    /// <returns><c>true</c> jeśli uruchomienie hosta powiodło się; w przeciwnym razie <c>false</c>.</returns>
    /// <remarks>
    /// Konfiguruje transport z adresem localhost i podanym adresem nasłuchiwania,
    /// rejestruje handler prefabu i uruchamia hosta przez <see cref="NetworkManager.StartHost"/>.
    /// </remarks>
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

    /// <summary>
    /// Uruchamia tryb klienta i łączy się z serwerem pod podanym adresem IP.
    /// </summary>
    /// <param name="ipAddress">Adres IP serwera, z którym klient ma się połączyć.</param>
    /// <returns><c>true</c> jeśli rozpoczęcie połączenia klienta powiodło się; w przeciwnym razie <c>false</c>.</returns>
    /// <remarks>
    /// Konfiguruje transport z podanym adresem i domyślnym portem,
    /// rejestruje handler prefabu i uruchamia klienta.
    /// </remarks>
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

    /// <summary>
    /// Rozłącza bieżącą sesję sieciową.
    /// </summary>
    /// <remarks>
    /// Wywołuje <see cref="NetworkManager.Shutdown"/> jeśli sieć jest aktualnie aktywna.
    /// </remarks>
    public void Disconnect()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("[NetworkSetup] Rozlaczono.");
        }
    }

    /// <summary>
    /// Obsługuje zdarzenie połączenia nowego klienta.
    /// Na serwerze tworzy i spawnuje obiekt gracza dla podłączonego klienta.
    /// </summary>
    /// <param name="clientId">Identyfikator połączonego klienta.</param>
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("[NetworkSetup] Klient polaczony: " + clientId +
            " (lacznie: " + ConnectedPlayerCount + ")");

        if (NetworkManager.Singleton.IsServer)
        {

            GameObject playerInstance = Instantiate(playerPrefabInstance);
            playerInstance.SetActive(true);
            NetworkObject no = playerInstance.GetComponent<NetworkObject>();

            SetGlobalObjectIdHash(no, PlayerPrefabHash);

            no.SpawnAsPlayerObject(clientId);
        }
    }

    /// <summary>
    /// Obsługuje zdarzenie rozłączenia klienta.
    /// Jeśli rozłączony jest klient lokalny, próbuje wrócić do ekranu lobby.
    /// </summary>
    /// <param name="clientId">Identyfikator rozłączonego klienta.</param>
    /// <remarks>
    /// Nie wykonuje żadnej akcji w trybie solo ani gdy menu główne jest otwarte.
    /// </remarks>
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log("[NetworkSetup] Klient rozlaczony: " + clientId);
        if (NetworkManager.Singleton != null
            && clientId == NetworkManager.Singleton.LocalClientId
            && !MainMenuUI.IsSoloMode)
        {
            MainMenuUI mainMenu = FindFirstObjectByType<MainMenuUI>();
            if (mainMenu != null && mainMenu.IsMenuOpen)
            {
                return;
            }

            var lobby = FindFirstObjectByType<LobbyUI>();
            if (lobby != null)
            {
                lobby.ShowLobby();
            }
        }
    }

    /// <summary>
    /// Ustawia hash identyfikacyjny (GlobalObjectIdHash) obiektu sieciowego za pomocą refleksji.
    /// </summary>
    /// <param name="networkObject">Obiekt sieciowy, którego hash ma zostać ustawiony.</param>
    /// <param name="hash">Wartość hasha do ustawienia.</param>
    /// <remarks>
    /// Próbuje ustawić hash przez:
    /// <list type="number">
    ///   <item>Znane nazwy pól ("GlobalObjectIdHash", "m_GlobalObjectIdHash", "m_PrefabHash")</item>
    ///   <item>Znane nazwy właściwości ("GlobalObjectIdHash", "PrefabHash")</item>
    ///   <item>Brute-force — wyszukiwanie pól typu <c>uint</c> zawierających "hash" w nazwie</item>
    /// </list>
    /// Jest to obejście braku publicznego API do ustawiania hasha w różnych wersjach Netcode.
    /// </remarks>
    public static void SetGlobalObjectIdHash(NetworkObject networkObject, uint hash)
    {
        System.Type type = typeof(NetworkObject);

        string[] fieldNames = { "GlobalObjectIdHash", "m_GlobalObjectIdHash", "m_PrefabHash" };
        foreach (string fieldName in fieldNames)
        {
            var field = type.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(networkObject, hash);
                Debug.Log("[NetworkSetup] Hash set via field '" + fieldName + "' = " + hash);
                return;
            }
        }

        string[] propNames = { "GlobalObjectIdHash", "PrefabHash" };
        foreach (string propName in propNames)
        {
            var prop = type.GetProperty(propName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(networkObject, hash);
                Debug.Log("[NetworkSetup] Hash set via property '" + propName + "' = " + hash);
                return;
            }
        }

        var allFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var f in allFields)
        {
            if (f.FieldType == typeof(uint) && f.Name.ToLower().Contains("hash"))
            {
                f.SetValue(networkObject, hash);
                Debug.Log("[NetworkSetup] Hash set via brute-force field '" + f.Name + "' = " + hash);
                return;
            }
        }

        Debug.LogError("[NetworkSetup] FAILED to set GlobalObjectIdHash! No matching field/property found. " +
            "Available fields: " + string.Join(", ", System.Array.ConvertAll(allFields, f => f.Name + ":" + f.FieldType.Name)));
    }

    /// <summary>
    /// Metoda wywoływana przy niszczeniu obiektu.
    /// Czyści referencję singletona i odsubskrybowuje zdarzenia NetworkManager.
    /// </summary>
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
