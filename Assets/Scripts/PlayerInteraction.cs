using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactionDistance = 3f;
    public LayerMask interactableLayer = ~0;

    [SerializeField] private string currentPrompt = string.Empty;
    [SerializeField] private string feedbackMessage = string.Empty;
    [SerializeField] private KitchenItem heldItem;

    private float feedbackUntilTime;
    private Interactable currentInteractable;
    private ShopUI cachedShopUI;
    private LobbyUI cachedLobbyUI;

    private GameObject viewModel;
    private Transform rightHand;
    private GameObject currentItemVisual;
    private KitchenItem lastVisualizedItem;
    private UnityEngine.Playables.PlayableGraph viewModelGraph;
    private UnityEngine.Animations.AnimationClipPlayable fpvPlayable;
    private float fpvClipLength;
    private bool isLocalViewModel = false;

    public string CurrentPrompt => currentPrompt;
    public string FeedbackMessage => Time.time <= feedbackUntilTime ? feedbackMessage : string.Empty;
    public bool HasItemInHand => heldItem != null;
    public KitchenItem HeldItem => heldItem;

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

    private void OnDestroy()
    {
        if (viewModelGraph.IsValid()) viewModelGraph.Destroy();
    }

    public bool TryReceiveItem(KitchenItem item)
    {
        if (item == null || heldItem != null)
        {
            return false;
        }

        heldItem = item;
        return true;
    }

    public KitchenItem RemoveHeldItem()
    {
        KitchenItem item = heldItem;
        heldItem = null;
        return item;
    }

    public void ClearHeldItem()
    {
        heldItem = null;
    }

    public void SetFeedback(string message, float duration = 2.5f)
    {
        feedbackMessage = message;
        feedbackUntilTime = Time.time + duration;
    }

    public string GetHeldItemSummary()
    {
        return heldItem == null ? "Puste rece" : heldItem.BuildSummary();
    }

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
