/// \file NetworkPlayer.cs
/// \brief Plik zawierający klasę NetworkPlayer oraz klasy pomocnicze HeldItemBob i RemotePlayerAnimator.
/// \details Definiuje logikę gracza sieciowego, w tym konfigurację kamery, synchronizację stanu
/// trzymanego przedmiotu, nadawanie stanów stacji kuchennych, ekonomii, zamówień, ulepszeń sklepowych
/// oraz efektów wizualnych (VFX) pomiędzy serwerem a klientami w grze wieloosobowej.

using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Linq;

/// <summary>
/// Typ efektu wizualnego (VFX) przesyłanego przez sieć.
/// </summary>
/// <remarks>
/// Używany do identyfikacji rodzaju efektu, który ma zostać odtworzony
/// na klientach po wywołaniu przez serwer.
/// </remarks>
public enum NetworkVFXType
{
    /// <summary>Efekt pary wodnej (np. z garnka).</summary>
    Steam,
    /// <summary>Zatrzymanie efektu pary wodnej.</summary>
    StopSteam,
    /// <summary>Efekt dymu z pieca do kebaba (doner).</summary>
    DonerSmoke,
    /// <summary>Zatrzymanie efektu dymu z pieca do kebaba.</summary>
    StopDonerSmoke,
    /// <summary>Efekt krojenia składnika.</summary>
    Chop,
    /// <summary>Efekt podnoszenia przedmiotu.</summary>
    Pickup,
    /// <summary>Efekt odkładania przedmiotu.</summary>
    Drop,
    /// <summary>Efekt gotowości składnika.</summary>
    Ready,
    /// <summary>Efekt zawijania kebaba.</summary>
    Wrap,
    /// <summary>Efekt ulepszenia zakupionego w sklepie.</summary>
    Upgrade,
    /// <summary>Efekt pieniędzy (np. przy transakcji).</summary>
    Money,
    /// <summary>Efekt udanej dostawy zamówienia.</summary>
    DeliverySuccess,
    /// <summary>Efekt nieudanej dostawy zamówienia.</summary>
    DeliveryFail,
    /// <summary>Efekt przekroczenia limitu czasu zamówienia.</summary>
    Timeout
}

/// <summary>
/// Główna klasa gracza sieciowego odpowiedzialna za zarządzanie lokalnym i zdalnym graczem.
/// </summary>
/// <remarks>
/// Dziedziczy po <see cref="NetworkBehaviour"/> i obsługuje:
/// <list type="bullet">
///   <item>Konfigurację kamery i kontrolera gracza lokalnego</item>
///   <item>Tworzenie wizualnej reprezentacji graczy zdalnych</item>
///   <item>Synchronizację trzymanego przedmiotu, nazwy gracza i indeksu</item>
///   <item>Nadawanie stanów stacji kuchennych, ekonomii, sklepu i zamówień</item>
///   <item>Obsługę interakcji ze stacjami kuchennymi przez RPC</item>
///   <item>Nadawanie efektów wizualnych (VFX) do klientów</item>
/// </list>
/// </remarks>
public class NetworkPlayer : NetworkBehaviour
{
    /// <summary>
    /// Statyczna referencja do lokalnej instancji gracza sieciowego.
    /// </summary>
    /// <value>Instancja <see cref="NetworkPlayer"/> należąca do lokalnego gracza lub <c>null</c>.</value>
    public static NetworkPlayer LocalInstance { get; private set; }

    /// <summary>
    /// Wysokość oczu gracza nad poziomem podłogi (w metrach).
    /// Używana do pozycjonowania kamery gracza.
    /// </summary>
    private const float EyeHeight = 1.75f;

    /// <summary>
    /// Interwał synchronizacji stanów stacji kuchennych (w sekundach).
    /// </summary>
    private const float StationSyncInterval = 0.15f;

    /// <summary>
    /// Interwał synchronizacji danych ekonomicznych (w sekundach).
    /// </summary>
    private const float EconomySyncInterval = 0.5f;

    /// <summary>
    /// Interwał synchronizacji ulepszeń sklepowych (w sekundach).
    /// </summary>
    private const float ShopSyncInterval = 1.0f;

    /// <summary>
    /// Tablica predefiniowanych punktów spawnu graczy na mapie.
    /// </summary>
    /// <remarks>
    /// Pozycje są przypisywane graczom cyklicznie na podstawie ich identyfikatora klienta.
    /// </remarks>
    private static readonly Vector3[] SpawnPoints = new Vector3[]
    {
        new Vector3(0f, 0f, -1.9f),
        new Vector3(-2f, 0f, -1.9f),
        new Vector3(2f, 0f, -1.9f),
        new Vector3(0f, 0f, -0.5f)
    };

