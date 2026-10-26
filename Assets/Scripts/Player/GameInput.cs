using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    // Mindestabstand zwischen zwei Mausrad-Schritten (Touchpads senden sehr viele kleine Werte).
    private const float ItemScrollCooldown = 0.08f;

    private InputSystem_Actions _input;
    // Aktionen, die nicht im Input-Asset stehen, werden hier im Code angelegt.
    private InputAction _itemScrollAction;
    private InputAction _slot5Action;
    private InputAction _switchAmmoAction;
    private float _nextItemScrollTime;

    public event Action OnDropPressed;

    /// <summary>Zifferntaste 1–5 als Index 0–4 (Taste 1 = Fäuste, 2–5 = Inventar-Slots).</summary>
    public event Action<int> OnItemSelect;

    /// <summary>R: Munitionsart der Fernkampfwaffe wechseln.</summary>
    public event Action OnSwitchAmmo;

    /// <summary>Mausrad: +1 = nächster Slot (runter scrollen), -1 = vorheriger Slot (hoch scrollen).</summary>
    public event Action<int> OnItemScroll;
    public event Action OnInteract;
    public event Action OnAttack;
    public event Action OnHeal;
    public event Action OnDialogue;
    public event Action OnPause;

    private void Awake()
    {
        // Es gibt schon ein GameInput aus einer vorherigen Szene -> dieses Duplikat entfernen.
        // Nur die Komponente, damit andere Komponenten auf demselben Objekt erhalten bleiben.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _input = new InputSystem_Actions();

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        ConnectInput();
    }

    private void ConnectInput()
    {
        _input.Player.Enable();
        _input.Player.Dialogue.Disable();

        _input.Player.Drop.performed += DropPerformed;
        _input.Player.Slot1.performed += Slot1Performed;
        _input.Player.Slot2.performed += Slot2Performed;
        _input.Player.Slot3.performed += Slot3Performed;
        _input.Player.Slot4.performed += Slot4Performed;
        _input.Player.Interact.performed += InteractPerformed;
        _input.Player.Attack.performed += AttackPerformed;
        _input.Player.Healing.performed += HealPerformed;
        _input.Player.Dialogue.performed += DialoguePerformed;
        _input.Player.Pause.performed += PausePerformed;

        _itemScrollAction = new InputAction("ItemScroll", InputActionType.Value, "<Mouse>/scroll/y");
        _itemScrollAction.performed += ItemScrollPerformed;

        _slot5Action = new InputAction("Slot5", InputActionType.Button, "<Keyboard>/5");
        _slot5Action.performed += Slot5Performed;

        _switchAmmoAction = new InputAction("SwitchAmmo", InputActionType.Button, "<Keyboard>/r");
        _switchAmmoAction.performed += SwitchAmmoPerformed;

        SetCodeActionsEnabled(true);
    }

    private void SetCodeActionsEnabled(bool isEnabled)
    {
        foreach (InputAction action in new[] { _itemScrollAction, _slot5Action, _switchAmmoAction })
        {
            if (isEnabled) action.Enable();
            else action.Disable();
        }
    }

    private void OnDisable()
    {
        // Duplikate werden in Awake entfernt, bevor sie ein Input-Objekt angelegt haben.
        if (_input == null)
        {
            return;
        }

        _input.Player.Disable();

        _input.Player.Drop.performed -= DropPerformed;
        _input.Player.Slot1.performed -= Slot1Performed;
        _input.Player.Slot2.performed -= Slot2Performed;
        _input.Player.Slot3.performed -= Slot3Performed;
        _input.Player.Slot4.performed -= Slot4Performed;
        _input.Player.Interact.performed -= InteractPerformed;
        _input.Player.Attack.performed -= AttackPerformed;
        _input.Player.Healing.performed -= HealPerformed;
        _input.Player.Dialogue.performed -= DialoguePerformed;
        _input.Player.Pause.performed -= PausePerformed;

        _itemScrollAction.performed -= ItemScrollPerformed;
        _slot5Action.performed -= Slot5Performed;
        _switchAmmoAction.performed -= SwitchAmmoPerformed;
        SetCodeActionsEnabled(false);
    }

    public void EnableDialogue()
    {
        _input.Player.Disable();
        _input.Player.Dialogue.Enable();
        SetCodeActionsEnabled(false);
    }

    public void DisableDialogue()
    {
        _input.Player.Enable();
        _input.Player.Dialogue.Disable();
        SetCodeActionsEnabled(true);
    }

    private void SwitchAmmoPerformed(InputAction.CallbackContext context)
    {
        OnSwitchAmmo?.Invoke();
    }

    private void ItemScrollPerformed(InputAction.CallbackContext context)
    {
        float scroll = context.ReadValue<float>();

        if (Mathf.Abs(scroll) < 0.01f || Time.unscaledTime < _nextItemScrollTime)
        {
            return;
        }

        _nextItemScrollTime = Time.unscaledTime + ItemScrollCooldown;

        // Hoch scrollen = vorheriger Slot, runter scrollen = nächster Slot.
        OnItemScroll?.Invoke(scroll > 0f ? -1 : 1);
    }

    private void PausePerformed(InputAction.CallbackContext context)
    {
        OnPause?.Invoke();
    }

    private void DialoguePerformed(InputAction.CallbackContext context)
    {
        OnDialogue?.Invoke();
    }

    private void HealPerformed(InputAction.CallbackContext context)
    {
        OnHeal?.Invoke();
    }

    private void AttackPerformed(InputAction.CallbackContext context)
    {
        OnAttack?.Invoke();
    }

    private void InteractPerformed(InputAction.CallbackContext context)
    {
        OnInteract?.Invoke();
    }

    private void DropPerformed(InputAction.CallbackContext context)
    {
        OnDropPressed?.Invoke();
    }

    private void Slot1Performed(InputAction.CallbackContext context)
    {
        OnItemSelect?.Invoke(0);
    }

    private void Slot2Performed(InputAction.CallbackContext context)
    {
        OnItemSelect?.Invoke(1);
    }

    private void Slot3Performed(InputAction.CallbackContext context)
    {
        OnItemSelect?.Invoke(2);
    }

    private void Slot4Performed(InputAction.CallbackContext context)
    {
        OnItemSelect?.Invoke(3);
    }

    private void Slot5Performed(InputAction.CallbackContext context)
    {
        OnItemSelect?.Invoke(4);
    }
}
