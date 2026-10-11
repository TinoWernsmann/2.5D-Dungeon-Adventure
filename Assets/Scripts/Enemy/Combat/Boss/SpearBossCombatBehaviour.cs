using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Speer-Boss. Dieses Script ist nur der "Dirigent":
/// Es entscheidet, WELCHE Aktion als Nächstes kommt. Die Aktionen selbst
/// (Kombo, Sprungangriff, Ausweichen, Blocken, ...) liegen in eigenen Klassen unter Actions/.
///
/// Kampfablauf:
///  1. Kampfhaltung einnehmen, sobald er den Player sieht.
///  2. Zwischen den Angriffen hält er Abstand und umkreist den Player (Footsies).
///  3. Angriff wählen: Kombo, Seitschritt + Konter oder Sprungangriff aus der Distanz.
///  4. Nach Angriffen: Verschnaufen (Angriffsfenster für den Player!) oder Rückzug.
///  5. Blocken nur von vorne und nur, wenn er gerade nichts anderes tut.
///     Spammt der Player Angriffe, blockt er häufiger (er "lernt").
///  6. Phasenwechsel bei bestimmten Lebenspunkten: kurz unverwundbar, danach schneller und aggressiver.
///
/// Der Kampfbeginn kann angehalten werden (<see cref="HoldEncounter"/>), z.B. für einen Intro-Dialog.
/// </summary>
public class SpearBossCombatBehaviour : EnemyCombatBehaviour, IBossActionContext
{
    [Header("Encounter")]
    [SerializeField] private Health health;
    [SerializeField] private GameObject _doorToDespawn;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip phaseTransitionSound;

    [Header("Quest")]
    [SerializeField] private string _questTargetId = "SpearBoss";

    [Header("Ranges")]
    [Min(0.1f)]
    [SerializeField] private float combatRange = 8f;

    [Min(0.1f)]
    [SerializeField] private float attackRange = 3f;

    [Min(0.1f)]
    [Tooltip("Abstand, den der Boss zwischen seinen Angriffen hält.")]
    [SerializeField] private float preferredDistance = 2.3f;

    [Min(0.1f)]
    [Tooltip("Bis zu dieser Distanz kann der Boss einen Sprungangriff machen.")]
    [SerializeField] private float lungeMaxRange = 7f;

    [Header("Block")]
    [Range(0f, 360f)]
    [SerializeField] private float blockAngle = 100f;

    [Range(0f, 1f)]
    [Tooltip("Zusätzliche Blockchance pro erlittenem Treffer (Player spammt Angriffe).")]
    [SerializeField] private float blockPressurePerHit = 0.12f;

    [Range(0f, 1f)]
    [SerializeField] private float maxBlockPressure = 0.35f;

    [Min(0f)]
    [SerializeField] private float blockPressureDecayPerSecond = 0.2f;

    [SerializeField] private bool debugBlock;

    [Header("Phase Thresholds")]
    [Range(0.01f, 0.99f)]
    [SerializeField] private float phaseTwoHealthPercent = 0.65f;

    [Range(0.01f, 0.99f)]
    [SerializeField] private float phaseThreeHealthPercent = 0.30f;

    [Header("Phases")]
    [SerializeField] private BossPhaseSettings phaseOne = new(
        blockChance: 0.25f, dodgeChance: 0.25f, backstepChance: 0.45f, lungeChance: 0.25f,
        comboHits: 1, decisionDelay: 0.9f, speedMultiplier: 1f, damageMultiplier: 1f);

    [SerializeField] private BossPhaseSettings phaseTwo = new(
        blockChance: 0.45f, dodgeChance: 0.45f, backstepChance: 0.25f, lungeChance: 0.4f,
        comboHits: 2, decisionDelay: 0.6f, speedMultiplier: 1.15f, damageMultiplier: 1.1f);

    [SerializeField] private BossPhaseSettings phaseThree = new(
        blockChance: 0.5f, dodgeChance: 0.6f, backstepChance: 0.1f, lungeChance: 0.55f,
        comboHits: 3, decisionDelay: 0.35f, speedMultiplier: 1.3f, damageMultiplier: 1.2f);

    [Header("Actions")]
    [SerializeField] private BossPoseAction prepareStance =
        new(duration: 0.9f, BossVisualState.PrepareCombat);

    [SerializeField] private BossStrikeAction comboStrike =
        new(windupDuration: 0.45f, strikeDuration: 0.5f, range: 3f, damage: 5);

    [SerializeField] private BossLungeAction lunge = new();

    [SerializeField] private BossDashAction sidestep =
        new(distance: 2.5f, duration: 0.45f);

