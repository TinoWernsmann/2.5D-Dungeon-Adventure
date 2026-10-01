using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference dialogueAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jumping & Gravity")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedVelocity = -2f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float maxLookAngle = 85f;

    private CharacterController characterController;

    private float verticalVelocity;
    private float cameraPitch;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        SetPlayerMovement(true);
    }

    private void OnDisable()
    {
        SetPlayerMovement(false);
    }

    private void Start()
    {
        LockCursor();
    }

    private void Update()
    {
        HandleMovement();
        HandleLook();
    }

    public void SetPlayerMovement(bool toggle)
    {
        if (toggle)
        {
            moveAction.action.Enable();
            lookAction.action.Enable();
            jumpAction.action.Enable();
        }
        else
        {
            moveAction.action.Disable();
            lookAction.action.Disable();
            jumpAction.action.Disable();
        }
    }

    public void SetPlayerDialogueInput(bool toggle)
    {
        SetPlayerMovement(!toggle);
        if (toggle)
        {
            dialogueAction.action.Enable();
        }
        else
        {
            dialogueAction.action.Disable();
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

        Vector3 moveDirection =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        HandleVerticalMovement();

        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleVerticalMovement()
    {
        if (characterController.isGrounded)
        {
            ResetGroundedVelocity();
            TryJump();
        }

        ApplyGravity();
    }

    private void ResetGroundedVelocity()
    {
        if (verticalVelocity < 0f)
        {
            verticalVelocity = groundedVelocity;
        }
    }

    private void TryJump()
    {
        if (!jumpAction.action.WasPressedThisFrame())
        {
            return;
        }

        verticalVelocity = Mathf.Sqrt(
            jumpHeight * -2f * gravity
        );
    }

    private void ApplyGravity()
    {
        verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleLook()
    {
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        float yaw = lookInput.x * mouseSensitivity;
        float pitch = lookInput.y * mouseSensitivity;

        RotatePlayer(yaw);
        RotateCamera(pitch);
    }

    private void RotatePlayer(float yaw)
    {
        transform.Rotate(Vector3.up * yaw);
    }

    private void RotateCamera(float pitch)
    {
        cameraPitch -= pitch;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -maxLookAngle,
            maxLookAngle
        );

        playerCamera.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}