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
    [SerializeField] private Health health;
    [SerializeField] private Transform deathPoint;

    [Header("Movement - Front")]
    [SerializeField] private Sprite[] frontFrames;

    [Header("Movement - Back")]
    [SerializeField] private Sprite[] backFrames;

    [Header("Movement - Left")]
    [SerializeField] private Sprite[] leftFrames;

    [Header("Movement - Right")]
    [SerializeField] private Sprite[] rightFrames;

    [Header("Idle - Front")]
    [SerializeField] private Sprite[] idleFrontFrames;

    [Header("Idle - Back")]
    [SerializeField] private Sprite[] idleBackFrames;

    [Header("Idle - Left")]
    [SerializeField] private Sprite[] idleLeftFrames;

    [Header("Idle - Right")]
    [SerializeField] private Sprite[] idleRightFrames;

    [Header("Attack")]
    [SerializeField] private Sprite[] attackFrontFrames;

    [Header("Death")]
    [SerializeField] private Sprite deadSprite;
    [SerializeField] private float corpseDuration = 5f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Movement Animation")]
    [Min(0.01f)]
    [SerializeField] private float movementFramesPerSecond = 6f;

    [Header("Idle Animation")]
    [Min(0.01f)]
    [SerializeField] private float idleFramesPerSecond = 4f;

    private int movementFrame;
    private float movementFrameTimer;

    private int idleFrame;
    private float idleFrameTimer;

    private bool isDead;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (health == null)
        {
            health = enemyRoot.GetComponent<Health>();
        }

        if (deathPoint == null)
        {
            foreach (Transform child in enemyRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "DeathPoint")
                {
                    deathPoint = child;
                    break;
                }
            }
        }

        if (health != null)
        {
            health.Died += HandleDeath;
        }
        else
        {
            Debug.LogError(
                "EnemyVisualController could not find enemy Health."
            );
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= HandleDeath;
        }
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        UpdateVisual();
    }

    private void HandleDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        DisableEnemySystems();

        if (deathPoint != null)
        {
            spriteRenderer.transform.position = deathPoint.position;
        }

        spriteRenderer.sprite = deadSprite;
        spriteRenderer.color = Color.white;

        StartCoroutine(FadeAndDestroy());
    }

    private void DisableEnemySystems()
    {
        enemyRoot.GetComponent<EnemyBrain>().enabled = false;
        movement.enabled = false;
        attack.enabled = false;
        enemyRoot.GetComponent<EnemyPatrol>().enabled = false;
        enemyRoot.GetComponent<EnemyPerception>().enabled = false;

        foreach (Collider enemyCollider in enemyRoot.GetComponentsInChildren<Collider>())
        {
            enemyCollider.enabled = false;
        }
    }

    private System.Collections.IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(corpseDuration);

        float elapsed = 0f;
        Color startColor = spriteRenderer.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            Color fadedColor = startColor;
            fadedColor.a = 1f - Mathf.Clamp01(
                elapsed / fadeDuration
            );

            spriteRenderer.color = fadedColor;

            yield return null;
        }

        Destroy(enemyRoot.gameObject);
    }

    private void UpdateVisual()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        if (attack.IsAttacking)
        {
            ResetMovementAnimation();
            ResetIdleAnimation();

            UpdateAttackAnimation();
            return;
        }

        ViewDirection direction = GetViewDirection();

        if (movement.IsMoving)
        {
            ResetIdleAnimation();
            UpdateMovementAnimation(direction);
            return;
        }

        ResetMovementAnimation();
        UpdateIdleAnimation(direction);
    }

    private void UpdateAttackAnimation()
    {
        if (attackFrontFrames == null ||
            attackFrontFrames.Length == 0)
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

        movementFrameTimer += Time.deltaTime;

        float frameDuration = 1f / movementFramesPerSecond;

        if (movementFrameTimer >= frameDuration)
        {
            movementFrameTimer -= frameDuration;

            movementFrame =
                (movementFrame + 1) % frames.Length;
        }

        movementFrame = Mathf.Clamp(
            movementFrame,
            0,
            frames.Length - 1
        );

        spriteRenderer.sprite = frames[movementFrame];
    }

    private void UpdateIdleAnimation(ViewDirection direction)
    {
        Sprite[] frames = GetIdleFrames(direction);

        if (frames == null || frames.Length == 0)
        {
            return;
        }

        idleFrameTimer += Time.deltaTime;

        float frameDuration = 1f / idleFramesPerSecond;

        if (idleFrameTimer >= frameDuration)
        {
            idleFrameTimer -= frameDuration;

            idleFrame =
                (idleFrame + 1) % frames.Length;
        }

        idleFrame = Mathf.Clamp(
            idleFrame,
            0,
            frames.Length - 1
        );

        spriteRenderer.sprite = frames[idleFrame];
    }

    private void ResetMovementAnimation()
    {
        movementFrame = 0;
        movementFrameTimer = 0f;
    }

    private void ResetIdleAnimation()
    {
        idleFrame = 0;
        idleFrameTimer = 0f;
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

    private Sprite[] GetIdleFrames(ViewDirection direction)
    {
        return direction switch
        {
            ViewDirection.Front => idleFrontFrames,
            ViewDirection.Back => idleBackFrames,
            ViewDirection.Left => idleLeftFrames,
            ViewDirection.Right => idleRightFrames,
            _ => idleFrontFrames
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