    [SerializeField] private BossStrikeAction dodgeCounter =
        new(windupDuration: 0.2f, strikeDuration: 0.55f, range: 3.5f, damage: 5);

    [SerializeField] private BossDashAction backstep =
        new(distance: 2f, duration: 0.5f);

    [SerializeField] private BossPoseAction block =
        new(duration: 0.35f, BossVisualState.Block, blocksFrontalDamage: true);

    [SerializeField] private BossStrikeAction blockCounter =
        new(windupDuration: 0.15f, strikeDuration: 0.5f, range: 3.2f, damage: 5);

    [Tooltip("Verschnaufpause nach einem Angriff: dreht sich nicht mit, blockt nicht.")]
    [SerializeField] private BossPoseAction recover =
        new(duration: 0.8f, BossVisualState.Idle, facesTarget: false);

    [SerializeField] private BossPoseAction phaseTransition =
        new(duration: 1.2f, BossVisualState.PrepareCombat, isInvulnerable: true);

    private BossAction currentAction;
    private BossAction lastChosenAttack;

    private BossPhase currentPhase = BossPhase.PhaseOne;

    private bool isCombatPrepared;
    private bool isEngaged;
    private bool isDead;
    private bool isPhaseTransitionPending;
    private bool questReported;
    private bool hasSpottedPlayer;
    private bool isEncounterOnHold;

    private float decisionTimer;
    private float blockPressure;

    /// <summary>Der Player wurde zum ersten Mal entdeckt (z.B. für einen Intro-Dialog).</summary>
    public event Action PlayerSpottedFirstTime;

    /// <summary>Der Boss hat seine Waffe gezogen und beginnt zu kämpfen.</summary>
    public event Action CombatStarted;

    private BossPhaseSettings Phase => currentPhase switch
    {
        BossPhase.PhaseTwo => phaseTwo,
        BossPhase.PhaseThree => phaseThree,
        _ => phaseOne
    };

    public BossVisualState VisualState
    {
        get
        {
            if (isDead)
            {
                return BossVisualState.Defeated;
            }

            if (currentAction != null)
            {
                return currentAction.VisualState;
            }

            if (!isCombatPrepared)
            {
                return movement.IsMoving
                    ? BossVisualState.Walk
                    : BossVisualState.Idle;
            }

            return BossVisualState.CombatIdle;
        }
    }

    public override bool IsPerformingAttack =>
        currentAction != null && currentAction.IsAttacking;

    public override float AttackProgress =>
        currentAction?.AttackProgress ?? 0f;

    // Nur zwischen zwei Aktionen ausbrechen – nicht mitten im Block, Phasenwechsel usw.
    protected override bool CanEscapeFromCorner =>
        !isDead &&
        isCombatPrepared &&
        currentAction == null &&
        !isPhaseTransitionPending;

    #region Unity

    protected override void Awake()
    {
        base.Awake();

        if (health == null)
        {
            health = GetComponent<Health>();
        }

        foreach (BossAction action in AllActions())
        {
            action.Bind(this);
        }
    }

    private void OnEnable()
    {
        health.Died += HandleDeath;
        health.Damaged += HandleDamaged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        health.Died -= HandleDeath;
        health.Damaged -= HandleDamaged;
    }

    private void Update()
    {
        blockPressure = Mathf.Max(
            0f,
            blockPressure - blockPressureDecayPerSecond * Time.deltaTime
        );
    }

    #endregion

    #region Brain Hooks

    public override bool IsInCombatRange(Transform target)
    {
        return GetHorizontalDistance(target) <= combatRange;
    }

    public override void OnTargetSpotted(Transform target, Health targetHealth)
    {
        if (isDead)
        {
            return;
        }

        SetTarget(target, targetHealth);
        isEngaged = true;

        if (!hasSpottedPlayer)
        {
            hasSpottedPlayer = true;
            PlayerSpottedFirstTime?.Invoke();
        }

        EnsureCombatStance();
    }

    public override void OnTargetLost()
    {
        AbortCurrentAction();

        isEngaged = false;
        isCombatPrepared = false;

        movement.ResetRotationControl();
    }

    public override void UpdateChase(Transform target)
    {
        if (isDead)
        {
            return;
        }

        if (isEncounterOnHold)
        {
            movement.StopAndFace(target);
            return;
        }

        if (currentAction != null)
        {
            TickCurrentAction();
            return;
        }

        movement.MoveToWhileFacing(target.position, target);
    }

    protected override void OnCombatEntered()
    {
        isEngaged = true;
        decisionTimer = Phase.DecisionDelay;

        EnsureCombatStance();
    }