    /// <summary>
    /// Tablica kolorów przypisywanych graczom na podstawie ich indeksu.
    /// </summary>
    /// <remarks>
    /// Kolory służą do wizualnego rozróżnienia graczy zdalnych.
    /// Kolejno: niebieski, czerwony, zielony, żółty.
    /// </remarks>
    public static readonly Color[] PlayerColors = new Color[]
    {
        new Color(0.2f, 0.6f, 0.9f),
        new Color(0.9f, 0.4f, 0.3f),
        new Color(0.3f, 0.8f, 0.4f),
        new Color(0.9f, 0.75f, 0.2f)
    };

    /// <summary>
    /// Zmienna sieciowa przechowująca nazwę (pseudonim) gracza.
    /// </summary>
    /// <remarks>
    /// Odczytywalna przez wszystkich klientów, zapisywalna tylko przez serwer.
    /// Domyślna wartość to "Gracz".
    /// </remarks>
    private NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        "Gracz",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    /// <summary>
    /// Zmienna sieciowa przechowująca stan trzymanego przedmiotu kuchennego.
    /// </summary>
    /// <remarks>
    /// Używana do synchronizacji wizualnej reprezentacji przedmiotu trzymanego przez gracza
    /// pomiędzy wszystkimi klientami.
    /// </remarks>
    private NetworkVariable<NetworkItemState> netHeldItem = new NetworkVariable<NetworkItemState>(
        NetworkItemState.Empty(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    /// <summary>
    /// Zmienna sieciowa przechowująca indeks gracza.
    /// </summary>
    /// <remarks>
    /// Indeks określa punkt spawnu oraz kolor gracza.
    /// Przypisywany na serwerze na podstawie identyfikatora klienta.
    /// </remarks>
    private NetworkVariable<int> playerIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    /// <summary>
    /// Referencja do kamery gracza lokalnego.
    /// </summary>
    private Camera playerCamera;

    /// <summary>
    /// Obiekt wizualny reprezentujący gracza zdalnego (model 3D).
    /// </summary>
    private GameObject remotePlayerVisual;

    /// <summary>
    /// Etykieta tekstowa wyświetlająca nazwę gracza zdalnego nad jego głową.
    /// </summary>
    private TextMesh nameLabel;

    /// <summary>
    /// Buforowana referencja do komponentu kontrolera ruchu gracza.
    /// </summary>
    private SimplePlayerController cachedController;

    /// <summary>
    /// Buforowana referencja do komponentu interakcji gracza.
    /// </summary>
    private PlayerInteraction cachedInteraction;

    /// <summary>
    /// Obiekt wizualny reprezentujący przedmiot trzymany przez gracza.
    /// </summary>
    private GameObject heldItemVisual;

    /// <summary>
    /// Ostatni zsynchronizowany stan wizualny trzymanego przedmiotu.
    /// Używany do wykrywania zmian i unikania niepotrzebnego odtwarzania wizualizacji.
    /// </summary>
    private NetworkItemState lastVisualState;

    /// <summary>
    /// Buforowana tablica wszystkich stacji kuchennych na scenie.
    /// </summary>
    private NetworkKitchenStation[] cachedStations;

    /// <summary>
    /// Czas ostatniego odświeżenia buforowanej tablicy stacji kuchennych.
    /// </summary>
    private float cachedStationsTime;

    /// <summary>
    /// Czas następnej synchronizacji stanów stacji kuchennych.
    /// </summary>
    private float nextStationSyncTime;

    /// <summary>
    /// Czas następnej synchronizacji danych ekonomicznych.
    /// </summary>
    private float nextEconomySyncTime;

    /// <summary>
    /// Czas następnej synchronizacji ulepszeń sklepowych.
    /// </summary>
    private float nextShopSyncTime;

    /// <summary>
    /// Czas następnej synchronizacji zamówień.
    /// </summary>
    private float nextOrderSyncTime;

    /// <summary>
    /// Właściwość zwracająca kamerę gracza.
    /// </summary>
    /// <value>Obiekt <see cref="Camera"/> przypisany do gracza lokalnego lub <c>null</c> dla graczy zdalnych.</value>
    public Camera PlayerCamera => playerCamera;

    /// <summary>
    /// Wywoływana po pojawieniu się obiektu sieciowego.
    /// Konfiguruje gracza lokalnego lub zdalnego oraz subskrybuje zmiany zmiennych sieciowych.
    /// </summary>
    /// <remarks>
    /// Na serwerze przypisuje indeks gracza, punkt spawnu i domyślną nazwę.
    /// Dla właściciela wywołuje <see cref="SetupLocalPlayer"/>, w przeciwnym razie <see cref="SetupRemotePlayer"/>.
    /// </remarks>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner) LocalInstance = this;

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

    /// <summary>
    /// Metoda Start wywoływana przez Unity w pierwszej klatce.
    /// </summary>
    /// <remarks>
    /// Zapewnia konfigurację gracza lokalnego w trybie hosta,
    /// gdy obiekt nie został jeszcze zdespawnowany sieciowo.
    /// </remarks>
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

    /// <summary>
    /// Wywoływana przy usunięciu obiektu sieciowego ze sceny.
    /// Odsubskrybowuje zdarzenia zmiennych sieciowych i niszczy obiekty pomocnicze.
    /// </summary>
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

    /// <summary>
    /// Flaga zapobiegająca wielokrotnej konfiguracji gracza lokalnego.
    /// </summary>
    private bool isLocalPlayerSetup = false;

    /// <summary>
    /// Konfiguruje gracza lokalnego: tworzy kamerę, włącza kontroler ruchu i interakcję,
    /// inicjalizuje interfejsy HUD i ustawia kursor.
    /// </summary>
    /// <remarks>
    /// Metoda jest chroniona flagą <see cref="isLocalPlayerSetup"/> przed wielokrotnym wywołaniem.
    /// Tworzy kamerę z odpowiednimi ustawieniami URP (post-processing, antyaliasing),
    /// konfiguruje <see cref="SimplePlayerController"/>, <see cref="PlayerInteraction"/>,
    /// oraz tworzy niezbędne komponenty HUD (<see cref="KitchenHUD"/>, <see cref="InteractionHighlight"/>,
    /// <see cref="ShopUI"/>, <see cref="PlayerListUI"/>) jeśli nie istnieją na scenie.
    /// Wysyła pseudonim gracza na serwer.
    /// </remarks>
    private void SetupLocalPlayer()
    {
        if (isLocalPlayerSetup) return;
        isLocalPlayerSetup = true;

        gameObject.name = "Player_Local";

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

    /// <summary>
    /// Konfiguruje gracza zdalnego: wyłącza kontroler i interakcję,
    /// usuwa kamerę i tworzy wizualną reprezentację gracza.
    /// </summary>
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

    /// <summary>
    /// Tworzy wizualną reprezentację gracza zdalnego (model 3D, etykietę z nazwą).
    /// </summary>
    /// <remarks>
    /// Próbuje załadować model FBX z Resources ("Models/Gracz_Idle").
    /// Jeśli model nie jest dostępny, tworzy prostą geometrię zastępczą (kapsułę).
    /// Przypisuje kolor gracza na podstawie indeksu i inicjalizuje animator.
    /// Dodaje etykietę tekstową z nazwą gracza jako billboard.
    /// </remarks>
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

    /// <summary>
    /// Wyłącza komponent Collider na podanym obiekcie gry.
    /// </summary>
    /// <param name="obj">Obiekt gry, na którym należy wyłączyć zderzacz.</param>
    private void DisableCollider(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    /// <summary>
    /// Wyszukuje kość prawej dłoni w hierarchii modelu gracza zdalnego.
    /// </summary>
    /// <returns>
    /// <see cref="Transform"/> kości prawej dłoni, jeśli została znaleziona;
    /// w przeciwnym razie zwraca transform samego gracza.
    /// </returns>
    /// <remarks>
    /// Pomija kości palców (Thumb, Index, Middle, Ring, Pinky), szukając głównej kości "RightHand".
    /// </remarks>
    private Transform GetRightHandBone()
    {
        if (remotePlayerVisual != null)
        {
            Transform[] allBones = remotePlayerVisual.GetComponentsInChildren<Transform>();
            foreach (Transform t in allBones)
            {
                if (t.name.Contains("RightHand") && !t.name.Contains("Thumb") && !t.name.Contains("Index") && 
                    !t.name.Contains("Middle") && !t.name.Contains("Ring") && !t.name.Contains("Pinky"))
                {
                    return t;
                }
            }
        }
        return transform;
    }

    /// <summary>
    /// Aktualizuje wizualną reprezentację przedmiotu trzymanego przez gracza.
    /// </summary>
    /// <param name="itemState">Nowy stan trzymanego przedmiotu sieciowego.</param>
    /// <remarks>
    /// Niszczy starą wizualizację, jeśli stan przedmiotu się zmienił,
    /// i tworzy nową za pomocą <see cref="KitchenItemVisualFactory"/>.
    /// Dla graczy zdalnych przyczepia wizualizację do kości prawej ręki lub
    /// do transform gracza jako fallback.
    /// </remarks>
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
            Transform parentBone = GetRightHandBone();
            Vector3 localPos;
            Vector3 localRot;

            if (parentBone == transform)
            {
                localPos = new Vector3(0.25f, 1.2f, 0.35f);
                localRot = isDish ? new Vector3(0f, 0f, 90f) : Vector3.zero;
            }
            else
            {
                localPos = new Vector3(0.12f, 0.04f, 0.02f);
                localRot = isDish ? new Vector3(0f, 90f, 90f) : new Vector3(0f, 90f, 0f);
            }

            float modelSize = isDish ? 0.3f : 0.2f;

            heldItemVisual = KitchenItemVisualFactory.CreateItemVisual(
                itemState.ingredientKind, itemState.state, isDish,
                parentBone, localPos, localRot, modelSize);
        }
    }

    /// <summary>
    /// Wywoływana przy zmianie wartości zmiennej sieciowej trzymanego przedmiotu.
    /// Aktualizuje wizualizację przedmiotu.
    /// </summary>
    /// <param name="oldValue">Poprzedni stan przedmiotu.</param>
    /// <param name="newValue">Nowy stan przedmiotu.</param>
    private void OnHeldItemChanged(NetworkItemState oldValue, NetworkItemState newValue)
    {
        UpdateHeldItemVisual(newValue);
    }

    /// <summary>
    /// Pseudonim oczekujący na wysłanie do serwera (używany, gdy obiekt nie został jeszcze zdespawnowany sieciowo).
    /// </summary>
    private string pendingNickname;

    /// <summary>
    /// Czas następnej synchronizacji stanu trzymanego przedmiotu.
    /// </summary>
    private float nextHeldItemSyncTime;

    /// <summary>
    /// Metoda Update wywoływana co klatkę.
    /// Synchronizuje trzymany przedmiot, nadaje stany stacji, ekonomii, sklepu i zamówień.
    /// </summary>
    /// <remarks>
    /// Dla właściciela synchronizuje stan trzymanego przedmiotu z serwerem co 0.15 sekundy.
    /// Dla hosta (serwer + właściciel) nadaje stany stacji kuchennych, ekonomii, ulepszeń i zamówień.
    /// </remarks>
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

    /// <summary>
    /// Nadaje stany wszystkich zmienionych stacji kuchennych do klientów.
    /// </summary>
    /// <remarks>
    /// Wywoływana cyklicznie co <see cref="StationSyncInterval"/> sekund.
    /// Buforuje tablicę stacji i odświeża ją co 5 sekund.
    /// Wysyła migawkę stanu tylko dla stacji, których stan się zmienił.
    /// </remarks>
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

    /// <summary>
    /// RPC klienta synchronizujący stan stacji kuchennej odebrany od serwera.
    /// </summary>
    /// <param name="snapshot">Migawka stanu stacji kuchennej do zastosowania.</param>
    /// <remarks>
    /// Ignorowana na serwerze (serwer jest źródłem prawdy).
    /// Wyszukuje odpowiednią stację po indeksie i stosuje migawkę.
    /// </remarks>
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

    /// <summary>
    /// Nadaje dane ekonomiczne (saldo i łączne zarobki) do wszystkich klientów.
    /// </summary>
    /// <remarks>
    /// Wywoływana cyklicznie co <see cref="EconomySyncInterval"/> sekund.
    /// Pobiera dane z <see cref="EconomyManager.Instance"/>.
    /// </remarks>
    private void BroadcastEconomy()
    {
        if (Time.time < nextEconomySyncTime) return;
        nextEconomySyncTime = Time.time + EconomySyncInterval;

        if (EconomyManager.Instance != null)
        {
            SyncEconomyClientRpc(EconomyManager.Instance.CurrentBalance, EconomyManager.Instance.TotalEarned);
        }
    }

    /// <summary>
    /// RPC klienta synchronizujący dane ekonomiczne od serwera.
    /// </summary>
    /// <param name="balance">Aktualne saldo gracza.</param>
    /// <param name="totalEarned">Łączna kwota zarobionych pieniędzy.</param>
    [ClientRpc]
    private void SyncEconomyClientRpc(float balance, float totalEarned)
    {
        if (IsServer) return;
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.SetBalanceFromNetwork(balance, totalEarned);
        }
    }

    /// <summary>
    /// Nadaje aktualny stan zamówień do wszystkich klientów.
    /// </summary>
    /// <remarks>
    /// Wywoływana co 0.25 sekundy. Przesyła indeks szablonu zamówienia, opis,
    /// pozostały czas, liczbę ukończonych i nieudanych zamówień.
    /// </remarks>
    private void BroadcastOrders()
    {
        if (Time.time < nextOrderSyncTime) return;
        nextOrderSyncTime = Time.time + 0.25f;

        if (OrderManager.Instance != null)
        {
            int index = OrderManager.Instance.ActiveTemplateIndex;
            string desc = OrderManager.Instance.ActiveOrderDescription ?? "";
            float time = OrderManager.Instance.RemainingOrderTime;
            int comp = OrderManager.Instance.CompletedOrders;
            int fail = OrderManager.Instance.FailedOrders;

            SyncOrdersClientRpc(index, desc, time, comp, fail);
        }
    }

    /// <summary>
    /// RPC klienta synchronizujący stan zamówień od serwera.
    /// </summary>
    /// <param name="templateIndex">Indeks aktywnego szablonu zamówienia.</param>
    /// <param name="desc">Opis aktywnego zamówienia.</param>
    /// <param name="time">Pozostały czas na realizację zamówienia.</param>
    /// <param name="comp">Liczba ukończonych zamówień.</param>
    /// <param name="fail">Liczba nieudanych zamówień.</param>
    [ClientRpc]
    private void SyncOrdersClientRpc(int templateIndex, string desc, float time, int comp, int fail)
    {
        if (IsServer) return;
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.SyncNetworkState(templateIndex, desc, time, comp, fail);
        }
    }

