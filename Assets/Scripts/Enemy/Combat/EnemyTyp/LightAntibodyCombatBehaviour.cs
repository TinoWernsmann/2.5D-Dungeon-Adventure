using UnityEngine;

public class LightAntibodyCombatBehaviour : EnemyCombatBehaviour
{
    private enum CombatState
    {
        None,
        Approach,
        Windup,
        Lunge,
        Recovery
    }

    [Header("References")]
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private CharacterController characterController;

    [Header("Combat")]
    [Min(0.1f)]
    [SerializeField] private float combatRange = 2.5f;

    [Min(0.1f)]
    [SerializeField] private float lungeStartDistance = 2f;

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

    [Header("Recovery")]
    [Min(0f)]
    [SerializeField] private float recoveryDuration = 0.5f;

    private CombatState currentState = CombatState.None;

    private Transform currentTarget;
    private Health currentTargetHealth;

    private Vector3 lungeDirection;

    private float stateTimer;
    private float attackTimer;

    private bool damageApplied;

    public override bool IsPerformingAttack =>
        currentState == CombatState.Windup ||
        currentState == CombatState.Lunge;

    public override float AttackProgress
    {
        get
        {
            if (currentState == CombatState.Windup)
            {
                return 0f;
            }

            if (currentState != CombatState.Lunge)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                attackTimer / lungeDuration
            );
        }
    }

    private void Awake()
    {
        if (movement == null)
        {
            movement = GetComponent<EnemyMovement>();
        }

        if (characterController == null)
        {
            characterController =
                GetComponent<CharacterController>();
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
               <= combatRange;
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

            case CombatState.Lunge:
                UpdateLunge();
                break;

            case CombatState.Recovery:
                UpdateRecovery();
                break;
        }
    }

    public override void ExitCombat()
    {
        movement.Stop();

        currentTarget = null;
        currentTargetHealth = null;

        lungeDirection = Vector3.zero;

        stateTimer = 0f;
        attackTimer = 0f;

        damageApplied = false;

        currentState = CombatState.None;
    }

    private void UpdateApproach()
    {
        FaceTarget();

        float distance =
            GetHorizontalDistance(currentTarget);

        if (distance <= lungeStartDistance)
        {
            movement.Stop();

            ChangeState(CombatState.Windup);
            return;
        }

        movement.MoveTo(currentTarget.position);
    }

    private void UpdateWindup()
    {
        movement.Stop();

        FaceTarget();

        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        PrepareLunge();

        ChangeState(CombatState.Lunge);
    }

    private void PrepareLunge()
    {
        lungeDirection =
            currentTarget.position -
            transform.position;

        lungeDirection.y = 0f;

        if (lungeDirection.sqrMagnitude <= 0.001f)
        {
            lungeDirection = transform.forward;
        }

        lungeDirection.Normalize();

        transform.rotation =
            Quaternion.LookRotation(lungeDirection);
    }

    private void UpdateLunge()
    {
        attackTimer += Time.deltaTime;

        MoveLunge();
        TryApplyDamage();

        if (attackTimer >= lungeDuration)
        {
            ChangeState(CombatState.Recovery);
        }
    }

    private void MoveLunge()
    {
        Vector3 movementDelta =
            lungeDirection *
            lungeSpeed *
            Time.deltaTime;

        if (characterController != null &&
            characterController.enabled)
        {
            characterController.Move(movementDelta);
            return;
        }

        transform.position += movementDelta;
    }

    private void TryApplyDamage()
    {
        if (damageApplied)
        {
            return;
        }

        float distance =
            GetHorizontalDistance(currentTarget);

        if (distance > hitRange)
        {
            return;
        }

        damageApplied = true;

        currentTargetHealth.TakeDamage(damage);
    }

    private void UpdateRecovery()
    {
        movement.Stop();

        FaceTarget();

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            ChangeState(CombatState.Approach);
        }
    }

    private void FaceTarget()
    {
        if (currentTarget == null)
        {
            return;
        }

        Vector3 direction =
            currentTarget.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        transform.rotation =
            Quaternion.LookRotation(
                direction.normalized
            );
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

            case CombatState.Lunge:
                movement.Stop();

                attackTimer = 0f;
                damageApplied = false;
                break;

            case CombatState.Recovery:
                movement.Stop();

                stateTimer = recoveryDuration;
                break;
        }
    }
}