    protected override void OnCombatUpdate()
    {
        if (isDead)
        {
            return;
        }

        if (isEncounterOnHold)
        {
            movement.StopAndFace(Target);
            return;
        }

        UpdatePhase();

        if (currentAction != null)
        {
            TickCurrentAction();
            return;
        }

        if (!isCombatPrepared)
        {
            EnsureCombatStance();
            return;
        }

        if (isPhaseTransitionPending)
        {
            StartPhaseTransition();
            return;
        }

        UpdateFootsies();
    }

    protected override void OnCombatExited()
    {
        AbortCurrentAction();
        movement.Stop();
    }

    /// <summary>Hält den Kampfbeginn an: Der Boss steht still und schaut zum Player.</summary>
    public void HoldEncounter()
    {
        isEncounterOnHold = true;

        AbortCurrentAction();
        movement.Stop();

        // Sofort zum Player drehen, nicht erst langsam während des Dialogs.
        if (Target != null)
        {
            movement.LookInDirection(DirectionToTarget());
        }
    }

    /// <summary>Der Kampf darf beginnen (zieht zuerst seine Waffe).</summary>
    public void ReleaseEncounter()
    {
        isEncounterOnHold = false;
    }

    private void EnsureCombatStance()
    {
        if (isEncounterOnHold ||
            isCombatPrepared ||
            currentAction != null)
        {
            return;
        }

        StartAction(prepareStance);
    }

    #endregion

    #region Decision Making

    /// <summary>Zwischen den Angriffen: Abstand halten, umkreisen, auf eine Lücke warten.</summary>
    private void UpdateFootsies()
    {
        float distance = DistanceToTarget();

        decisionTimer -= Time.deltaTime;

        if (distance > lungeMaxRange)
        {
            movement.MoveToWhileFacing(Target.position, Target);
            return;
        }

        if (decisionTimer > 0f)
        {
            HoldDistance(distance);
            return;
        }

        ChooseAttack(distance);
    }

    private void HoldDistance(float distance)
    {
        const float Tolerance = 0.5f;

        if (distance > preferredDistance + Tolerance)
        {
            movement.MoveToWhileFacing(Target.position, Target);
            return;
        }

        if (distance < preferredDistance - Tolerance)
        {
            movement.MoveToWhileFacing(GetRetreatPoint(1f), Target);
            return;
        }

        StrafeAroundTarget(preferredDistance, stepDegrees: 25f);
    }

    private void ChooseAttack(float distance)
    {
        if (distance > attackRange)
        {
            if (Random.value <= Phase.LungeChance)
            {
                StartAttack(lunge);
                return;
            }

            // Kein Sprungangriff -> näher ran und kurz danach neu entscheiden.
            decisionTimer = 0.3f;
            return;
        }

        bool dodgedLastTime = lastChosenAttack == sidestep;

        if (!dodgedLastTime && Random.value <= Phase.DodgeChance)
        {
            StartSidestep();
            return;
        }

        comboStrike.SetHitCount(Phase.ComboHits);
        StartAttack(comboStrike);
    }

    private void OnActionFinished(BossAction finishedAction)
    {
        decisionTimer = Phase.DecisionDelay;

        if (finishedAction == prepareStance)
        {
            isCombatPrepared = true;
            CombatStarted?.Invoke();
        }
        else if (finishedAction == sidestep)
        {
            StartAction(dodgeCounter);
        }
        else if (finishedAction == block)
        {
            StartAction(blockCounter);
        }
        else if (finishedAction.IsAttacking)
        {
            FollowUpAfterAttack();
        }
        else if (finishedAction == backstep)
        {
            TryMixUpLunge();
        }
    }

    private void FollowUpAfterAttack()
    {
        if (Random.value <= Phase.BackstepChance)
        {
            StartBackstep();
            return;
        }

        StartAction(recover);
    }

    /// <summary>Ab Phase 2: Rückzug direkt gefolgt von einem Sprungangriff.</summary>
    private void TryMixUpLunge()
    {
        bool canMixUp =
            currentPhase != BossPhase.PhaseOne &&
            HasValidTarget() &&
            DistanceToTarget() <= lungeMaxRange &&
            Random.value <= Phase.LungeChance;

        if (canMixUp)
        {
            StartAction(lunge);
        }
    }

    private void StartAttack(BossAction attack)
    {
        lastChosenAttack = attack;
        StartAction(attack);
    }

    private void StartSidestep()
    {
        bool toLeft = Random.value < 0.5f;

        sidestep.SetDirection(
            toLeft ? -transform.right : transform.right,
            toLeft ? BossVisualState.WindupLeft : BossVisualState.WindupRight
        );

        StartAttack(sidestep);
    }

    private void StartBackstep()
    {
        backstep.SetDirection(-DirectionToTarget(), BossVisualState.CombatIdle);
        StartAction(backstep);
    }

