using UnityEngine;

/// <summary>
/// Nahkämpfer: Nähert sich, umkreist den Player solange kein Angriffs-Slot frei ist,
/// holt sichtbar aus (Windup), schlägt zu – manchmal als Kombo – und weicht danach aus.
/// </summary>
public class SwordCombatBehaviour : EnemyCombatBehaviour
{
    private enum CombatState
    {
        None,
        Approach,
        Strafe,
        Windup,
        Attack,
        Recovery,
        Reposition
    }

    [Header("Range")]
    [Min(0.1f)]
    [SerializeField] private float attackRange = 1.8f;

    [Min(0.1f)]
    [SerializeField] private float preferredDistance = 1.4f;

    [Tooltip("Ab dieser Distanz wechselt der Brain in den Kampfmodus.")]
    [Min(0.1f)]
    [SerializeField] private float engageRange = 4f;

    [Tooltip("Abstand, in dem der Gegner den Player umkreist, solange er nicht angreifen darf.")]
    [Min(0.1f)]
    [SerializeField] private float strafeRadius = 3f;

    [Header("Attack")]
    [Min(1)]
    [SerializeField] private int damage = 20;

    [Min(0f)]
    [SerializeField] private float windupDuration = 0.3f;

    [Min(0.01f)]
    [SerializeField] private float attackDuration = 0.4f;

    [Range(0f, 1f)]
    [SerializeField] private float damageMoment = 0.5f;

    [Min(0f)]
    [SerializeField] private float recoveryDuration = 0.6f;

    [Header("Combo")]
    [Range(0f, 1f)]
    [SerializeField] private float comboChance = 0.35f;

    [Min(1)]
    [SerializeField] private int maxComboHits = 2;

    [Range(0.1f, 1f)]
    [Tooltip("Folgeschläge einer Kombo holen schneller aus.")]
    [SerializeField] private float comboWindupFactor = 0.6f;

    [Header("Reposition")]
    [Min(0f)]
    [SerializeField] private float repositionDistance = 1.5f;

    [Min(0f)]
    [SerializeField] private float repositionDuration = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float repositionChance = 0.6f;

    private CombatState currentState = CombatState.None;

    private float stateTimer;
    private float attackTimer;
    private int comboHitCount;
    private bool damageApplied;

    public override bool IsPerformingAttack =>
        currentState == CombatState.Windup ||
        currentState == CombatState.Attack;

    public override float AttackProgress =>
        currentState == CombatState.Attack
            ? Mathf.Clamp01(attackTimer / attackDuration)
            : 0f;

    public override bool IsInCombatRange(Transform target)
    {
        return GetHorizontalDistance(target) <= engageRange;
    }

    protected override void OnCombatEntered()
    {
        ChangeState(CombatState.Approach);
    }

    protected override void OnCombatUpdate()
    {
        switch (currentState)
        {
            case CombatState.None:
            case CombatState.Approach:
                UpdateApproach();
                break;

            case CombatState.Strafe:
                UpdateStrafe();
                break;

            case CombatState.Windup:
                UpdateWindup();
                break;

            case CombatState.Attack:
                UpdateAttack();
                break;

            case CombatState.Recovery:
                UpdateRecovery();
                break;

            case CombatState.Reposition:
                UpdateReposition();
                break;
        }
    }

    protected override void OnCombatExited()
    {
        movement.Stop();
        movement.ResetRotationControl();

        currentState = CombatState.None;
    }

    private void UpdateApproach()
    {
        float distance = DistanceToTarget();

        if (distance <= strafeRadius &&
            !TryAcquireAttackSlot())
        {
            ChangeState(CombatState.Strafe);
            return;
        }

        if (distance <= preferredDistance)
        {
            comboHitCount = 0;
            ChangeState(CombatState.Windup);
            return;
        }

        movement.MoveTo(Target.position);
    }

    private void UpdateStrafe()
    {
        if (TryAcquireAttackSlot())
        {
            ChangeState(CombatState.Approach);
            return;
        }

        StrafeAroundTarget(strafeRadius);
    }

    private void UpdateWindup()
    {
        movement.StopAndFace(Target);

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            ChangeState(CombatState.Attack);
        }
    }

    private void UpdateAttack()
    {
        movement.StopAndFace(Target);

        attackTimer += Time.deltaTime;

        if (!damageApplied &&
            attackTimer >= attackDuration * damageMoment)
        {
            damageApplied = true;
            TryHitTarget(attackRange, damage);
        }

        if (attackTimer < attackDuration)
        {
            return;
        }

        if (ShouldContinueCombo())
        {
            ChangeState(CombatState.Windup);
            stateTimer *= comboWindupFactor;
            return;
        }

        ChangeState(CombatState.Recovery);
    }

    private bool ShouldContinueCombo()
    {
        return comboHitCount < maxComboHits &&
               DistanceToTarget() <= attackRange &&
               Random.value <= comboChance;
    }

    private void UpdateRecovery()
    {
        movement.StopAndFace(Target);

        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        // Anderen Gegnern eine Chance geben.
        ReleaseAttackSlot();

        if (Random.value <= repositionChance)
        {
            StartReposition();
            return;
        }

        ChangeState(CombatState.Approach);
    }

    private void StartReposition()
    {
        // Schräg nach hinten ausweichen statt stur rückwärts.
        float sideAngle = Random.Range(-60f, 60f);

        movement.MoveToWhileFacing(
            GetRetreatPoint(repositionDistance, sideAngle),
            Target
        );

        ChangeState(CombatState.Reposition);
    }

    private void UpdateReposition()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f &&
            !movement.HasReachedDestination)
        {
            return;
        }

        ChangeState(CombatState.Approach);
    }

    private void ChangeState(CombatState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case CombatState.Windup:
                stateTimer = windupDuration;
                break;

            case CombatState.Attack:
                attackTimer = 0f;
                damageApplied = false;
                comboHitCount++;

                PlayRandomAttackSound();
                break;

            case CombatState.Recovery:
                stateTimer = recoveryDuration;
                break;

            case CombatState.Reposition:
                stateTimer = repositionDuration;
                break;
        }
    }
}
