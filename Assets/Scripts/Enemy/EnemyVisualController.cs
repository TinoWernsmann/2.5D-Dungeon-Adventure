using System.Collections;
using UnityEngine;

public class EnemyVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform enemyRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyCombatBehaviour combatBehaviour;
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
        FindMissingReferences();

        if (health != null)
        {
            health.Died += HandleDeath;
        }
        else
        {
            Debug.LogError(
                $"{gameObject.name}: EnemyVisualController " +
                "could not find enemy Health."
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

    private void FindMissingReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (enemyRoot == null)
        {
            enemyRoot = transform.root;
        }

        if (movement == null)
        {
            movement =
                enemyRoot.GetComponent<EnemyMovement>();
        }

        if (combatBehaviour == null)
        {
            combatBehaviour =
                enemyRoot.GetComponent<EnemyCombatBehaviour>();
        }

        if (health == null)
        {
            health =
                enemyRoot.GetComponent<Health>();
        }

        if (deathPoint == null)
        {
            FindDeathPoint();
        }
    }

    private void FindDeathPoint()
    {
        Transform[] children =
            enemyRoot.GetComponentsInChildren<Transform>(
                true
            );

        foreach (Transform child in children)
        {
            if (child.name != "DeathPoint")
            {
                continue;
            }

            deathPoint = child;
            return;
        }
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
            spriteRenderer.transform.position =
                deathPoint.position;
        }

        spriteRenderer.sprite = deadSprite;
        spriteRenderer.color = Color.white;

        StartCoroutine(FadeAndDestroy());
    }

    private void DisableEnemySystems()
    {
        EnemyBrain brain =
            enemyRoot.GetComponent<EnemyBrain>();

        EnemyPatrol patrol =
            enemyRoot.GetComponent<EnemyPatrol>();

        EnemyPerception perception =
            enemyRoot.GetComponent<EnemyPerception>();

        if (brain != null)
        {
            brain.enabled = false;
        }

        if (movement != null)
        {
            movement.enabled = false;
        }

        if (combatBehaviour != null)
        {
            combatBehaviour.enabled = false;
        }

        if (patrol != null)
        {
            patrol.enabled = false;
        }

        if (perception != null)
        {
            perception.enabled = false;
        }

        Collider[] colliders =
            enemyRoot.GetComponentsInChildren<Collider>();

        foreach (Collider enemyCollider in colliders)
        {
            enemyCollider.enabled = false;
        }
    }

    private IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(
            corpseDuration
        );

        float elapsed = 0f;

        Color startColor =
            spriteRenderer.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            Color fadedColor = startColor;

            fadedColor.a =
                1f -
                Mathf.Clamp01(
                    elapsed / fadeDuration
                );

            spriteRenderer.color =
                fadedColor;

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

        if (combatBehaviour.IsPerformingAttack)
        {
            ResetMovementAnimation();
            ResetIdleAnimation();

            UpdateAttackAnimation();
            return;
        }

        SpriteViewDirection direction =
            GetViewDirection();

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

        int frameIndex =
            Mathf.FloorToInt(
                combatBehaviour.AttackProgress *
                attackFrontFrames.Length
            );

        frameIndex =
            Mathf.Clamp(
                frameIndex,
                0,
                attackFrontFrames.Length - 1
            );

        spriteRenderer.sprite =
            attackFrontFrames[frameIndex];
    }

    private void UpdateMovementAnimation(
        SpriteViewDirection direction)
    {
        Sprite[] frames =
            GetMovementFrames(direction);

        if (frames == null ||
            frames.Length == 0)
        {
            return;
        }

        movementFrameTimer +=
            Time.deltaTime;

        float frameDuration =
            1f / movementFramesPerSecond;

        if (movementFrameTimer >= frameDuration)
        {
            movementFrameTimer -=
                frameDuration;

            movementFrame =
                (movementFrame + 1) %
                frames.Length;
        }

        movementFrame =
            Mathf.Clamp(
                movementFrame,
                0,
                frames.Length - 1
            );

        spriteRenderer.sprite =
            frames[movementFrame];
    }

    private void UpdateIdleAnimation(
        SpriteViewDirection direction)
    {
        Sprite[] frames =
            GetIdleFrames(direction);

        if (frames == null ||
            frames.Length == 0)
        {
            return;
        }

        idleFrameTimer +=
            Time.deltaTime;

        float frameDuration =
            1f / idleFramesPerSecond;

        if (idleFrameTimer >= frameDuration)
        {
            idleFrameTimer -=
                frameDuration;

            idleFrame =
                (idleFrame + 1) %
                frames.Length;
        }

        idleFrame =
            Mathf.Clamp(
                idleFrame,
                0,
                frames.Length - 1
            );

        spriteRenderer.sprite =
            frames[idleFrame];
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

    private SpriteViewDirection GetViewDirection()
    {
        return SpriteViewDirectionResolver.Resolve(
            enemyRoot,
            targetCamera.transform.position
        );
    }

    private Sprite[] GetMovementFrames(
        SpriteViewDirection direction)
    {
        return direction switch
        {
            SpriteViewDirection.Front =>
                frontFrames,

            SpriteViewDirection.Back =>
                backFrames,

            SpriteViewDirection.Left =>
                leftFrames,

            SpriteViewDirection.Right =>
                rightFrames,

            _ => frontFrames
        };
    }

    private Sprite[] GetIdleFrames(
        SpriteViewDirection direction)
    {
        return direction switch
        {
            SpriteViewDirection.Front =>
                idleFrontFrames,

            SpriteViewDirection.Back =>
                idleBackFrames,

            SpriteViewDirection.Left =>
                idleLeftFrames,

            SpriteViewDirection.Right =>
                idleRightFrames,

            _ => idleFrontFrames
        };
    }

    private bool HasRequiredReferences()
    {
        return enemyRoot != null &&
               spriteRenderer != null &&
               movement != null &&
               combatBehaviour != null &&
               targetCamera != null;
    }
}