    /// <summary>
    /// Nadaje efekt wizualny (VFX) do wszystkich klientów.
    /// </summary>
    /// <param name="type">Typ efektu wizualnego do odtworzenia.</param>
    /// <param name="pos">Pozycja świata, w której efekt ma zostać odtworzony.</param>
    /// <param name="color">Opcjonalny kolor efektu. Domyślnie <c>default</c>.</param>
    /// <remarks>
    /// Może być wywoływana tylko na serwerze. Wysyła żądanie odtworzenia VFX do klientów.
    /// </remarks>
    public void BroadcastVFX(NetworkVFXType type, Vector3 pos, Color color = default)
    {
        if (!IsServer) return;
        PlayVFXClientRpc(type, pos, color);
    }

    /// <summary>
    /// RPC klienta odtwarzający efekt wizualny na podstawie odebranego typu.
    /// </summary>
    /// <param name="type">Typ efektu wizualnego.</param>
    /// <param name="pos">Pozycja odtworzenia efektu.</param>
    /// <param name="color">Kolor efektu.</param>
    /// <remarks>
    /// Ignorowana na serwerze (serwer sam odtwarza efekty).
    /// Deleguje odtworzenie do odpowiedniej metody <see cref="VFXManager"/>.
    /// </remarks>
    [ClientRpc]
    private void PlayVFXClientRpc(NetworkVFXType type, Vector3 pos, Color color)
    {
        if (IsServer) return; 
        if (VFXManager.Instance == null) return;

        switch (type)
        {
            case NetworkVFXType.Steam: VFXManager.Instance.PlaySteamEffectLocal(pos); break;
            case NetworkVFXType.StopSteam: VFXManager.Instance.StopSteamEffectLocal(pos); break;
            case NetworkVFXType.DonerSmoke: VFXManager.Instance.PlayDonerSmokeEffectLocal(pos); break;
            case NetworkVFXType.StopDonerSmoke: VFXManager.Instance.StopDonerSmokeEffectLocal(pos); break;
            case NetworkVFXType.Chop: VFXManager.Instance.PlayChopEffectLocal(pos, color); break;
            case NetworkVFXType.Pickup: VFXManager.Instance.PlayPickupEffectLocal(pos, color); break;
            case NetworkVFXType.Drop: VFXManager.Instance.PlayDropEffectLocal(pos); break;
            case NetworkVFXType.Ready: VFXManager.Instance.PlayReadyEffectLocal(pos, color); break;
            case NetworkVFXType.Wrap: VFXManager.Instance.PlayWrapEffectLocal(pos); break;
            case NetworkVFXType.Upgrade: VFXManager.Instance.PlayUpgradeEffectLocal(pos, color); break;
            case NetworkVFXType.Money: VFXManager.Instance.PlayMoneyEffectLocal(pos); break;
            case NetworkVFXType.DeliverySuccess: VFXManager.Instance.PlayDeliverySuccessEffectLocal(pos); break;
            case NetworkVFXType.DeliveryFail: VFXManager.Instance.PlayDeliveryFailEffectLocal(pos); break;
            case NetworkVFXType.Timeout: VFXManager.Instance.PlayTimeoutEffectLocal(); break;
        }
    }

