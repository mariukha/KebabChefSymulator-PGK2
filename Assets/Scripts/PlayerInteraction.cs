/// \file PlayerInteraction.cs
/// \brief Plik zawierający klasę PlayerInteraction odpowiedzialną za interakcję gracza z obiektami w świecie gry.
/// \details Obsługuje rzucanie promieni (raycast) z kamery gracza, wykrywanie obiektów interaktywnych,
/// zarządzanie trzymanym przedmiotem kuchennym oraz wyświetlanie modelu pierwszoosobowego (view model).

using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

/// <summary>
/// Klasa zarządzająca interakcją gracza z otoczeniem w grze.
/// Odpowiada za wykrywanie obiektów interaktywnych za pomocą raycastu z kamery,
/// obsługę trzymanego przedmiotu kuchennego, wyświetlanie podpowiedzi i komunikatów zwrotnych,
/// a także tworzenie i aktualizację modelu pierwszoosobowego (ręce gracza) wraz z wizualizacją trzymanego przedmiotu.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    /// <summary>
    /// Kamera gracza używana do rzucania promieni w celu wykrywania interaktywnych obiektów.
    /// </summary>
    public Camera playerCamera;

    /// <summary>
    /// Maksymalna odległość interakcji gracza z obiektami (w jednostkach Unity).
    /// </summary>
    public float interactionDistance = 3f;

    /// <summary>
    /// Maska warstw określająca, które obiekty mogą być celem interakcji.
    /// Domyślnie ustawiona na wszystkie warstwy.
    /// </summary>
    public LayerMask interactableLayer = ~0;

    /// <summary>
    /// Aktualny komunikat podpowiedzi wyświetlany graczowi, gdy patrzy na obiekt interaktywny.
    /// </summary>
    [SerializeField] private string currentPrompt = string.Empty;

    /// <summary>
    /// Komunikat zwrotny informujący gracza o wyniku ostatniej akcji (np. wyrzucenie przedmiotu).
    /// </summary>
    [SerializeField] private string feedbackMessage = string.Empty;

    /// <summary>
    /// Aktualnie trzymany przez gracza przedmiot kuchenny.
    /// Wartość null oznacza puste ręce.
    /// </summary>
    [SerializeField] private KitchenItem heldItem;

    /// <summary>
    /// Czas (w sekundach gry), do którego komunikat zwrotny jest aktywny.
    /// Po przekroczeniu tego czasu komunikat nie jest już wyświetlany.
    /// </summary>
    private float feedbackUntilTime;

    /// <summary>
    /// Referencja do aktualnie namierzonego obiektu interaktywnego.
    /// </summary>
    private Interactable currentInteractable;

    /// <summary>
    /// Zbuforowana referencja do interfejsu sklepu, aby uniknąć wielokrotnego wyszukiwania.
    /// </summary>
    private ShopUI cachedShopUI;

    /// <summary>
    /// Zbuforowana referencja do interfejsu lobby, aby uniknąć wielokrotnego wyszukiwania.
    /// </summary>
    private LobbyUI cachedLobbyUI;

    /// <summary>
    /// Obiekt modelu pierwszoosobowego (ręce gracza) widoczny w widoku FPV.
    /// </summary>
    private GameObject viewModel;

    /// <summary>
    /// Transform kości prawej ręki w modelu pierwszoosobowym, służący do pozycjonowania trzymanych przedmiotów.
    /// </summary>
    private Transform rightHand;

    /// <summary>
    /// Aktualny obiekt wizualizacji trzymanego przedmiotu w ręce gracza.
    /// </summary>
    private GameObject currentItemVisual;

    /// <summary>
    /// Ostatni zwizualizowany przedmiot kuchenny, używany do wykrywania zmian i unikania niepotrzebnych aktualizacji.
    /// </summary>
    private KitchenItem lastVisualizedItem;

    /// <summary>
    /// Graf Playable używany do odtwarzania animacji modelu pierwszoosobowego.
    /// </summary>
    private UnityEngine.Playables.PlayableGraph viewModelGraph;

    /// <summary>
    /// Obiekt Playable klipu animacji pierwszoosobowej.
    /// </summary>
    private UnityEngine.Animations.AnimationClipPlayable fpvPlayable;

    /// <summary>
    /// Długość klipu animacji pierwszoosobowej w sekundach.
    /// </summary>
    private float fpvClipLength;

    /// <summary>
    /// Flaga określająca, czy ten obiekt jest lokalnym modelem widoku (tj. należy do lokalnego gracza).
    /// W trybie wieloosobowym tylko właściciel ma aktywny view model.
    /// </summary>
    private bool isLocalViewModel = false;

    /// <summary>
    /// Aktualny komunikat podpowiedzi wyświetlany graczowi.
    /// </summary>
    /// <value>Tekst podpowiedzi lub pusty ciąg znaków, gdy brak obiektu interaktywnego w zasięgu.</value>
    public string CurrentPrompt => currentPrompt;

    /// <summary>
    /// Komunikat zwrotny wyświetlany graczowi przez określony czas po wykonaniu akcji.
    /// </summary>
    /// <value>Tekst komunikatu zwrotnego lub pusty ciąg, jeśli czas wyświetlania minął.</value>
    public string FeedbackMessage => Time.time <= feedbackUntilTime ? feedbackMessage : string.Empty;

    /// <summary>
    /// Określa, czy gracz trzyma aktualnie jakiś przedmiot kuchenny.
    /// </summary>
    /// <value><c>true</c> jeśli gracz trzyma przedmiot; w przeciwnym razie <c>false</c>.</value>
    public bool HasItemInHand => heldItem != null;

    /// <summary>
    /// Referencja do aktualnie trzymanego przedmiotu kuchennego.
    /// </summary>
    /// <value>Obiekt <see cref="KitchenItem"/> lub <c>null</c>, gdy ręce są puste.</value>
    public KitchenItem HeldItem => heldItem;

    /// <summary>
    /// Inicjalizuje komponent interakcji gracza.
    /// W trybie wieloosobowym sprawdza, czy ten gracz jest właścicielem obiektu sieciowego.
    /// Tworzy model pierwszoosobowy tylko dla lokalnego gracza.
    /// </summary>
    private void Start()
    {
        heldItem = null;

        bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isMultiplayer)
        {
            NetworkPlayer netPlayer = GetComponentInParent<NetworkPlayer>();
            if (netPlayer != null && !netPlayer.IsOwner) return;
        }

        isLocalViewModel = true;
        EnsureCamera();
        CreateViewModel();
    }

    /// <summary>
    /// Aktualizacja wywoływana co klatkę.
    /// Sprawdza zmiany wizualizacji trzymanego przedmiotu, zapętla animację FPV,
    /// upewnia się o dostępności kamery oraz obsługuje raycast i wejście gracza.
    /// </summary>
    private void Update()
    {
        if (isLocalViewModel)
        {
            bool needsVisualUpdate = false;
            if (heldItem == null && lastVisualizedItem != null) needsVisualUpdate = true;
            else if (heldItem != null && lastVisualizedItem == null) needsVisualUpdate = true;
            else if (heldItem != null && lastVisualizedItem != null)
            {
                if (heldItem.ingredientKind != lastVisualizedItem.ingredientKind ||
                    heldItem.state != lastVisualizedItem.state ||
                    heldItem.isDish != lastVisualizedItem.isDish)
                {
                    needsVisualUpdate = true;
                }
                else if (heldItem != lastVisualizedItem)
                {

                    lastVisualizedItem = heldItem;
                }
            }

            if (needsVisualUpdate)
            {
                UpdateItemVisual();
            }

            if (viewModelGraph.IsValid() && fpvPlayable.IsValid())
            {
                if (fpvPlayable.GetTime() >= fpvClipLength)
                {
                    fpvPlayable.SetTime(0);
                }
            }
        }

        EnsureCamera();
        HandleRaycast();
        HandleInput();
    }

    /// <summary>
    /// Tworzy model pierwszoosobowy (ręce gracza) jako dziecko kamery.
    /// Ładuje prefab z zasobów, konfiguruje pozycję, ukrywa głowę i szyję,
    /// wyłącza kolizje i cienie, a następnie konfiguruje animację Playable.
    /// </summary>
    private void CreateViewModel()
    {
        if (playerCamera == null) return;

        GameObject prefab = Resources.Load<GameObject>("Models/fpv");
        if (prefab == null) return;

        viewModel = Instantiate(prefab, playerCamera.transform);
        viewModel.name = "FirstPersonViewModel";

        viewModel.transform.localPosition = new Vector3(-0.1f, -1.17f, -0.1f);
        viewModel.transform.localRotation = Quaternion.identity;

        Transform[] allTransforms = viewModel.GetComponentsInChildren<Transform>();
        Transform head = null;
        Transform neck = null;
        foreach (Transform t in allTransforms)
        {
            string n = t.name.ToLower();
            if (n.Contains("head")) head = t;
            if (n.Contains("neck")) neck = t;
            if (n.Contains("right") && n.Contains("hand")) rightHand = t;
        }

        if (neck != null) neck.localScale = Vector3.zero;
        if (head != null) head.localScale = Vector3.zero;

        if (rightHand == null) rightHand = viewModel.transform;

        foreach (Collider c in viewModel.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (Renderer r in viewModel.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Animator animator = viewModel.GetComponent<Animator>();
        if (animator == null) animator = viewModel.AddComponent<Animator>();

        AnimationClip[] clips = Resources.LoadAll<AnimationClip>("Models/fpv");
        if (clips != null && clips.Length > 0)
        {
            AnimationClip fpvClip = clips[0];
            if (fpvClip != null)
            {
                fpvClip.wrapMode = WrapMode.Loop;
                viewModelGraph = UnityEngine.Playables.PlayableGraph.Create("ViewModelGraph");
                var output = UnityEngine.Animations.AnimationPlayableOutput.Create(viewModelGraph, "Animation", animator);

                fpvPlayable = UnityEngine.Animations.AnimationClipPlayable.Create(viewModelGraph, fpvClip);
                fpvClipLength = fpvClip.length;

                output.SetSourcePlayable(fpvPlayable);
                viewModelGraph.Play();
            }
        }
    }

    /// <summary>
    /// Aktualizuje wizualizację trzymanego przedmiotu w prawej ręce modelu FPV.
    /// Niszczy poprzednią wizualizację i tworzy nową na podstawie aktualnego przedmiotu kuchennego.
    /// Pozycjonuje przedmiot w centrum dłoni, uwzględniając kości palców (środkowy i kciuk).
    /// </summary>
    private void UpdateItemVisual()
    {
        lastVisualizedItem = heldItem;

        if (currentItemVisual != null)
        {
            Destroy(currentItemVisual);
            currentItemVisual = null;
        }

        if (heldItem == null || rightHand == null) return;

        Transform middleBone = rightHand;
        Transform thumbBone = rightHand;

        for (int i = 0; i < rightHand.childCount; i++)
        {
            string n = rightHand.GetChild(i).name.ToLower();
            if (n.Contains("middle")) middleBone = rightHand.GetChild(i);
            if (n.Contains("thumb")) thumbBone = rightHand.GetChild(i);
        }

        if (middleBone == rightHand && rightHand.childCount > 0)
            middleBone = rightHand.GetChild(0);
        if (thumbBone == rightHand && rightHand.childCount > 1)
            thumbBone = rightHand.GetChild(1);

        Vector3 worldPalmCenter = Vector3.Lerp(middleBone.position, rightHand.position, 0.85f);

        Vector3 towardsThumb = thumbBone.position - middleBone.position;
        worldPalmCenter += towardsThumb * 0.6f;

        Vector3 palmDown = Vector3.Cross(thumbBone.position - rightHand.position, middleBone.position - rightHand.position).normalized;
        worldPalmCenter += palmDown * 0.08f;

        Vector3 localPalmCenter = middleBone.InverseTransformPoint(worldPalmCenter);

        currentItemVisual = KitchenItemVisualFactory.CreateItemVisual(
            heldItem.ingredientKind,
            heldItem.state,
            heldItem.isDish,
            middleBone,
            localPalmCenter,
            Vector3.zero,
            0.18f
        );

        if (currentItemVisual != null)
        {
            foreach (Renderer r in currentItemVisual.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    /// <summary>
    /// Wywoływana przy niszczeniu obiektu. Zwalnia zasoby grafu Playable animacji modelu FPV.
    /// </summary>
    private void OnDestroy()
    {
        if (viewModelGraph.IsValid()) viewModelGraph.Destroy();
    }

    /// <summary>
    /// Próbuje przekazać graczowi przedmiot kuchenny.
    /// Operacja kończy się niepowodzeniem, gdy gracz już trzyma przedmiot lub przekazywany przedmiot jest null.
    /// </summary>
    /// <param name="item">Przedmiot kuchenny do przekazania graczowi.</param>
    /// <returns><c>true</c> jeśli przedmiot został pomyślnie przyjęty; <c>false</c> w przeciwnym razie.</returns>
    public bool TryReceiveItem(KitchenItem item)
    {
        if (item == null || heldItem != null)
        {
            return false;
        }

        heldItem = item;
        return true;
    }

    /// <summary>
    /// Zabiera trzymany przedmiot z rąk gracza i zwraca go.
    /// Po wywołaniu gracz ma puste ręce.
    /// </summary>
    /// <returns>Przedmiot kuchenny, który był trzymany, lub <c>null</c> jeśli ręce były puste.</returns>
    public KitchenItem RemoveHeldItem()
    {
        KitchenItem item = heldItem;
        heldItem = null;
        return item;
    }

    /// <summary>
    /// Czyści referencję do trzymanego przedmiotu bez jego zwracania.
    /// Używane, gdy przedmiot został zużyty lub zniszczony.
    /// </summary>
    public void ClearHeldItem()
    {
        heldItem = null;
    }

    /// <summary>
    /// Ustawia komunikat zwrotny wyświetlany graczowi przez określony czas.
    /// </summary>
    /// <param name="message">Treść komunikatu do wyświetlenia.</param>
    /// <param name="duration">Czas wyświetlania komunikatu w sekundach (domyślnie 2.5s).</param>
    public void SetFeedback(string message, float duration = 2.5f)
    {
        feedbackMessage = message;
        feedbackUntilTime = Time.time + duration;
    }

    /// <summary>
    /// Zwraca tekstowy opis aktualnie trzymanego przedmiotu.
    /// </summary>
    /// <returns>Opis trzymanego przedmiotu lub "Puste rece", gdy gracz nic nie trzyma.</returns>
    public string GetHeldItemSummary()
    {
        return heldItem == null ? "Puste rece" : heldItem.BuildSummary();
    }

    /// <summary>
    /// Upewnia się, że referencja do kamery gracza jest prawidłowa.
    /// Szuka kamery w obiektach podrzędnych, a następnie jako fallback używa kamery głównej.
    /// </summary>
    private void EnsureCamera()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    /// <summary>
    /// Wykonuje raycast z kamery gracza w celu wykrycia obiektów interaktywnych.
    /// Aktualizuje podpowiedź, aktualny obiekt interaktywny oraz podświetlenie celu.
    /// </summary>
    private void HandleRaycast()
    {
        currentPrompt = string.Empty;
        currentInteractable = null;

        if (InteractionHighlight.Instance != null)
        {
            InteractionHighlight.Instance.SetTarget(null);
        }

        if (playerCamera == null)
        {
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        int mask = interactableLayer.value == 0 ? Physics.DefaultRaycastLayers : interactableLayer.value;

        if (!Physics.Raycast(ray, out hit, interactionDistance, mask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
        if (interactable == null)
        {
            hit.collider.TryGetComponent<Interactable>(out interactable);
        }

        if (interactable == null)
        {
            return;
        }

        currentInteractable = interactable;
        currentPrompt = interactable.GetPrompt(this);

        if (InteractionHighlight.Instance != null)
        {
            InteractionHighlight.Instance.SetTarget(interactable.gameObject);
        }
    }

    /// <summary>
    /// Obsługuje wejście gracza dotyczące interakcji.
    /// Blokuje interakcje, gdy otwarte jest menu, sklep, lobby, ustawienia lub pauza.
    /// Klawisz Q wyrzuca trzymany przedmiot, klawisz E aktywuje interakcję z namierzonym obiektem.
    /// W trybie wieloosobowym interakcja ze stacjami kuchennymi jest przekazywana przez sieć (ServerRpc).
    /// </summary>
    private void HandleInput()
    {
        if (cachedShopUI == null) cachedShopUI = FindFirstObjectByType<ShopUI>();
        if (cachedLobbyUI == null) cachedLobbyUI = FindFirstObjectByType<LobbyUI>();

        bool shopOpen = cachedShopUI != null && cachedShopUI.IsShopOpen;
        bool lobbyOpen = cachedLobbyUI != null && cachedLobbyUI.IsLobbyOpen;
        MainMenuUI mainMenu = FindFirstObjectByType<MainMenuUI>();
        bool menuOpen = mainMenu != null && mainMenu.IsMenuOpen;
        bool pauseOpen = PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused;
        bool settingsOpen = SettingsMenuUI.Instance != null && SettingsMenuUI.Instance.IsOpen;

        if (shopOpen || lobbyOpen || menuOpen || pauseOpen || settingsOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q) && heldItem != null)
        {
            SetFeedback("Wyrzucono: " + heldItem.BuildSummary());
            Vector3 dropPosition = playerCamera != null
                ? playerCamera.transform.position + playerCamera.transform.forward * 1.05f + Vector3.down * 0.55f
                : transform.position + transform.forward * 0.75f;
            heldItem = null;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayDropSound();
            if (VFXManager.Instance != null) VFXManager.Instance.PlayDropEffect(dropPosition);
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            NetworkKitchenStation networkStation = isMultiplayer
                ? currentInteractable.GetComponent<NetworkKitchenStation>()
                : null;

            if (networkStation != null && isMultiplayer)
            {
                NetworkPlayer localPlayer = GetComponentInParent<NetworkPlayer>();
                if (localPlayer == null)
                {
                    localPlayer = FindLocalNetworkPlayer();
                }

                if (localPlayer != null)
                {
                    NetworkItemState heldState = NetworkItemState.FromKitchenItem(heldItem);
                    localPlayer.InteractWithStationServerRpc(networkStation.StationIndex, heldState);
                }
            }
            else
            {
                currentInteractable.Interact(this);
            }
        }
    }

    /// <summary>
    /// Wyszukuje lokalnego gracza sieciowego (NetworkPlayer) wśród wszystkich aktywnych graczy na scenie.
    /// </summary>
    /// <returns>Obiekt <see cref="NetworkPlayer"/> będący własnością lokalnego klienta lub <c>null</c>, jeśli nie znaleziono.</returns>
    private static NetworkPlayer FindLocalNetworkPlayer()
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
