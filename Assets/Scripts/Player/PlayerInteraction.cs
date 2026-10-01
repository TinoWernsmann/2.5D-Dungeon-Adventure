using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private ItemManager itemManager;
    [SerializeField] private PlayerCombat playerCombat;

    [Header("Input")]
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputActionReference attackAction;
    [SerializeField] private InputActionReference healAction;

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
        interactAction?.action.Enable();
        attackAction?.action.Enable();
    }

    private void OnDisable()
    {
        interactAction?.action.Disable();
        attackAction?.action.Disable();
    }

    private void Update()
    {
        HandleInteractionInput();
        HandleCombatInput();
        HandleInventoryInput();
        HandleHealInput();
    }

    private void HandleHealInput()
    {
        if (healAction != null &&
            healAction.action.WasPressedThisFrame())
        {
            TryHeal();
        }
    }

    private void TryHeal()
    {
        itemManager?.TryUseHealing();
    }

    private void HandleInteractionInput()
    {
        if (interactAction != null &&
            interactAction.action.WasPressedThisFrame())
        {
            TryInteract();
        }
    }

    private void HandleCombatInput()
    {
        bool attackPressed = attackAction != null
            ? attackAction.action.WasPressedThisFrame()
            : Mouse.current != null &&
              Mouse.current.leftButton.wasPressedThisFrame;

        if (attackPressed)
        {
            playerCombat?.TryAttack();
        }
    }

    private void HandleInventoryInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            itemManager?.SelectSlot(0);
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            itemManager?.SelectSlot(1);
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            itemManager?.SelectSlot(2);
        }

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            itemManager?.SelectSlot(3);
        }

        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            itemManager?.DropSelectedItem();
        }
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