    /// <summary>
    /// Nadaje poziomy ulepszeń sklepowych do wszystkich klientów.
    /// </summary>
    /// <remarks>
    /// Wywoływana cyklicznie co <see cref="ShopSyncInterval"/> sekund.
    /// Pobiera poziomy ulepszeń z <see cref="ShopManager"/> i wysyła je via ClientRpc.
    /// </remarks>
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

    /// <summary>
    /// RPC klienta synchronizujący poziomy ulepszeń sklepowych od serwera.
    /// </summary>
    /// <param name="grillLvl">Poziom ulepszenia szybkości grilla.</param>
    /// <param name="cutLvl">Poziom ulepszenia szybkości krojenia.</param>
    /// <param name="rewardLvl">Poziom ulepszenia bonusu nagrody.</param>
    /// <param name="timeLvl">Poziom ulepszenia czasu zamówienia.</param>
    /// <param name="meatLvl">Poziom ulepszenia wielkości porcji mięsa.</param>
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
    /// RPC serwera obsługujący żądanie zakupu ulepszenia od klienta.
    /// </summary>
    /// <param name="upgradeTypeInt">Typ ulepszenia jako wartość całkowita enum <see cref="UpgradeType"/>.</param>
    /// <param name="rpcParams">Parametry RPC zawierające identyfikator wysyłającego klienta.</param>
    /// <remarks>
    /// Próbuje dokonać zakupu przez <see cref="ShopManager.TryPurchaseUpgrade"/>.
    /// W przypadku sukcesu wymusza natychmiastową synchronizację sklepu i ekonomii.
    /// Wysyła wynik zakupu do odpowiedniego klienta.
    /// </remarks>
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

