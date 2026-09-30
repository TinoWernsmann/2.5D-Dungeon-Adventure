using UnityEngine;

public class EnemyVisualController : MonoBehaviour
{
    private enum ViewDirection
    {
        Front,
        Back,
        Left,
        Right
    }

    [Header("References")]
    [SerializeField] private Transform enemyRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyAttack attack;
    [SerializeField] private Camera targetCamera;

    [Header("Movement - Front")]
    [SerializeField] private Sprite[] frontFrames;

    [Header("Movement - Back")]
    [SerializeField] private Sprite[] backFrames;

    [Header("Movement - Left")]
    [SerializeField] private Sprite[] leftFrames;

    [Header("Movement - Right")]
    [SerializeField] private Sprite[] rightFrames;

    [Header("Attack")]
    [SerializeField] private Sprite[] attackFrontFrames;

    [Header("Movement Animation")]
    [Min(0.01f)]
    [SerializeField] private float movementFramesPerSecond = 6f;

    private int movementFrame;
    private float movementFrameTimer;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        if (attack.IsAttacking)
        {
            UpdateAttackAnimation();
            return;
        }

        ViewDirection direction = GetViewDirection();
        UpdateMovementAnimation(direction);
    }

    private void UpdateAttackAnimation()
    {
        if (attackFrontFrames == null || attackFrontFrames.Length == 0)
        {
            return;
        }

        int frameIndex = Mathf.FloorToInt(
            attack.AttackProgress * attackFrontFrames.Length
        );

        frameIndex = Mathf.Clamp(
            frameIndex,
            0,
            attackFrontFrames.Length - 1
        );

        spriteRenderer.sprite = attackFrontFrames[frameIndex];
    }

    private void UpdateMovementAnimation(ViewDirection direction)
    {
        Sprite[] frames = GetMovementFrames(direction);

        if (frames == null || frames.Length == 0)
        {
            return;
        }

        if (!movement.IsMoving)
        {
            ShowIdleFrame(frames);
            return;
        }

        AnimateMovement(frames);
    }

    private void ShowIdleFrame(Sprite[] frames)
    {
        movementFrame = 0;
        movementFrameTimer = 0f;

        spriteRenderer.sprite = frames[0];
    }

    private void AnimateMovement(Sprite[] frames)
    {
        movementFrameTimer += Time.deltaTime;

        float frameDuration = 1f / movementFramesPerSecond;

        if (movementFrameTimer >= frameDuration)
        {
            movementFrameTimer -= frameDuration;
            movementFrame = (movementFrame + 1) % frames.Length;
        }

        movementFrame = Mathf.Clamp(
            movementFrame,
            0,
            frames.Length - 1
        );

        spriteRenderer.sprite = frames[movementFrame];
    }

    private ViewDirection GetViewDirection()
    {
        Vector3 directionToCamera =
            targetCamera.transform.position - enemyRoot.position;

        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude <= 0.001f)
        {
            return ViewDirection.Front;
        }

        directionToCamera.Normalize();

        float forwardDot = Vector3.Dot(
            enemyRoot.forward,
            directionToCamera
        );

        float rightDot = Vector3.Dot(
            enemyRoot.right,
            directionToCamera
        );

        if (Mathf.Abs(forwardDot) >= Mathf.Abs(rightDot))
        {
            return forwardDot >= 0f
                ? ViewDirection.Front
                : ViewDirection.Back;
        }

        return rightDot >= 0f
            ? ViewDirection.Right
            : ViewDirection.Left;
    }

    private Sprite[] GetMovementFrames(ViewDirection direction)
    {
        return direction switch
        {
            ViewDirection.Front => frontFrames,
            ViewDirection.Back => backFrames,
            ViewDirection.Left => leftFrames,
            ViewDirection.Right => rightFrames,
            _ => frontFrames
        };
    }

    private bool HasRequiredReferences()
    {
        return enemyRoot != null &&
               spriteRenderer != null &&
               movement != null &&
               attack != null &&
               targetCamera != null;
    }
}