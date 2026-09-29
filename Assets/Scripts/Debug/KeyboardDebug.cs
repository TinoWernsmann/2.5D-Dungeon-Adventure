using UnityEngine;

public class KeyboardDebug : MonoBehaviour
{
    [SerializeField] private ItemBase _itemToTest;

    private InputSystem_Actions _input;

    private void Start()
    {
        _input = new InputSystem_Actions();

        _input.Debug.Press.performed += Press_performed;

        EnableInput();
    }

    private void Press_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        _itemToTest.ItemDebugCollect();
    }

    private void EnableInput()
    {
        _input.Debug.Enable();
    }
}
