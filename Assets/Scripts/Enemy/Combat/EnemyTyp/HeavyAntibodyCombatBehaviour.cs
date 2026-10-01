using UnityEngine;

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

    [Header("References")]
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private CharacterController characterController;

    [Header("Combat Range")]
    [Min(0.1f)]
    [SerializeField] private float combatRange = 6f;

    [Min(0.1f)]
    [SerializeField] private float preferredChargeDistance = 4f;

    [Header("Telegraph")]
    [Min(0.1f)]
    [SerializeField] private float telegraphDuration = 0.8f;

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

    private CombatState currentState;

    private Transform currentTarget;
    private Health currentTargetHealth;

    private Vector3 chargeDirection;

    private float stateTimer;
    private float attackTimer;

    private bool damageApplied;

    public override bool IsPerformingAttack =>
        currentState == CombatState.Telegraph ||
        currentState == CombatState.Charge;

    public override float AttackProgress
    {
        get
        {
            if (currentState == CombatState.Telegraph)
            {
                return 0f;
            }

            if (currentState != CombatState.Charge)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                attackTimer / chargeDuration
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

    public override void ExitCombat()
    {
        movement.Stop();

        currentTarget = null;
        currentTargetHealth = null;

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

        if (distance <= preferredChargeDistance)
        {
            movement.Stop();
            ChangeState(CombatState.Telegraph);
            return;
        }

        movement.MoveTo(currentTarget.position);
    }

    private void UpdateTelegraph()
    {
        movement.Stop();

        FaceTarget();

        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        PrepareCharge();

        ChangeState(CombatState.Charge);
    }

    private void PrepareCharge()
    {
        chargeDirection =
            currentTarget.position -
            transform.position;

        chargeDirection.y = 0f;

        if (chargeDirection.sqrMagnitude <= 0.001f)
        {
            chargeDirection = transform.forward;
        }

        chargeDirection.Normalize();

        transform.rotation =
            Quaternion.LookRotation(chargeDirection);
    }

    private void UpdateCharge()
    {
        attackTimer += Time.deltaTime;

        MoveCharge();

        TryApplyChargeDamage();

        if (attackTimer >= chargeDuration)
        {
            ChangeState(CombatState.Recovery);
        }
    }

    private void MoveCharge()
    {
        Vector3 movementDelta =
            chargeDirection *
            chargeSpeed *
            Time.deltaTime;

        if (characterController != null &&
            characterController.enabled)
        {
            characterController.Move(movementDelta);
            return;
        }

        transform.position += movementDelta;
    }

    private void TryApplyChargeDamage()
    {
        if (damageApplied)
        {
            return;
        }

        if (GetHorizontalDistance(currentTarget)
            > hitRange)
        {
            return;
        }

        damageApplied = true;

        currentTargetHealth.TakeDamage(
            chargeDamage
        );
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

            case CombatState.Telegraph:
                stateTimer = telegraphDuration;
                break;

            case CombatState.Charge:
                attackTimer = 0f;
                damageApplied = false;
                movement.Stop();
                break;

            case CombatState.Recovery:
                stateTimer = recoveryDuration;
                movement.Stop();
                break;
        }
    }
}