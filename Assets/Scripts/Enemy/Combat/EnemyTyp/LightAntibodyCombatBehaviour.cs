using UnityEngine;

/// <summary>
/// Schneller Hit-and-Run Gegner: Umkreist den Player, springt mit einem Ausfallschritt
/// auf die VORAUSBERECHNETE Position des Players und zieht sich danach sofort zurück.
/// </summary>
public class LightAntibodyCombatBehaviour : EnemyCombatBehaviour
{
    private enum CombatState
    {
        None,
        Approach,
        Strafe,
        Windup,
        Lunge,
        Retreat
    }

    [Header("Combat")]
    [Min(0.1f)]
    [SerializeField] private float combatRange = 2.5f;

    [Min(0.1f)]
    [SerializeField] private float lungeStartDistance = 2f;

    [Min(0.1f)]
    [SerializeField] private float strafeRadius = 3.5f;

    [Header("Windup")]
    [Min(0f)]
    [SerializeField] private float windupDuration = 0.25f;

    [Header("Lunge")]
    [Min(0.1f)]
    [SerializeField] private float lungeSpeed = 7f;

    [Min(0.01f)]
    [SerializeField] private float lungeDuration = 0.3f;

    [Min(0.1f)]
    [SerializeField] private float hitRange = 1.1f;

    [Min(1)]
    [SerializeField] private int damage = 15;

    [Tooltip("Wie weit (in Sekunden) die Bewegung des Players vorausberechnet wird.")]
    [Min(0f)]
    [SerializeField] private float aimLeadTime = 0.25f;

    [Header("Recovery")]
    [Min(0f)]
    [SerializeField] private float recoveryDuration = 0.5f;

    [Min(0f)]
    [SerializeField] private float retreatDistance = 2f;

    private CombatState currentState = CombatState.None;

    private Vector3 lungeDirection;

    private float stateTimer;
    private float attackTimer;
    private bool damageApplied;

    public override bool IsPerformingAttack =>
        currentState == CombatState.Windup ||
        currentState == CombatState.Lunge;

    public override float AttackProgress =>
        currentState == CombatState.Lunge
            ? Mathf.Clamp01(attackTimer / lungeDuration)
            : 0f;

    public override bool IsInCombatRange(Transform target)
    {
        return GetHorizontalDistance(target) <=
               Mathf.Max(combatRange, strafeRadius);
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

            case CombatState.Lunge:
                UpdateLunge();
                break;

            case CombatState.Retreat:
                UpdateRetreat();
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

        if (distance <= lungeStartDistance)
        {
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

        StrafeAroundTarget(strafeRadius, stepDegrees: 50f);
    }

    private void UpdateWindup()
    {
        movement.StopAndFace(Target);

        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        lungeDirection =
            HorizontalDirectionTo(PredictTargetPosition(aimLeadTime));

        movement.LookInDirection(lungeDirection);

        ChangeState(CombatState.Lunge);
    }

    private void UpdateLunge()
    {
        attackTimer += Time.deltaTime;

        movement.Dash(lungeDirection * lungeSpeed * Time.deltaTime);

        if (!damageApplied)
        {
            damageApplied = TryHitTarget(hitRange, damage);
        }

        if (attackTimer >= lungeDuration)
        {
            ChangeState(CombatState.Retreat);
        }
    }

    private void UpdateRetreat()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        ReleaseAttackSlot();
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

            case CombatState.Lunge:
                movement.Stop();

                attackTimer = 0f;
                damageApplied = false;

                PlayRandomAttackSound();
                break;

            case CombatState.Retreat:
                stateTimer = recoveryDuration;

                // Hit-and-Run: nach dem Angriff schräg zurückziehen.
                movement.MoveToWhileFacing(
                    GetRetreatPoint(retreatDistance, Random.Range(-45f, 45f)),
                    Target
                );
                break;
        }
    }
}
