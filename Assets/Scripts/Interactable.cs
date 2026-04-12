using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [SerializeField] private string promptMessage = "Interact";
    [SerializeField] private UnityEvent onInteract;

    public string PromptMessage
    {
        get { return promptMessage; }
        set { promptMessage = value; }
    }

    public virtual string GetPrompt(PlayerInteraction player)
    {
        return promptMessage;
    }

    public virtual void Interact(PlayerInteraction player)
    {
        onInteract?.Invoke();
    }

    public void BaseInteract()
    {
        Interact(null);
    }
}