    /// <summary>
    /// RPC klienta informujący o wyniku próby zakupu ulepszenia.
    /// </summary>
    /// <param name="success">Czy zakup się powiódł.</param>
    /// <param name="upgradeTypeInt">Typ ulepszenia jako wartość całkowita enum <see cref="UpgradeType"/>.</param>
    /// <param name="clientRpcParams">Opcjonalne parametry RPC do wysyłki celowanej do konkretnego klienta.</param>
    [ClientRpc]
    private void PurchaseResultClientRpc(bool success, int upgradeTypeInt, ClientRpcParams clientRpcParams = default)
    {
        ShopUI shopUI = FindFirstObjectByType<ShopUI>();
        if (shopUI != null)
        {
            shopUI.HandlePurchaseResult(success, (UpgradeType)upgradeTypeInt);
        }
    }

    /// <summary>
    /// RPC serwera obsługujący żądanie interakcji gracza ze stacją kuchenną.
    /// </summary>
    /// <param name="stationIndex">Indeks stacji kuchennej, z którą gracz chce wejść w interakcję.</param>
    /// <param name="heldItem">Stan przedmiotu trzymanego przez gracza w momencie żądania.</param>
    /// <param name="rpcParams">Parametry RPC zawierające identyfikator wysyłającego klienta.</param>
    /// <remarks>
    /// Serwer rekonstruuje stan gracza (przedmiot w ręku) na podstawie zmiennej sieciowej,
    /// wykonuje interakcję ze stacją, a następnie synchronizuje wynik z powrotem do klienta.
    /// Wysyła zaktualizowany stan trzymanego przedmiotu i komunikat zwrotny.
    /// </remarks>
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

