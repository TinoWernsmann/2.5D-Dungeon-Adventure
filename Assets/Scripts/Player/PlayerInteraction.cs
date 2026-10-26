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
        GameInput.Instance.OnSwitchAmmo += HandleSwitchAmmo;
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
        GameInput.Instance.OnSwitchAmmo -= HandleSwitchAmmo;
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
        // Munition in der Hand: Linksklick rüstet sie für die Armbrust aus.
        if (itemManager != null && itemManager.SelectedItem is AmmoSO ammo)
        {
            if (Time.timeScale > 0f) playerCombat?.EquipAmmo(ammo);
            return;
        }

        // Heiltrank in der Hand: Linksklick trinkt ihn (wie H), statt anzugreifen.
        if (itemManager != null && itemManager.IsHealingSelected)
        {
            if (Time.timeScale > 0f) TryHeal();
            return;
        }

        playerCombat?.TryAttack();
    }

    // Taste 1 = Fäuste, Tasten 2–5 = Inventar-Slots 1–4.
    private void HandleInventoryInput(int key)
    {
        if (itemManager == null)
        {
            return;
        }

        if (key == 0)
        {
            itemManager.SelectFists();
            return;
        }

        itemManager.SelectSlot(key - 1);
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

    private void HandleSwitchAmmo()
    {
        if (Time.timeScale > 0f) playerCombat?.SwitchAmmo();
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