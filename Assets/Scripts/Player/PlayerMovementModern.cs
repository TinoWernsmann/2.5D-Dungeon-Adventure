using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private FootstepAudio footsteps;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jumping & Gravity")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedVelocity = -2f;

    [Tooltip("Ab dieser Fallgeschwindigkeit wird ein Lande-Sound abgespielt " +
             "(verhindert Sounds bei kleinen Stufen).")]
    [Min(0f)]
    [SerializeField] private float minLandingSpeed = 3f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float maxLookAngle = 85f;

    private CharacterController characterController;

    private float verticalVelocity;
    private float cameraPitch;
    private bool wasGrounded = true;

    // Pausiert = Zeit angehalten. Kein eigener Pause-Zustand, der mit dem
    // PauseMenu auseinanderlaufen kann (z.B. beim Fortsetzen über den Button).
    private static bool IsGamePaused => Time.timeScale <= 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (footsteps == null)
        {
            footsteps = GetComponent<FootstepAudio>();
        }
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

        UpdateFootsteps();
    }

    private void UpdateFootsteps()
    {
        if (footsteps == null)
        {
            return;
        }

        bool isGrounded = characterController.isGrounded;
        float fallSpeed = -verticalVelocity;

        if (isGrounded && !wasGrounded && fallSpeed >= minLandingSpeed)
        {
            footsteps.PlayLand();
        }

        Vector3 horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;

        footsteps.UpdateMovement(
            horizontalVelocity.magnitude,
            isGrounded,
            Time.deltaTime
        );

        wasGrounded = isGrounded;
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

        if (footsteps != null)
        {
            footsteps.PlayJump();
        }
    }

    private void ApplyGravity()
    {
        verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleLook()
    {
        if (IsGamePaused) return;

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