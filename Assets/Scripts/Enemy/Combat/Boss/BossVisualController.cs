using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Wählt die Sprites des Bosses anhand seines <see cref="BossVisualState"/>.
/// Laufen und Stehen werden abhängig vom Kamerawinkel gezeigt (vorne / hinten / links / rechts),
/// damit man sieht, wohin der Boss wirklich schaut. Angriffe und Blocken zeigen immer die Front,
/// weil der Boss dabei zum Player schaut.
/// (Das Ausrichten zur Kamera übernimmt die <see cref="EnemyBillboard"/>-Komponente.)
/// </summary>
public class BossVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bossRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpearBossCombatBehaviour combatBehaviour;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private Health health;
    [SerializeField] private Camera targetCamera;

    [Header("Front - Stand / Walk")]
    [SerializeField] private Sprite[] standFrames;
    [SerializeField] private Sprite[] forwardFrames;

    [Header("Side - Stand")]
    [SerializeField] private Sprite standLeft;
    [SerializeField] private Sprite standRight;

    [Header("Back View")]
    [Tooltip("Rückenansicht. Das erste Bild wird im Stehen verwendet.")]
    [SerializeField] private Sprite[] backFrames;

    [Header("Combat Stance")]
    [SerializeField] private Sprite[] prepareCombatFrames;

    [Header("Attack")]
    [FormerlySerializedAs("dodgeLeft")]
    [SerializeField] private Sprite windupLeft;

    [FormerlySerializedAs("dodgeRight")]
    [SerializeField] private Sprite windupRight;

    [SerializeField] private Sprite attackLeft;
    [SerializeField] private Sprite attackRight;

    [Header("Block")]
    [SerializeField] private Sprite blockSprite;

    [Header("Defeat")]
    [SerializeField] private Sprite[] defeatFrames;

    [Tooltip("Wird nach dem Dialog nach der Niederlage dauerhaft angezeigt.")]
    [SerializeField] private Sprite defeatCrySprite;

    [Header("Animation Speed")]
    [Min(0.01f)]
    [SerializeField] private float standFramesPerSecond = 3f;

    [Min(0.01f)]
    [SerializeField] private float movementFramesPerSecond = 6f;

    [Min(0.01f)]
    [SerializeField] private float prepareFramesPerSecond = 3.33f;

    [Min(0.01f)]
    [SerializeField] private float defeatFramesPerSecond = 3f;

    private BossVisualState previousState = BossVisualState.Idle;
    private SpriteViewDirection previousDirection = SpriteViewDirection.Front;

    private int frameIndex;
    private float frameTimer;

    private bool isDefeated;
    private bool isShowingFinalPose;

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
        if (spriteRenderer == null || combatBehaviour == null)
        {
            return;
        }

        if (isShowingFinalPose)
        {
            SetSprite(defeatCrySprite);
            return;
        }

        if (isDefeated)
        {
            PlayLoop(defeatFrames, defeatFramesPerSecond);
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
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (combatBehaviour == null)
        {
            combatBehaviour = bossRoot.GetComponent<SpearBossCombatBehaviour>();
        }

        if (movement == null)
        {
            movement = bossRoot.GetComponent<EnemyMovement>();
        }

        if (health == null)
        {
            health = bossRoot.GetComponent<Health>();
        }
    }

    private void UpdateVisual()
    {
        BossVisualState state = combatBehaviour.VisualState;
        SpriteViewDirection direction = GetViewDirection(state);

        HandleAnimationChange(state, direction);

        if (direction != SpriteViewDirection.Front)
        {
            ShowFromSideOrBack(direction);
            return;
        }

        ShowFromFront(state);
    }

    private SpriteViewDirection GetViewDirection(BossVisualState state)
    {
        bool usesDirectionalSprites =
            state == BossVisualState.Idle ||
            state == BossVisualState.Walk ||
            state == BossVisualState.CombatIdle;

        if (!usesDirectionalSprites || targetCamera == null)
        {
            return SpriteViewDirection.Front;
        }

        return SpriteViewDirectionResolver.Resolve(
            bossRoot,
            targetCamera.transform.position
        );
    }

    private void ShowFromFront(BossVisualState state)
    {
        switch (state)
        {
            case BossVisualState.Idle:
                // Fallback, falls keine Steh-Sprites zugewiesen sind.
                PlayLoop(
                    HasFrames(standFrames) ? standFrames : forwardFrames,
                    standFramesPerSecond
                );
                break;

            case BossVisualState.Walk:
                PlayLoop(forwardFrames, movementFramesPerSecond);
                break;

            case BossVisualState.PrepareCombat:
            case BossVisualState.CombatIdle:
                PlayLoop(prepareCombatFrames, prepareFramesPerSecond);
                break;

            case BossVisualState.WindupLeft:
                SetSprite(windupLeft);
                break;

            case BossVisualState.WindupRight:
                SetSprite(windupRight);
                break;

            case BossVisualState.AttackLeft:
                SetSprite(attackLeft);
                break;

            case BossVisualState.AttackRight:
                SetSprite(attackRight);
                break;

            case BossVisualState.Block:
                SetSprite(blockSprite);
                break;
        }
    }

    private void ShowFromSideOrBack(SpriteViewDirection direction)
    {
        switch (direction)
        {
            case SpriteViewDirection.Back:
                ShowBack();
                break;

            case SpriteViewDirection.Left:
                SetSprite(standLeft);
                break;

            case SpriteViewDirection.Right:
                SetSprite(standRight);
                break;
        }
    }

    private void ShowBack()
    {
        bool isMoving = movement != null && movement.IsMoving;

        if (isMoving)
        {
            PlayLoop(backFrames, movementFramesPerSecond);
            return;
        }

        if (HasFrames(backFrames))
        {
            SetSprite(backFrames[0]);
        }
    }

    private void HandleAnimationChange(
        BossVisualState newState,
        SpriteViewDirection newDirection)
    {
        bool stateChanged = newState != previousState;
        bool directionChanged = newDirection != previousDirection;

        bool isSameStanceLoop =
            IsCombatStance(previousState) &&
            IsCombatStance(newState);

        previousState = newState;
        previousDirection = newDirection;

        if (directionChanged || (stateChanged && !isSameStanceLoop))
        {
            ResetAnimation();
        }
    }

    private static bool IsCombatStance(BossVisualState state)
    {
        return state == BossVisualState.PrepareCombat ||
               state == BossVisualState.CombatIdle;
    }

    private void PlayLoop(Sprite[] frames, float framesPerSecond)
    {
        if (!HasFrames(frames))
        {
            return;
        }

        float frameDuration = 1f / framesPerSecond;

        frameTimer += Time.deltaTime;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
        }

        frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);

        SetSprite(frames[frameIndex]);
    }

    private void SetSprite(Sprite sprite)
    {
        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    /// <summary>
    /// true, wenn die Animation abspielbar ist. Fehlende Sprites (z.B. gelöschte Bilder)
    /// zählen nicht – sonst bliebe einfach das zuletzt gezeigte Sprite hängen.
    /// </summary>
    private static bool HasFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
        {
            return false;
        }

        foreach (Sprite frame in frames)
        {
            if (frame == null)
            {
                return false;
            }
        }

        return true;
    }

    private void ResetAnimation()
    {
        frameIndex = 0;
        frameTimer = 0f;
    }

    /// <summary>Zeigt ab jetzt dauerhaft das "DefeatCry"-Sprite.</summary>
    public void ShowFinalDefeatPose()
    {
        isShowingFinalPose = true;
    }

    private void HandleDeath()
    {
        if (isDefeated)
        {
            return;
        }

        isDefeated = true;

        ResetAnimation();
        DisableBossSystems();
    }

    private void DisableBossSystems()
    {
        DisableComponent<EnemyBrain>();
        DisableComponent<EnemyPatrol>();
        DisableComponent<EnemyPerception>();

        if (movement != null)
        {
            movement.Stop();
            movement.enabled = false;
        }

        foreach (Collider bossCollider in bossRoot.GetComponentsInChildren<Collider>())
        {
            bossCollider.enabled = false;
        }
    }

    private void DisableComponent<T>() where T : Behaviour
    {
        T component = bossRoot.GetComponent<T>();

        if (component != null)
        {
            component.enabled = false;
        }
    }
}
