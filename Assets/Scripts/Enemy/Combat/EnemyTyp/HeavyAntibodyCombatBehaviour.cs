using UnityEngine;

/// <summary>
/// Schwerer Ansturm-Gegner: Kündigt den Ansturm deutlich an und rennt dann in einer
/// FESTEN Richtung los – der Player kann seitlich ausweichen.
/// Prallt er gegen eine Wand, ist er lange betäubt (Bestrafungsfenster für den Player).
/// </summary>
public class HeavyAntibodyCombatBehaviour : EnemyCombatBehaviour
{
    private enum CombatState
    {
        None,
        Approach,
        Telegraph,
        Charge,
        Recovery
    }

    [Header("Combat Range")]
    [Min(0.1f)]
    [SerializeField] private float combatRange = 6f;

    [Min(0.1f)]
    [SerializeField] private float preferredChargeDistance = 4f;

    [Header("Telegraph")]
    [Min(0.1f)]
    [SerializeField] private float telegraphDuration = 0.8f;

    [Tooltip("In den letzten Sekunden des Telegraphs dreht er sich nicht mehr – " +
             "der Player kann die Richtung lesen und ausweichen.")]
    [Min(0f)]
    [SerializeField] private float aimLockDuration = 0.25f;

    [Header("Charge")]
    [Min(0.1f)]
    [SerializeField] private float chargeSpeed = 10f;

    [Min(0.1f)]
    [SerializeField] private float chargeDuration = 0.7f;

    [Min(0.1f)]
    [SerializeField] private float hitRange = 1.2f;

    [Min(1)]
    [SerializeField] private int chargeDamage = 30;

    [Header("Recovery")]
    [Min(0f)]
    [SerializeField] private float recoveryDuration = 1.5f;

    [Tooltip("Betäubungsdauer, wenn der Ansturm gegen eine Wand prallt.")]
    [Min(0f)]
    [SerializeField] private float wallStunDuration = 2.5f;

    private CombatState currentState = CombatState.None;

    private Vector3 chargeDirection;

    private float stateTimer;
    private float attackTimer;
    private bool damageApplied;

    public bool IsStunned { get; private set; }

    // Betäubt (nach Wandaufprall) bleibt er stehen – das ist das Angriffsfenster des Players.
    protected override bool CanEscapeFromCorner =>
        base.CanEscapeFromCorner && !IsStunned;

    public override bool IsPerformingAttack =>
        currentState == CombatState.Telegraph ||
        currentState == CombatState.Charge;

    public override float AttackProgress =>
        currentState == CombatState.Charge
            ? Mathf.Clamp01(attackTimer / chargeDuration)
            : 0f;

    public override bool IsInCombatRange(Transform target)
    {
        return GetHorizontalDistance(target) <= combatRange;
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

            case CombatState.Telegraph:
                UpdateTelegraph();
                break;

            case CombatState.Charge:
                UpdateCharge();
                break;

            case CombatState.Recovery:
                UpdateRecovery();
                break;
        }
    }

    protected override void OnCombatExited()
    {
        movement.Stop();
        movement.ResetRotationControl();

        IsStunned = false;
        currentState = CombatState.None;
    }

    private void UpdateApproach()
    {
        if (DistanceToTarget() > preferredChargeDistance)
        {
            movement.MoveTo(Target.position);
            return;
        }

        if (TryAcquireAttackSlot())
        {
            ChangeState(CombatState.Telegraph);
            return;
        }

        StrafeAroundTarget(preferredChargeDistance);
    }

    private void UpdateTelegraph()
    {
        stateTimer -= Time.deltaTime;

        bool isAimLocked = stateTimer <= aimLockDuration;

        if (isAimLocked)
        {
            movement.Stop();
            movement.ResetRotationControl();
        }
        else
        {
            movement.StopAndFace(Target);
        }

        if (stateTimer > 0f)
        {
            return;
        }

        chargeDirection = transform.forward;
        ChangeState(CombatState.Charge);
    }

    private void UpdateCharge()
    {
        attackTimer += Time.deltaTime;

        bool movedFreely =
            movement.Dash(chargeDirection * chargeSpeed * Time.deltaTime);

        if (!damageApplied)
        {
            damageApplied = TryHitTarget(hitRange, chargeDamage);
        }

        if (!movedFreely)
        {
            StartRecovery(hitWall: true);
            return;
        }

        if (attackTimer >= chargeDuration)
        {
            StartRecovery(hitWall: false);
        }
    }

    private void StartRecovery(bool hitWall)
    {
        ChangeState(CombatState.Recovery);

        IsStunned = hitWall;
        stateTimer = hitWall ? wallStunDuration : recoveryDuration;
    }

    private void UpdateRecovery()
    {
        movement.Stop();

        if (!IsStunned)
        {
            movement.FaceTarget(Target);
        }

        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        IsStunned = false;

        ReleaseAttackSlot();
        ChangeState(CombatState.Approach);
    }

    private void ChangeState(CombatState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case CombatState.Telegraph:
                stateTimer = telegraphDuration;
                break;

            case CombatState.Charge:
                movement.Stop();
                movement.LookInDirection(chargeDirection);

                attackTimer = 0f;
                damageApplied = false;

                PlayRandomAttackSound();
                break;

            case CombatState.Recovery:
                movement.Stop();
                break;
        }
    }
}
