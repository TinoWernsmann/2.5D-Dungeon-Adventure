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

    public void Start()
    {
        GameInput.Instance.OnHeal += HandleHealInput;
        GameInput.Instance.OnInteract += HandleInteractionInput;
        GameInput.Instance.OnAttack += HandleCombatInput;
        GameInput.Instance.OnItemSelect += HandleInventoryInput;
        GameInput.Instance.OnItemScroll += HandleItemScroll;
        GameInput.Instance.OnDropPressed += HandleDropInput;
    }

    private void OnDisable()
    {
        if (GameInput.Instance == null) return;

        GameInput.Instance.OnHeal -= HandleHealInput;
        GameInput.Instance.OnInteract -= HandleInteractionInput;
        GameInput.Instance.OnAttack -= HandleCombatInput;
        GameInput.Instance.OnItemSelect -= HandleInventoryInput;
        GameInput.Instance.OnItemScroll -= HandleItemScroll;
        GameInput.Instance.OnDropPressed -= HandleDropInput;
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
        // Heiltrank in der Hand: Linksklick trinkt ihn (wie H), statt anzugreifen.
        if (itemManager != null && itemManager.IsHealingSelected)
        {
            if (Time.timeScale > 0f) TryHeal();
            return;
        }

        playerCombat?.TryAttack();
    }

    private void HandleInventoryInput(int slot)
    {
        itemManager?.SelectSlot(slot);
    }

    private void HandleItemScroll(int step)
    {
        // Im Pause-Menü soll das Mausrad nicht die Waffe wechseln.
        if (Time.timeScale <= 0f)
        {
            return;
        }

        itemManager?.SelectRelativeSlot(step);
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