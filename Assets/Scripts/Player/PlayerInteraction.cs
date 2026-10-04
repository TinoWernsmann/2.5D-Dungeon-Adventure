using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private ItemManager itemManager;
    [SerializeField] private PlayerCombat playerCombat;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 3f;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (itemManager == null)
        {
            itemManager = FindAnyObjectByType<ItemManager>();
        }

        if (playerCombat == null)
        {
            playerCombat = GetComponent<PlayerCombat>();
        }

        itemManager?.ConfigurePlayer(transform);
    }

    private void OnEnable()
    {
        GameInput.Instance.OnHeal += HandleHealInput;
        GameInput.Instance.OnInteract += HandleInteractionInput;
        GameInput.Instance.OnAttack += HandleCombatInput;
        GameInput.Instance.OnItemSelect += HandleInventoryInput;
        GameInput.Instance.OnDropPressed += HandleDropInput;
        GameInput.Instance.OnDialogue += HandleDialogue;
    }

    private void OnDisable()
    {
        GameInput.Instance.OnDialogue -= HandleDialogue;
        GameInput.Instance.OnHeal -= HandleHealInput;
        GameInput.Instance.OnInteract -= HandleInteractionInput;
        GameInput.Instance.OnAttack -= HandleCombatInput;
        GameInput.Instance.OnItemSelect -= HandleInventoryInput;
        GameInput.Instance.OnDropPressed -= HandleDropInput;
    }

    private void HandleDialogue()
    {
        TryInteract();
    }

    private void HandleHealInput()
    {
        TryHeal();
    }

    private void TryHeal()
    {
        itemManager?.TryUseHealing();
    }

    private void HandleInteractionInput()
    {
        TryInteract();
    }

    private void HandleCombatInput()
    {
        playerCombat?.TryAttack();
    }

    private void HandleInventoryInput(int slot)
    {
        itemManager?.SelectSlot(slot);
    }

    private void HandleDropInput()
    {
        itemManager?.DropSelectedItem();
    }

    private void TryInteract()
    {
        if (playerCamera == null)
        {
            return;
        }

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