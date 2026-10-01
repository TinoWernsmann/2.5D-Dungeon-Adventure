using UnityEngine;

public class BossVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bossRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpearBossCombatBehaviour combatBehaviour;
    [SerializeField] private Health health;
    [SerializeField] private Camera targetCamera;

    [Header("Stand")]
    [SerializeField] private Sprite[] standFrames;
    [SerializeField] private Sprite standLeft;
    [SerializeField] private Sprite standRight;

    [Header("Forward")]
    [SerializeField] private Sprite[] forwardFrames;

    [Header("Back")]
    [SerializeField] private Sprite[] backFrames;

    [Header("Prepare Combat")]
    [SerializeField] private Sprite[] prepareCombatFrames;

    [Header("Attack")]
    [SerializeField] private Sprite attackLeft;
    [SerializeField] private Sprite attackRight;

    [Header("Dodge")]
    [SerializeField] private Sprite dodgeLeft;
    [SerializeField] private Sprite dodgeRight;

    [Header("Block")]
    [SerializeField] private Sprite blockSprite;

    [Header("Defeat")]
    [SerializeField] private Sprite[] defeatFrames;

    [Header("Animation Speed")]
    [Min(0.01f)]
    [SerializeField] private float standFramesPerSecond = 3f;

    [Min(0.01f)]
    [SerializeField] private float movementFramesPerSecond = 6f;

    [Min(0.01f)]
    [SerializeField] private float backFramesPerSecond = 5f;

    [Min(0.01f)]
    [SerializeField] private float prepareFramesPerSecond = 3.33f;

    [Min(0.01f)]
    [SerializeField] private float defeatFramesPerSecond = 3f;

    private BossVisualState previousState =
        BossVisualState.Stand;

    private int frameIndex;
    private float frameTimer;

    private bool isDefeated;

    private void Awake()
    {
        FindMissingReferences();
    }

    private void Start()
    {
        if (health != null)
        {
            health.Died += HandleDeath;
        }

        ResetAnimation();
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= HandleDeath;
        }
    }

    private void LateUpdate()
    {
        FaceCamera();

        if (isDefeated)
        {
            UpdateDefeatVisual();
            return;
        }

        UpdateVisual();
    }

    private void FindMissingReferences()
    {
        if (bossRoot == null)
        {
            bossRoot = transform.root;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (bossRoot == null)
        {
            return;
        }

        if (combatBehaviour == null)
        {
            combatBehaviour =
                bossRoot.GetComponent<
                    SpearBossCombatBehaviour
                >();
        }

        if (health == null)
        {
            health =
                bossRoot.GetComponent<Health>();
        }
    }

    private void UpdateVisual()
    {
        if (combatBehaviour == null ||
            spriteRenderer == null)
        {
            return;
        }

        BossVisualState state =
            combatBehaviour.VisualState;

        HandleStateChange(state);

        switch (state)
        {
            case BossVisualState.Stand:
                PlayLoop(
                    standFrames,
                    standFramesPerSecond
                );
                break;

            case BossVisualState.StandLeft:
                SetSprite(standLeft);
                break;

            case BossVisualState.StandRight:
                SetSprite(standRight);
                break;

            case BossVisualState.Forward:
                PlayLoop(
                    forwardFrames,
                    movementFramesPerSecond
                );
                break;

            case BossVisualState.Back:
                PlayBackAnimation();
                break;

            case BossVisualState.PrepareCombat:
                PlayPrepareCombatLoop();
                break;

            case BossVisualState.CombatIdle:
                PlayPrepareCombatLoop();
                break;

            case BossVisualState.AttackLeft:
                SetSprite(attackLeft);
                break;

            case BossVisualState.AttackRight:
                SetSprite(attackRight);
                break;

            case BossVisualState.DodgeLeft:
                SetSprite(dodgeLeft);
                break;

            case BossVisualState.DodgeRight:
                SetSprite(dodgeRight);
                break;

            case BossVisualState.Block:
                SetSprite(blockSprite);
                break;

            case BossVisualState.Defeated:
                break;
        }
    }

    private void HandleStateChange(
        BossVisualState newState)
    {
        if (newState == previousState)
        {
            return;
        }

        BossVisualState oldState =
            previousState;

        previousState = newState;

        bool switchingBetweenCombatIdleStates =
            IsPrepareLoopState(oldState) &&
            IsPrepareLoopState(newState);

        if (switchingBetweenCombatIdleStates)
        {
            return;
        }

        ResetAnimation();
    }

    private bool IsPrepareLoopState(
        BossVisualState state)
    {
        return state ==
                   BossVisualState.PrepareCombat ||
               state ==
                   BossVisualState.CombatIdle;
    }

    private void PlayPrepareCombatLoop()
    {
        PlayLoop(
            prepareCombatFrames,
            prepareFramesPerSecond
        );
    }

    private void PlayBackAnimation()
    {
        PlayOnceAndHoldLastFrame(
            backFrames,
            backFramesPerSecond
        );
    }

    private void UpdateDefeatVisual()
    {
        PlayLoop(
            defeatFrames,
            defeatFramesPerSecond
        );
    }

    private void PlayLoop(
        Sprite[] frames,
        float framesPerSecond)
    {
        if (!HasFrames(frames))
        {
            return;
        }

        if (framesPerSecond <= 0f)
        {
            return;
        }

        float frameDuration =
            1f / framesPerSecond;

        frameTimer += Time.deltaTime;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;

            frameIndex++;

            if (frameIndex >= frames.Length)
            {
                frameIndex = 0;
            }
        }

        SetCurrentFrame(frames);
    }

    private void PlayOnceAndHoldLastFrame(
        Sprite[] frames,
        float framesPerSecond)
    {
        if (!HasFrames(frames))
        {
            return;
        }

        if (framesPerSecond <= 0f)
        {
            return;
        }

        if (frameIndex >= frames.Length - 1)
        {
            frameIndex =
                frames.Length - 1;

            SetCurrentFrame(frames);
            return;
        }

        float frameDuration =
            1f / framesPerSecond;

        frameTimer += Time.deltaTime;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;

            if (frameIndex >=
                frames.Length - 1)
            {
                frameIndex =
                    frames.Length - 1;

                break;
            }
        }

        SetCurrentFrame(frames);
    }

    private void SetCurrentFrame(
        Sprite[] frames)
    {
        if (!HasFrames(frames))
        {
            return;
        }

        frameIndex = Mathf.Clamp(
            frameIndex,
            0,
            frames.Length - 1
        );

        SetSprite(
            frames[frameIndex]
        );
    }

    private void SetSprite(
        Sprite sprite)
    {
        if (spriteRenderer == null ||
            sprite == null)
        {
            return;
        }

        spriteRenderer.sprite = sprite;
    }

    private bool HasFrames(
        Sprite[] frames)
    {
        return frames != null &&
               frames.Length > 0;
    }

    private void ResetAnimation()
    {
        frameIndex = 0;
        frameTimer = 0f;
    }

    private void HandleDeath()
    {
        if (isDefeated)
        {
            return;
        }

        isDefeated = true;

        ResetAnimation();

        if (combatBehaviour != null)
        {
            combatBehaviour.EnterDeathState();
        }

        DisableBossSystems();
    }

    private void DisableBossSystems()
    {
        if (bossRoot == null)
        {
            return;
        }

        EnemyBrain brain =
            bossRoot.GetComponent<EnemyBrain>();

        EnemyPatrol patrol =
            bossRoot.GetComponent<EnemyPatrol>();

        EnemyPerception perception =
            bossRoot.GetComponent<
                EnemyPerception
            >();

        EnemyMovement movement =
            bossRoot.GetComponent<
                EnemyMovement
            >();

        if (brain != null)
        {
            brain.enabled = false;
        }

        if (patrol != null)
        {
            patrol.enabled = false;
        }

        if (perception != null)
        {
            perception.enabled = false;
        }

        if (movement != null)
        {
            movement.Stop();
            movement.enabled = false;
        }

        DisableBossColliders();
    }

    private void DisableBossColliders()
    {
        Collider[] colliders =
            bossRoot.GetComponentsInChildren<
                Collider
            >();

        foreach (Collider bossCollider in colliders)
        {
            bossCollider.enabled = false;
        }
    }

    private void FaceCamera()
    {
        if (targetCamera == null ||
            spriteRenderer == null)
        {
            return;
        }

        Transform visualTransform =
            spriteRenderer.transform;

        Vector3 directionToCamera =
            targetCamera.transform.position -
            visualTransform.position;

        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude
            <= 0.001f)
        {
            return;
        }

        visualTransform.rotation =
            Quaternion.LookRotation(
                directionToCamera.normalized
            );
    }
}