using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Input")]
    [SerializeField] private InputActionReference interactAction;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 3f;

    private void OnEnable()
    {
        interactAction.action.Enable();
    }

    private void OnDisable()
    {
        interactAction.action.Disable();
    }

    private void Update()
    {
        if (interactAction.action.WasPressedThisFrame())
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Ray interactionRay = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
                interactionRay,
                out RaycastHit hit,
                interactionDistance))
        {
            return;
        }

        IInteractable interactable =
            hit.collider.GetComponentInParent<IInteractable>();

        interactable?.Interact();
    }
}