    /// <summary>
    /// RPC klienta synchronizujący wynik interakcji ze stacją kuchenną.
    /// </summary>
    /// <param name="itemState">Zaktualizowany stan trzymanego przedmiotu po interakcji.</param>
    /// <param name="feedback">Komunikat zwrotny do wyświetlenia graczowi (np. informacja o postępie).</param>
    /// <param name="clientRpcParams">Parametry RPC do wysyłki celowanej.</param>
    /// <remarks>
    /// Aktualizuje trzymany przedmiot i komunikat zwrotny gracza lokalnego
    /// po przetworzeniu interakcji przez serwer.
    /// </remarks>
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

    /// <summary>
    /// RPC serwera aktualizujący stan trzymanego przedmiotu w zmiennej sieciowej.
    /// </summary>
    /// <param name="newState">Nowy stan przedmiotu trzymanego przez gracza.</param>
    [ServerRpc]
    private void UpdateHeldItemServerRpc(NetworkItemState newState)
    {
        netHeldItem.Value = newState;
    }

    /// <summary>
    /// RPC serwera ustawiający nazwę (pseudonim) gracza.
    /// </summary>
    /// <param name="requestedName">Żądana nazwa gracza od klienta.</param>
    /// <remarks>
    /// Nazwa jest sanityzowana przed zapisaniem do zmiennej sieciowej
    /// za pomocą <see cref="SanitizePlayerName"/>.
    /// </remarks>
    [ServerRpc]
    public void SetPlayerNameServerRpc(string requestedName)
    {
        requestedName = SanitizePlayerName(requestedName);
        playerName.Value = new FixedString32Bytes(requestedName);
    }

