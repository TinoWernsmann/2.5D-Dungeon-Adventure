using UnityEngine;

public abstract class InteractableController : MonoBehaviour, IInteract
{
    [SerializeField] protected Interactable _interactData;


    public abstract void Interact();

    public virtual void ShowInteractPrompt()
    {
        //UIManager.Instance.UpdatePrompt(_interactData.InteractableName);
    }

    public virtual void HideInteractPrompt()
    {
        //UIManager.Instance.UpdatePrompt(string.Empty);
    }
}