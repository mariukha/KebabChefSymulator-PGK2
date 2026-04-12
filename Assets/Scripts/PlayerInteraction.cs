using UnityEngine;

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

    public string CurrentPrompt => currentPrompt;
    public string FeedbackMessage => Time.time <= feedbackUntilTime ? feedbackMessage : string.Empty;
    public bool HasItemInHand => heldItem != null;
    public KitchenItem HeldItem => heldItem;

    private void Update()
    {
        EnsureCamera();
        HandleRaycast();
        HandleInput();
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

        if (playerCamera == null)
        {
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        int mask = interactableLayer.value == 0 ? Physics.DefaultRaycastLayers : interactableLayer.value;

        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red);

        if (!Physics.Raycast(ray, out hit, interactionDistance, mask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
        if (interactable == null)
        {
            interactable = hit.collider.GetComponent<Interactable>();
        }

        if (interactable == null)
        {
            return;
        }

        currentInteractable = interactable;
        currentPrompt = interactable.GetPrompt(this);
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Q) && heldItem != null)
        {
            SetFeedback("Wyrzucono: " + heldItem.BuildSummary());
            heldItem = null;
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            currentInteractable.Interact(this);
        }
    }
}