    /// <summary>
    /// Oczyszcza nazwę gracza z niebezpiecznych znaków i tagów.
    /// </summary>
    /// <param name="name">Surowa nazwa do oczyszczenia.</param>
    /// <returns>Oczyszczona nazwa gracza, ograniczona do 20 znaków. Domyślnie "Gracz" jeśli pusta.</returns>
    /// <remarks>
    /// Usuwa tagi HTML/XML, znaki sterujące, białe znaki na początku/końcu
    /// oraz ogranicza długość do 20 znaków.
    /// </remarks>
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

    /// <summary>
    /// Obsługuje zmianę nazwy gracza — aktualizuje etykietę tekstową nad głową gracza zdalnego.
    /// </summary>
    /// <param name="oldValue">Poprzednia nazwa gracza.</param>
    /// <param name="newValue">Nowa nazwa gracza.</param>
    private void OnPlayerNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        if (nameLabel != null) nameLabel.text = newValue.ToString();
    }

    /// <summary>
    /// Obsługuje zmianę indeksu gracza — aktualizuje kolor modelu gracza zdalnego.
    /// </summary>
    /// <param name="oldValue">Poprzedni indeks gracza.</param>
    /// <param name="newValue">Nowy indeks gracza.</param>
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

    /// <summary>
    /// Właściwość zwracająca nazwę gracza jako ciąg znaków.
    /// </summary>
    /// <value>Pseudonim gracza odczytany ze zmiennej sieciowej.</value>
    public string PlayerName => playerName.Value.ToString();

    /// <summary>
    /// Właściwość zwracająca indeks gracza.
    /// </summary>
    /// <value>Indeks gracza określający punkt spawnu i kolor.</value>
    public int PlayerIndex => playerIndex.Value;

    /// <summary>
    /// Wyszukuje obiekt <see cref="NetworkPlayer"/> na podstawie identyfikatora klienta sieciowego.
    /// </summary>
    /// <param name="clientId">Identyfikator klienta sieciowego.</param>
    /// <returns>
    /// Instancja <see cref="NetworkPlayer"/> przypisana do danego klienta
    /// lub <c>null</c> jeśli klient nie został znaleziony.
    /// </returns>
    private static NetworkPlayer FindNetworkPlayerByClientId(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return null;

        NetworkClient client;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out client)) return null;
        if (client.PlayerObject == null) return null;

        return client.PlayerObject.GetComponent<NetworkPlayer>();
    }

    /// <summary>
    /// Wyszukuje komponent <see cref="PlayerInteraction"/> gracza lokalnego na scenie.
    /// </summary>
    /// <returns>
    /// Komponent <see cref="PlayerInteraction"/> gracza lokalnego
    /// lub <c>null</c> jeśli nie znaleziono.
    /// </returns>
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
    /// Wyszukuje lokalnego gracza sieciowego na scenie.
    /// </summary>
    /// <returns>
    /// Instancja <see cref="NetworkPlayer"/> należąca do lokalnego gracza
    /// lub <c>null</c> jeśli nie znaleziono.
    /// </returns>
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
/// Komponent dodający efekt delikatnego kołysania (bob) do trzymanego przedmiotu.
/// </summary>
/// <remarks>
/// Animuje pozycję lokalną obiektu sinusoidalnie w osi Y,
/// symulując naturalne kołysanie przedmiotu w ręku gracza.
/// </remarks>
public class HeldItemBob : MonoBehaviour
{
    /// <summary>
    /// Amplituda kołysania (w jednostkach świata). Określa maksymalne odchylenie od pozycji bazowej.
    /// </summary>
    public float amplitude = 0.01f;

