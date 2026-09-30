using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    [SerializeField] private DialogueInteractable _testDialogue;

    private InputSystem_Actions _inputActions;

    public event Action OnInteract;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("There are multiple Game Inputs!");
            Destroy(gameObject);
            return;
        }

        _inputActions = new InputSystem_Actions();
        Instance = this;
    }

    private void Start()
    {
        _inputActions.Debug.Interact.Enable();
        _inputActions.Debug.Interact.performed += InteractPerformed;
    }

    private void InteractPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        _testDialogue.Interact();
    }

    private void OnDestroy()
    {
        _inputActions.Debug.Interact.performed -= InteractPerformed;
    }

    private void OnDisable()
    {
        OnInteract = null;
    }
}