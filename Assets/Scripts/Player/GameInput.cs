using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    private InputSystem_Actions _input;
    public event Action OnDropPressed;
    public event Action<int> OnItemSelect;
    public event Action OnInteract;
    public event Action OnAttack;
    public event Action OnHeal;
    public event Action OnDialogue;
    public event Action OnPause;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _input = new InputSystem_Actions();
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
    }

    private void OnDisable()
    {
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
    }

    public void EnableDialogue()
    {
        _input.Player.Disable();
        _input.Player.Dialogue.Enable();
    }

    public void DisableDialogue()
    {
        _input.Player.Enable();
        _input.Player.Dialogue.Disable();
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
}
