using UnityEngine;

public class KeyboardDebug : MonoBehaviour
{
    [SerializeField] private ItemBase _itemToTest;
    [SerializeField] private ItemManager _itemManager;

    private InputSystem_Actions _input;

    private void Start()
    {
        _input = new InputSystem_Actions();

        _input.Debug.Press.performed += Press_performed;
        _input.Debug.Att.performed += Att_performed;

        EnableInput();
    }

    private void Att_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        _itemManager.UseWeapon();
    }

    private void Press_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        _itemToTest.ItemDebugCollect();
    }

    private void EnableInput()
    {
        _input.Debug.Enable();
    }

    private void OnDisable()
    {
        _input.Debug.Press.performed -= Press_performed;
        _input.Debug.Att.performed -= Att_performed;
    }
}