    #endregion

    #region Action Handling

    private void StartAction(BossAction action)
    {
        currentAction?.Exit();

        currentAction = action;
        currentAction.Enter();
    }

    private void TickCurrentAction()
    {
        if (!currentAction.Tick(Time.deltaTime))
        {
            return;
        }

        BossAction finishedAction = currentAction;

        currentAction.Exit();
        currentAction = null;

        OnActionFinished(finishedAction);
    }

    private void AbortCurrentAction()
    {
        currentAction?.Exit();
        currentAction = null;
    }

    private BossAction[] AllActions()
    {
        return new BossAction[]
        {
            prepareStance, comboStrike, lunge, sidestep, dodgeCounter,
            backstep, block, blockCounter, recover, phaseTransition
        };
    }

    #endregion

    #region Phases

    private void UpdatePhase()
    {
        if (health.MaxHealth <= 0)
        {
            return;
        }

        float healthPercent = (float)health.CurrentHealth / health.MaxHealth;

        BossPhase newPhase =
            healthPercent <= phaseThreeHealthPercent ? BossPhase.PhaseThree :
            healthPercent <= phaseTwoHealthPercent ? BossPhase.PhaseTwo :
            BossPhase.PhaseOne;

        if (newPhase <= currentPhase)
        {
            return;
        }

        currentPhase = newPhase;
        isPhaseTransitionPending = true;

        movement.SetSpeedMultiplier(Phase.SpeedMultiplier);

        LogBoss($"Entered {currentPhase}.");
    }

    private void StartPhaseTransition()
    {
        isPhaseTransitionPending = false;

        if (audioSource != null && phaseTransitionSound != null)
        {
            audioSource.PlayOneShot(phaseTransitionSound);
        }

        StartAction(phaseTransition);
    }

    #endregion

    #region Block & Damage

    /// <summary>Wird von <see cref="BossDamageReceiver"/> bei jedem Treffer gefragt.</summary>
    public bool TryBlockAttack(Transform attacker)
    {
        if (isDead || !isCombatPrepared || !isEngaged || attacker == null)
        {
            return false;
        }

        if (currentAction != null && currentAction.IsInvulnerable)
        {
            return true;
        }

        // Von hinten / der Seite getroffen -> kein Block möglich.
        if (!IsInFrontArc(attacker))
        {
            LogBoss("Hit from behind – cannot block.");
            return false;
        }

        if (currentAction != null)
        {
            // Nur echtes Blocken hält, alles andere ist ein Angriffsfenster.
            return currentAction.BlocksFrontalDamage;
        }

        float blockChance = Mathf.Clamp01(Phase.BlockChance + blockPressure);
        bool blocked = Random.value <= blockChance;

        LogBoss($"Block attempt ({blockChance:P0}): {(blocked ? "BLOCKED" : "failed")}");

        if (blocked)
        {
            StartAction(block);
        }

        return blocked;
    }

    private bool IsInFrontArc(Transform attacker)
    {
        Vector3 direction = attacker.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        return Vector3.Angle(transform.forward, direction) <= blockAngle * 0.5f;
    }

    private void HandleDamaged(int damage)
    {
        blockPressure = Mathf.Min(
            maxBlockPressure,
            blockPressure + blockPressurePerHit
        );
    }

    private void HandleDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isEngaged = false;

        AbortCurrentAction();
        movement.Stop();
        movement.ResetRotationControl();

        _doorToDespawn?.SetActive(false);

        if (musicSource != null)
        {
            musicSource.Stop();
        }

        ReportQuestProgress();
    }

    private void ReportQuestProgress()
    {
        if (questReported)
        {
            return;
        }

        questReported = true;

        QuestManager.Report(QuestType.Kill, _questTargetId);
    }

    #endregion

    #region IBossActionContext

    public Transform Root => transform;

    public EnemyMovement Movement => movement;

    public Transform CurrentTarget => Target;

    public float ScaleDuration(float baseDuration)
    {
        return baseDuration / Phase.SpeedMultiplier;
    }

    public bool TryDealDamage(float range, int baseDamage)
    {
        int scaledDamage = Mathf.RoundToInt(baseDamage * Phase.DamageMultiplier);

        return TryHitTarget(range, scaledDamage);
    }

    public Vector3 GetDirectionToPredictedTarget(float secondsAhead)
    {
        return HorizontalDirectionTo(PredictTargetPosition(secondsAhead));
    }

    public void PlayAttackSound()
    {
        PlayRandomAttackSound();
    }

    #endregion

    private void LogBoss(string message)
    {
        if (debugBlock)
        {
            Debug.Log($"[BOSS] {message}", gameObject);
        }
    }
}
