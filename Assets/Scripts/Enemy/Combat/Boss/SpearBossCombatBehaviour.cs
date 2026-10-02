using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpearBossCombatBehaviour : EnemyCombatBehaviour
{
    private enum CombatState
    {
        None,
        PrepareCombat,
        Approach,
        Attack,
        Recovery,
        DodgeLeft,
        DodgeRight,
        DodgeFollowUp,
        Backstep,
        Block,
        BlockCounter,
        Dead
    }

    private enum BossPhase
    {
        PhaseOne,
        PhaseTwo,
        PhaseThree
    }

    [Header("References")]
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private Health health;
    [Min(2)]
    [SerializeField] private float _bossDeathWaitTime;

    [Header("Combat Range")]
    [Min(0.1f)]
    [SerializeField] private float combatRange = 8f;

    [Min(0.1f)]
    [SerializeField] private float attackRange = 3f;

    [Min(0.1f)]
    [SerializeField] private float preferredDistance = 2.3f;

    [Header("Prepare Combat")]
    [Min(0.01f)]
    [SerializeField] private float prepareCombatDuration = 0.9f;

    [Header("Base Attack")]
    [Min(1)]
    [SerializeField] private int attackDamage = 30;

    [Min(0.01f)]
    [SerializeField] private float attackDuration = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float damageMoment = 0.55f;

    [Min(0f)]
    [SerializeField] private float recoveryDuration = 0.55f;

    [Header("Dodge")]
    [Min(0.1f)]
    [SerializeField] private float dodgeDistance = 2.5f;

    [Min(0.01f)]
    [SerializeField] private float dodgeDuration = 0.45f;

    [Min(0.1f)]
    [SerializeField] private float dodgeAttackRange = 3.5f;

    [Min(1)]
    [SerializeField] private int dodgeAttackDamage = 35;

    [Min(0.01f)]
    [SerializeField] private float dodgeAttackDuration = 0.55f;

    [Header("Block")]
    [Range(0f, 180f)]
    [SerializeField] private float blockAngle = 140f;

    [Min(0.01f)]
    [SerializeField] private float blockDuration = 0.6f;

    [Min(1)]
    [SerializeField] private int blockCounterDamage = 40;

    [Min(0.1f)]
    [SerializeField] private float blockCounterRange = 3.2f;

    [Min(0.01f)]
    [SerializeField] private float blockCounterDuration = 0.5f;

    [Header("Block Debug")]
    [SerializeField] private bool debugBlock = true;

    [Header("Backstep")]
    [Min(0.1f)]
    [SerializeField] private float backstepDistance = 2f;

    [Min(0.01f)]
    [SerializeField] private float backstepDuration = 0.5f;

    [Header("Phase Thresholds")]
    [Range(0.01f, 0.99f)]
    [SerializeField] private float phaseTwoHealthPercent = 0.65f;

    [Range(0.01f, 0.99f)]
    [SerializeField] private float phaseThreeHealthPercent = 0.30f;

    [Header("Phase 1 - Defensive")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseOneBlockChance = 0.45f;

    [Range(0f, 1f)]
    [SerializeField] private float phaseOneDodgeChance = 0.25f;

    [Range(0f, 1f)]
    [SerializeField] private float phaseOneBackstepChance = 0.45f;

    [Header("Phase 2 - Mobile")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseTwoBlockChance = 0.30f;

    [Range(0f, 1f)]
    [SerializeField] private float phaseTwoDodgeChance = 0.45f;

    [Range(0f, 1f)]
    [SerializeField] private float phaseTwoBackstepChance = 0.25f;

    [Min(1f)]
    [SerializeField] private float phaseTwoSpeedMultiplier = 1.15f;

    [Min(1f)]
    [SerializeField] private float phaseTwoDamageMultiplier = 1.10f;

    [Header("Phase 3 - Aggressive")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseThreeBlockChance = 0.15f;

    [Range(0f, 1f)]
    [SerializeField] private float phaseThreeDodgeChance = 0.60f;

    [Range(0f, 1f)]
    [SerializeField] private float phaseThreeBackstepChance = 0.10f;

    [Min(1f)]
    [SerializeField] private float phaseThreeSpeedMultiplier = 1.30f;

    [Min(1f)]
    [SerializeField] private float phaseThreeDamageMultiplier = 1.20f;

    private CombatState currentState = CombatState.None;
    private BossPhase currentPhase = BossPhase.PhaseOne;

    private Transform currentTarget;
    private Health currentTargetHealth;

    private float stateTimer;
    private float attackTimer;

    private bool damageApplied;
    private bool dodgeToLeft;
    private bool combatPrepared;
    private bool combatAwarenessActive;

    private BossVisualState visualState =
        BossVisualState.Stand;

    public BossVisualState VisualState => visualState;

    public bool IsCombatPrepared => combatPrepared;

    public bool IsCombatAwarenessActive =>
        combatAwarenessActive;

    public override bool IsPerformingAttack =>
        currentState == CombatState.Attack ||
        currentState == CombatState.DodgeFollowUp ||
        currentState == CombatState.BlockCounter;

    public override float AttackProgress
    {
        get
        {
            float duration;

            switch (currentState)
            {
                case CombatState.Attack:
                    duration =
                        GetAdjustedDuration(attackDuration);
                    break;

                case CombatState.DodgeFollowUp:
                    duration =
                        GetAdjustedDuration(dodgeAttackDuration);
                    break;

                case CombatState.BlockCounter:
                    duration =
                        GetAdjustedDuration(blockCounterDuration);
                    break;

                default:
                    return 0f;
            }

            if (duration <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                attackTimer / duration
            );
        }
    }

    private void Awake()
    {
        if (movement == null)
        {
            movement = GetComponent<EnemyMovement>();
        }

        if (health == null)
        {
            health = GetComponent<Health>();
        }
    }

    private void OnEnable()
    {
        health.Died += Die;
    }

    private void OnDisable()
    {
        health.Died -= Die;
    }

    private void Die()
    {
        StartCoroutine(WaitTillSceneLoad());
    }

    private IEnumerator WaitTillSceneLoad()
    {
        yield return new WaitForSeconds(_bossDeathWaitTime);

        SceneManager.LoadScene("TinoMainMenu");
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

    public void NotifyPlayerDetected(
        Transform target,
        Health targetHealth)
    {
        if (currentState == CombatState.Dead)
        {
            return;
        }

        combatAwarenessActive = true;

        currentTarget = target;
        currentTargetHealth = targetHealth;

        if (combatPrepared)
        {
            return;
        }

        if (currentState ==
            CombatState.PrepareCombat)
        {
            return;
        }

        ChangeState(
            CombatState.PrepareCombat
        );
    }

    public void NotifyPlayerLost()
    {
        if (currentState == CombatState.Dead)
        {
            return;
        }

        combatAwarenessActive = false;

        currentTarget = null;
        currentTargetHealth = null;

        attackTimer = 0f;
        stateTimer = 0f;
        damageApplied = false;

        currentState = CombatState.None;
        visualState = BossVisualState.Stand;

        if (movement != null)
        {
            movement.ResetRotationControl();
        }
    }

    public override void EnterCombat(
        Transform target,
        Health targetHealth)
    {
        if (currentState == CombatState.Dead)
        {
            return;
        }

        combatAwarenessActive = true;

        currentTarget = target;
        currentTargetHealth = targetHealth;

        UpdateBossPhase();

        if (!combatPrepared)
        {
            if (currentState !=
                CombatState.PrepareCombat)
            {
                ChangeState(
                    CombatState.PrepareCombat
                );
            }

            return;
        }

        ChangeState(
            CombatState.Approach
        );
    }

    public override void UpdateCombat(
        Transform target,
        Health targetHealth)
    {
        if (currentState == CombatState.Dead)
        {
            return;
        }

        combatAwarenessActive = true;

        currentTarget = target;
        currentTargetHealth = targetHealth;

        if (!HasValidTarget())
        {
            return;
        }

        UpdateBossPhase();

        switch (currentState)
        {
            case CombatState.PrepareCombat:
                UpdatePrepareCombat();
                break;

            case CombatState.None:
                ChangeState(
                    combatPrepared
                        ? CombatState.Approach
                        : CombatState.PrepareCombat
                );
                break;

            case CombatState.Approach:
                UpdateApproach();
                break;

            case CombatState.Attack:
                UpdateAttack();
                break;

            case CombatState.Recovery:
                UpdateRecovery();
                break;

            case CombatState.DodgeLeft:
            case CombatState.DodgeRight:
                UpdateDodge();
                break;

            case CombatState.DodgeFollowUp:
                UpdateDodgeFollowUp();
                break;

            case CombatState.Backstep:
                UpdateBackstep();
                break;

            case CombatState.Block:
                UpdateBlock();
                break;

            case CombatState.BlockCounter:
                UpdateBlockCounter();
                break;
        }
    }

    public override void ExitCombat()
    {
        if (currentState == CombatState.Dead)
        {
            return;
        }

        attackTimer = 0f;
        stateTimer = 0f;
        damageApplied = false;

        currentState = CombatState.None;

        visualState =
            combatAwarenessActive && combatPrepared
                ? BossVisualState.CombatIdle
                : BossVisualState.Stand;
    }

    public bool TryBlockAttack(
        Transform attacker)
    {
        if (!CanAttemptBlock(attacker))
        {
            if (debugBlock)
            {
                Debug.Log(
                    $"[BOSS BLOCK] Cannot block. " +
                    $"State: {currentState} | " +
                    $"Prepared: {combatPrepared} | " +
                    $"Aware: {combatAwarenessActive}",
                    gameObject
                );
            }

            return false;
        }

        float blockChance =
            GetBlockChance();

        float roll = Random.value;

        if (debugBlock)
        {
            Debug.Log(
                $"[BOSS BLOCK] Attempt | " +
                $"Phase: {currentPhase} | " +
                $"Chance: {blockChance:P0} | " +
                $"Roll: {roll:F2}",
                gameObject
            );
        }

        if (roll > blockChance)
        {
            if (debugBlock)
            {
                Debug.Log(
                    $"[BOSS BLOCK] FAILED | " +
                    $"Damage will be received.",
                    gameObject
                );
            }

            return false;
        }

        Debug.Log(
            $"<color=cyan><b>[BOSS BLOCK]</b></color> " +
            $"{gameObject.name} BLOCKED THE ATTACK!",
            gameObject
        );

        StartBlock(attacker);

        return true;
    }

    public void EnterDeathState()
    {
        if (currentState == CombatState.Dead)
        {
            return;
        }

        if (movement != null)
        {
            movement.Stop();
            movement.ResetRotationControl();
        }

        combatAwarenessActive = false;

        currentState = CombatState.Dead;
        visualState = BossVisualState.Defeated;

        stateTimer = 0f;
        attackTimer = 0f;
        damageApplied = false;

        currentTarget = null;
        currentTargetHealth = null;
    }

    private void UpdateBossPhase()
    {
        if (health == null ||
            health.MaxHealth <= 0)
        {
            return;
        }

        float healthPercent =
            (float)health.CurrentHealth /
            health.MaxHealth;

        BossPhase newPhase;

        if (healthPercent <=
            phaseThreeHealthPercent)
        {
            newPhase =
                BossPhase.PhaseThree;
        }
        else if (healthPercent <=
                 phaseTwoHealthPercent)
        {
            newPhase =
                BossPhase.PhaseTwo;
        }
        else
        {
            newPhase =
                BossPhase.PhaseOne;
        }

        if (newPhase == currentPhase)
        {
            return;
        }

        currentPhase = newPhase;

        Debug.Log(
            $"[BOSS] Entered {currentPhase}.",
            gameObject
        );
    }

    private void UpdatePrepareCombat()
    {
        if (currentTarget != null)
        {
            movement.StopAndFace(
                currentTarget
            );
        }

        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        combatPrepared = true;

        if (debugBlock)
        {
            Debug.Log(
                "[BOSS] Combat stance prepared.",
                gameObject
            );
        }

        if (currentTarget != null)
        {
            ChangeState(
                CombatState.Approach
            );
        }
        else
        {
            currentState =
                CombatState.None;

            visualState =
                BossVisualState.CombatIdle;
        }
    }

    private void UpdateApproach()
    {
        float distance =
            GetHorizontalDistance(
                currentTarget
            );

        if (distance <= preferredDistance)
        {
            movement.StopAndFace(
                currentTarget
            );

            visualState =
                BossVisualState.CombatIdle;

            DecideCombatAction();
            return;
        }

        visualState =
            BossVisualState.CombatIdle;

        movement.MoveToWhileFacing(
            currentTarget.position,
            currentTarget
        );
    }

    private void DecideCombatAction()
    {
        float distance =
            GetHorizontalDistance(
                currentTarget
            );

        if (distance > attackRange)
        {
            ChangeState(
                CombatState.Approach
            );

            return;
        }

        if (Random.value <=
            GetDodgeChance())
        {
            StartRandomDodge();
            return;
        }

        StartNormalAttack();
    }

    private void StartNormalAttack()
    {
        bool attackLeft =
            Random.value < 0.5f;

        visualState = attackLeft
            ? BossVisualState.AttackLeft
            : BossVisualState.AttackRight;

        ChangeState(
            CombatState.Attack
        );
    }

    private void UpdateAttack()
    {
        movement.StopAndFace(
            currentTarget
        );

        attackTimer += Time.deltaTime;

        float duration =
            GetAdjustedDuration(
                attackDuration
            );

        TryApplyDamage(
            attackRange,
            GetAdjustedDamage(
                attackDamage
            ),
            duration
        );

        if (attackTimer >= duration)
        {
            ChangeState(
                CombatState.Recovery
            );
        }
    }

    private void StartRandomDodge()
    {
        StartDodge(
            Random.value < 0.5f
        );
    }

    private void StartDodge(
        bool toLeft)
    {
        dodgeToLeft = toLeft;

        Vector3 sideDirection =
            toLeft
                ? -transform.right
                : transform.right;

        Vector3 destination =
            transform.position +
            sideDirection * dodgeDistance;

        visualState = toLeft
            ? BossVisualState.DodgeLeft
            : BossVisualState.DodgeRight;

        movement.MoveToWhileFacing(
            destination,
            currentTarget
        );

        ChangeState(
            toLeft
                ? CombatState.DodgeLeft
                : CombatState.DodgeRight
        );
    }

    private void UpdateDodge()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f &&
            !movement.HasReachedDestination)
        {
            return;
        }

        StartDodgeFollowUp();
    }

    private void StartDodgeFollowUp()
    {
        movement.StopAndFace(
            currentTarget
        );

        visualState = dodgeToLeft
            ? BossVisualState.AttackLeft
            : BossVisualState.AttackRight;

        ChangeState(
            CombatState.DodgeFollowUp
        );
    }

    private void UpdateDodgeFollowUp()
    {
        movement.StopAndFace(
            currentTarget
        );

        attackTimer += Time.deltaTime;

        float duration =
            GetAdjustedDuration(
                dodgeAttackDuration
            );

        TryApplyDamage(
            dodgeAttackRange,
            GetAdjustedDamage(
                dodgeAttackDamage
            ),
            duration
        );

        if (attackTimer >= duration)
        {
            ChangeState(
                CombatState.Recovery
            );
        }
    }

    private bool CanAttemptBlock(
        Transform attacker)
    {
        if (!combatPrepared ||
            !combatAwarenessActive ||
            attacker == null)
        {
            return false;
        }

        if (currentState == CombatState.Dead ||
            currentState == CombatState.Block ||
            currentState == CombatState.BlockCounter ||
            currentState == CombatState.Attack ||
            currentState == CombatState.DodgeLeft ||
            currentState == CombatState.DodgeRight ||
            currentState == CombatState.DodgeFollowUp)
        {
            return false;
        }

        return IsAttackerInBlockArc(
            attacker
        );
    }

    private bool IsAttackerInBlockArc(
        Transform attacker)
    {
        Vector3 direction =
            attacker.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        float angle =
            Vector3.Angle(
                transform.forward,
                direction.normalized
            );

        return angle <=
               blockAngle * 0.5f;
    }

    private void StartBlock(
        Transform attacker)
    {
        currentTarget = attacker;

        Health attackerHealth =
            attacker.GetComponent<Health>();

        if (attackerHealth == null)
        {
            attackerHealth =
                attacker.GetComponentInParent<Health>();
        }

        if (attackerHealth != null)
        {
            currentTargetHealth =
                attackerHealth;
        }

        movement.StopAndFace(attacker);

        ChangeState(
            CombatState.Block
        );
    }

    private void UpdateBlock()
    {
        movement.StopAndFace(
            currentTarget
        );

        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        StartBlockCounter();
    }

    private void StartBlockCounter()
    {
        bool attackLeft =
            Random.value < 0.5f;

        visualState = attackLeft
            ? BossVisualState.AttackLeft
            : BossVisualState.AttackRight;

        ChangeState(
            CombatState.BlockCounter
        );
    }

    private void UpdateBlockCounter()
    {
        movement.StopAndFace(
            currentTarget
        );

        attackTimer += Time.deltaTime;

        float duration =
            GetAdjustedDuration(
                blockCounterDuration
            );

        TryApplyDamage(
            blockCounterRange,
            GetAdjustedDamage(
                blockCounterDamage
            ),
            duration
        );

        if (attackTimer >= duration)
        {
            ChangeState(
                CombatState.Recovery
            );
        }
    }

    private void UpdateRecovery()
    {
        movement.StopAndFace(
            currentTarget
        );

        visualState =
            BossVisualState.CombatIdle;

        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        if (Random.value <=
            GetBackstepChance())
        {
            StartBackstep();
            return;
        }

        ChangeState(
            CombatState.Approach
        );
    }

    private void StartBackstep()
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
            directionAway *
            backstepDistance;

        visualState =
            BossVisualState.Back;

        movement.MoveToWhileFacing(
            destination,
            currentTarget
        );

        ChangeState(
            CombatState.Backstep
        );
    }

    private void UpdateBackstep()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f &&
            !movement.HasReachedDestination)
        {
            return;
        }

        ChangeState(
            CombatState.Approach
        );
    }

    private void TryApplyDamage(
        float range,
        int damage,
        float duration)
    {
        if (damageApplied)
        {
            return;
        }

        float damageTime =
            duration * damageMoment;

        if (attackTimer < damageTime)
        {
            return;
        }

        damageApplied = true;

        if (!HasValidTarget())
        {
            return;
        }

        if (GetHorizontalDistance(
                currentTarget) > range)
        {
            return;
        }

        currentTargetHealth.TakeDamage(
            damage
        );
    }

    private float GetBlockChance()
    {
        return currentPhase switch
        {
            BossPhase.PhaseOne =>
                phaseOneBlockChance,

            BossPhase.PhaseTwo =>
                phaseTwoBlockChance,

            BossPhase.PhaseThree =>
                phaseThreeBlockChance,

            _ => phaseOneBlockChance
        };
    }

    private float GetDodgeChance()
    {
        return currentPhase switch
        {
            BossPhase.PhaseOne =>
                phaseOneDodgeChance,

            BossPhase.PhaseTwo =>
                phaseTwoDodgeChance,

            BossPhase.PhaseThree =>
                phaseThreeDodgeChance,

            _ => phaseOneDodgeChance
        };
    }

    private float GetBackstepChance()
    {
        return currentPhase switch
        {
            BossPhase.PhaseOne =>
                phaseOneBackstepChance,

            BossPhase.PhaseTwo =>
                phaseTwoBackstepChance,

            BossPhase.PhaseThree =>
                phaseThreeBackstepChance,

            _ => phaseOneBackstepChance
        };
    }

    private float GetSpeedMultiplier()
    {
        return currentPhase switch
        {
            BossPhase.PhaseOne => 1f,

            BossPhase.PhaseTwo =>
                phaseTwoSpeedMultiplier,

            BossPhase.PhaseThree =>
                phaseThreeSpeedMultiplier,

            _ => 1f
        };
    }

    private float GetDamageMultiplier()
    {
        return currentPhase switch
        {
            BossPhase.PhaseOne => 1f,

            BossPhase.PhaseTwo =>
                phaseTwoDamageMultiplier,

            BossPhase.PhaseThree =>
                phaseThreeDamageMultiplier,

            _ => 1f
        };
    }

    private float GetAdjustedDuration(
        float baseDuration)
    {
        return baseDuration /
               GetSpeedMultiplier();
    }

    private int GetAdjustedDamage(
        int baseDamage)
    {
        return Mathf.RoundToInt(
            baseDamage *
            GetDamageMultiplier()
        );
    }

    private float GetHorizontalDistance(
        Transform target)
    {
        if (target == null)
        {
            return float.MaxValue;
        }

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
            case CombatState.PrepareCombat:
                visualState =
                    BossVisualState.PrepareCombat;

                stateTimer =
                    prepareCombatDuration;

                if (currentTarget != null)
                {
                    movement.StopAndFace(
                        currentTarget
                    );
                }
                break;

            case CombatState.Approach:
                visualState =
                    BossVisualState.CombatIdle;

                stateTimer = 0f;
                break;

            case CombatState.Attack:
                attackTimer = 0f;
                damageApplied = false;
                break;

            case CombatState.Recovery:
                visualState =
                    BossVisualState.CombatIdle;

                stateTimer =
                    GetAdjustedDuration(
                        recoveryDuration
                    );
                break;

            case CombatState.DodgeLeft:
            case CombatState.DodgeRight:
                stateTimer =
                    GetAdjustedDuration(
                        dodgeDuration
                    );
                break;

            case CombatState.DodgeFollowUp:
                attackTimer = 0f;
                damageApplied = false;
                break;

            case CombatState.Backstep:
                stateTimer =
                    GetAdjustedDuration(
                        backstepDuration
                    );
                break;

            case CombatState.Block:
                visualState =
                    BossVisualState.Block;

                stateTimer =
                    GetAdjustedDuration(
                        blockDuration
                    );

                damageApplied = false;
                break;

            case CombatState.BlockCounter:
                attackTimer = 0f;
                damageApplied = false;
                break;

            case CombatState.Dead:
                visualState =
                    BossVisualState.Defeated;
                break;
        }
    }
}