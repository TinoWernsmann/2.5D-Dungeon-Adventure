using UnityEngine;

public class SwordCombatBehaviour : EnemyCombatBehaviour
{
    private enum CombatState
    {
        None,
        Approach,
        Windup,
        Attack,
        Recovery,
        Reposition
    }

    [Header("References")]
    [SerializeField] private EnemyMovement movement;

    [Header("Range")]
    [Min(0.1f)]
    [SerializeField] private float attackRange = 1.8f;

    [Min(0.1f)]
    [SerializeField] private float preferredDistance = 1.4f;

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

    [Header("Reposition")]
    [Min(0f)]
    [SerializeField] private float repositionDistance = 1.5f;

    [Min(0f)]
    [SerializeField] private float repositionDuration = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float repositionChance = 0.6f;

    private CombatState currentState = CombatState.None;

    private Transform currentTarget;
    private Health currentTargetHealth;

    private float stateTimer;
    private float attackTimer;

    private bool damageApplied;

    public override bool IsPerformingAttack =>
        currentState == CombatState.Windup ||
        currentState == CombatState.Attack;

    public override float AttackProgress
    {
        get
        {
            if (currentState == CombatState.Windup)
            {
                return 0f;
            }

            if (currentState != CombatState.Attack)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                attackTimer / attackDuration
            );
        }
    }

    private void Awake()
    {
        if (movement == null)
        {
            movement = GetComponent<EnemyMovement>();
        }
    }

    public override bool IsInCombatRange(
        Transform target)
    {
        if (target == null)
        {
            return false;
        }

        return GetHorizontalDistance(target)
               <= attackRange;
    }

    public override void EnterCombat(
        Transform target,
        Health targetHealth)
    {
        currentTarget = target;
        currentTargetHealth = targetHealth;

        ChangeState(CombatState.Approach);
    }

    public override void UpdateCombat(
        Transform target,
        Health targetHealth)
    {
        currentTarget = target;
        currentTargetHealth = targetHealth;

        if (!HasValidTarget())
        {
            return;
        }

        switch (currentState)
        {
            case CombatState.Approach:
                UpdateApproach();
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

    public override void ExitCombat()
    {
        movement.Stop();
        movement.ResetRotationControl();

        currentTarget = null;
        currentTargetHealth = null;

        stateTimer = 0f;
        attackTimer = 0f;

        damageApplied = false;

        currentState = CombatState.None;
    }

    private void UpdateApproach()
    {
        float distance =
            GetHorizontalDistance(currentTarget);

        if (distance <= preferredDistance)
        {
            movement.StopAndFace(currentTarget);

            ChangeState(CombatState.Windup);
            return;
        }

        movement.MoveTo(currentTarget.position);
    }

    private void UpdateWindup()
    {
        movement.StopAndFace(currentTarget);

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            ChangeState(CombatState.Attack);
        }
    }

    private void UpdateAttack()
    {
        movement.StopAndFace(currentTarget);

        attackTimer += Time.deltaTime;

        TryApplyDamage();

        if (attackTimer >= attackDuration)
        {
            ChangeState(CombatState.Recovery);
        }
    }

    private void UpdateRecovery()
    {
        movement.StopAndFace(currentTarget);

        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        DecideNextAction();
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

    private void DecideNextAction()
    {
        if (!IsInCombatRange(currentTarget))
        {
            ChangeState(CombatState.Approach);
            return;
        }

        if (Random.value <= repositionChance)
        {
            StartReposition();
            return;
        }

        ChangeState(CombatState.Windup);
    }

    private void StartReposition()
    {
        Vector3 directionAway =
            transform.position -
            currentTarget.position;

        directionAway.y = 0f;

        if (directionAway.sqrMagnitude <= 0.001f)
        {
            directionAway =
                -transform.forward;
        }

        directionAway.Normalize();

        Vector3 destination =
            transform.position +
            directionAway * repositionDistance;

        movement.MoveToWhileFacing(
            destination,
            currentTarget
        );

        ChangeState(CombatState.Reposition);
    }

    private void TryApplyDamage()
    {
        if (damageApplied)
        {
            return;
        }

        float damageTime =
            attackDuration * damageMoment;

        if (attackTimer < damageTime)
        {
            return;
        }

        damageApplied = true;

        if (!IsInCombatRange(currentTarget))
        {
            return;
        }

        currentTargetHealth.TakeDamage(damage);
    }

    private float GetHorizontalDistance(
        Transform target)
    {
        Vector3 enemyPosition =
            transform.position;

        Vector3 targetPosition =
            target.position;

        enemyPosition.y = 0f;
        targetPosition.y = 0f;

        return Vector3.Distance(
            enemyPosition,
            targetPosition
        );
    }

    private bool HasValidTarget()
    {
        return currentTarget != null &&
               currentTargetHealth != null &&
               !currentTargetHealth.IsDead;
    }

    private void ChangeState(
        CombatState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        switch (currentState)
        {
            case CombatState.Approach:
                stateTimer = 0f;
                break;

            case CombatState.Windup:
                stateTimer = windupDuration;
                break;

            case CombatState.Attack:
                attackTimer = 0f;
                damageApplied = false;
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