    /// <summary>
    /// Prędkość kołysania. Określa częstotliwość oscylacji sinusoidalnej.
    /// </summary>
    public float speed = 2.5f;

    /// <summary>
    /// Bazowa pozycja lokalna obiektu, wokół której odbywa się kołysanie.
    /// </summary>
    private Vector3 basePosition;

    /// <summary>
    /// Inicjalizuje pozycję bazową na podstawie aktualnej pozycji lokalnej obiektu.
    /// </summary>
    private void Start()
    {
        basePosition = transform.localPosition;
    }

    /// <summary>
    /// Aktualizuje pozycję obiektu co klatkę, dodając sinusoidalne przesunięcie w osi Y.
    /// </summary>
    private void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * amplitude;
        transform.localPosition = basePosition + new Vector3(0f, offset, 0f);
    }
}

/// <summary>
/// Komponent animacji gracza zdalnego wykorzystujący system PlayableGraph.
/// </summary>
/// <remarks>
/// Miksuje animacje bezczynności (idle) i chodzenia (walk) na podstawie
/// prędkości przemieszczania się obiektu gracza. Używa <see cref="PlayableGraph"/>
/// do płynnego przejścia między animacjami bez konieczności używania Animator Controller.
/// </remarks>
public class RemotePlayerAnimator : MonoBehaviour
{
    /// <summary>
    /// Klip animacji bezczynności (idle) gracza.
    /// </summary>
    public AnimationClip idleClip;

    /// <summary>
    /// Klip animacji chodzenia (walk) gracza.
    /// </summary>
    public AnimationClip walkClip;

    /// <summary>
    /// Graf odtwarzania animacji (PlayableGraph) zarządzający miksowaniem klipów.
    /// </summary>
    private PlayableGraph graph;

    /// <summary>
    /// Mikser animacji łączący animację idle i walk z odpowiednimi wagami.
    /// </summary>
    private AnimationMixerPlayable mixer;

    /// <summary>
    /// Odtwarzalny klip animacji bezczynności.
    /// </summary>
    private AnimationClipPlayable idlePlayable;

    /// <summary>
    /// Odtwarzalny klip animacji chodzenia.
    /// </summary>
    private AnimationClipPlayable walkPlayable;

    /// <summary>
    /// Długość klipu animacji bezczynności (w sekundach).
    /// </summary>
    private float idleLength;

    /// <summary>
    /// Długość klipu animacji chodzenia (w sekundach).
    /// </summary>
    private float walkLength;

    /// <summary>
    /// Ostatnia zapamiętana pozycja gracza, używana do obliczania prędkości.
    /// </summary>
    private Vector3 lastPosition;

    /// <summary>
    /// Wygładzona wartość prędkości używana do interpolacji wag miksera animacji.
    /// </summary>
    private float speedSmoothed;

    /// <summary>
    /// Inicjalizuje system animacji: tworzy PlayableGraph, konfiguruje mikser
    /// i podłącza klipy animacji.
    /// </summary>
    /// <remarks>
    /// Wymaga, aby <see cref="idleClip"/> i <see cref="walkClip"/> były ustawione przed wywołaniem.
    /// Automatycznie dodaje komponent <see cref="Animator"/> jeśli nie istnieje.
    /// Początkowo ustawia pełną wagę na animację bezczynności.
    /// </remarks>
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

    /// <summary>
    /// Aktualizuje wagi miksera animacji na podstawie prędkości przemieszczania się gracza.
    /// </summary>
    /// <remarks>
    /// Oblicza prędkość na podstawie zmiany pozycji między klatkami.
    /// Waga animacji chodzenia jest proporcjonalna do prędkości (znormalizowana do 2.5 jednostek/s).
    /// Zapewnia zapętlanie klipów animacji po osiągnięciu ich końca.
    /// </remarks>
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

    /// <summary>
    /// Niszczy graf odtwarzania animacji przy usunięciu komponentu z obiektu gry.
    /// </summary>
    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
