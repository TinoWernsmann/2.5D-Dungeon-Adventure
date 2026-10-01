using UnityEngine;

public class MageCombatBehaviour : EnemyCombatBehaviour
{
    private enum CombatState
    {
        None,
        Approach,
        Retreat,
        Cast,
        Recovery
    }

    private enum SpellType
    {
        MagicBolt,
        Fireball
    }

    [Header("References")]
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Combat Distance")]
    [Min(0.1f)]
    [SerializeField] private float combatRange = 12f;

    [Min(0.1f)]
    [SerializeField] private float minimumDistance = 5f;

    [Min(0.1f)]
    [SerializeField] private float preferredDistance = 8f;

    [Min(0.1f)]
    [SerializeField] private float maximumAttackDistance = 10f;

    [Header("Magic Bolt")]
    [SerializeField] private EnemyProjectile magicBoltPrefab;

    [Min(1)]
    [SerializeField] private int magicBoltDamage = 10;

    [Min(0.01f)]
    [SerializeField] private float magicBoltCastDuration = 0.5f;

    [Min(0f)]
    [SerializeField] private float magicBoltRecoveryDuration = 0.65f;

    [Header("Fireball")]
    [SerializeField] private EnemyProjectile fireballPrefab;

    [Min(1)]
    [SerializeField] private int fireballDamage = 25;

    [Min(0.01f)]
    [SerializeField] private float fireballCastDuration = 1.2f;

    [Min(0f)]
    [SerializeField] private float fireballRecoveryDuration = 1.4f;

    [Range(0f, 1f)]
    [SerializeField] private float fireballChance = 0.35f;

    [Min(0f)]
    [SerializeField] private float fireballCooldown = 4f;

    [Header("Retreat")]
    [Min(0.1f)]
    [SerializeField] private float retreatDistance = 3f;

    [Min(0.1f)]
    [SerializeField] private float retreatDuration = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugCombat;

    private CombatState currentState =
        CombatState.None;

    private SpellType currentSpell =
        SpellType.MagicBolt;

    private Transform currentTarget;
    private Health currentTargetHealth;

    private float stateTimer;
    private float castDuration;
    private float castElapsed;
    private float fireballCooldownTimer;

    private bool spellReleased;

    public override bool IsPerformingAttack =>
        currentState == CombatState.Cast;

    public override float AttackProgress
    {
        get
        {
            if (currentState != CombatState.Cast ||
                castDuration <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                castElapsed / castDuration
            );
        }
    }

    public bool IsCasting =>
        currentState == CombatState.Cast;

    public bool IsRetreating =>
        currentState == CombatState.Retreat;

    public bool IsCastingFireball =>
        IsCasting &&
        currentSpell == SpellType.Fireball;

    public bool IsCastingMagicBolt =>
        IsCasting &&
        currentSpell == SpellType.MagicBolt;

    private void Awake()
    {
        if (movement == null)
        {
            movement =
                GetComponent<EnemyMovement>();
        }
    }

    private void Update()
    {
        UpdateFireballCooldown();
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

        ChangeState(
            CombatState.Approach
        );
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
            case CombatState.None:
                ChangeState(
                    CombatState.Approach
                );
                break;

            case CombatState.Approach:
                UpdateApproach();
                break;

            case CombatState.Retreat:
                UpdateRetreat();
                break;

            case CombatState.Cast:
                UpdateCast();
                break;

            case CombatState.Recovery:
                UpdateRecovery();
                break;
        }
    }

    public override void ExitCombat()
    {
        if (movement != null)
        {
            movement.Stop();
            movement.ResetRotationControl();
        }

        currentTarget = null;
        currentTargetHealth = null;

        stateTimer = 0f;
        castElapsed = 0f;
        castDuration = 0f;

        spellReleased = false;

        currentState =
            CombatState.None;
    }

    private void UpdateApproach()
    {
        float distance =
            GetHorizontalDistance(
                currentTarget
            );

        if (distance < minimumDistance)
        {
            StartRetreat();
            return;
        }

        if (distance > maximumAttackDistance)
        {
            movement.MoveTo(
                currentTarget.position
            );

            return;
        }

        movement.StopAndFace(
            currentTarget
        );

        SelectSpell();

        ChangeState(
            CombatState.Cast
        );
    }

    private void UpdateRetreat()
    {
        if (!HasValidTarget())
        {
            return;
        }

        stateTimer -= Time.deltaTime;

        float distance =
            GetHorizontalDistance(
                currentTarget
            );

        if (distance >= preferredDistance)
        {
            FinishRetreat();
            return;
        }

        if (stateTimer <= 0f ||
            movement.HasReachedDestination)
        {
            FinishRetreat();
        }
    }

    private void StartRetreat()
    {
        if (!HasValidTarget())
        {
            return;
        }

        Vector3 directionAway =
            transform.position -
            currentTarget.position;

        directionAway.y = 0f;

        if (directionAway.sqrMagnitude <=
            0.001f)
        {
            directionAway =
                -transform.forward;
        }

        directionAway.Normalize();

        Vector3 destination =
            transform.position +
            directionAway *
            retreatDistance;

        movement.MoveToWhileFacing(
            destination,
            currentTarget
        );

        ChangeState(
            CombatState.Retreat
        );

        LogCombat(
            "Player too close -> Retreat."
        );
    }

    private void FinishRetreat()
    {
        movement.StopAndFace(
            currentTarget
        );

        ChangeState(
            CombatState.Approach
        );
    }

    private void SelectSpell()
    {
        if (CanUseFireball())
        {
            currentSpell =
                SpellType.Fireball;

            castDuration =
                fireballCastDuration;

            LogCombat(
                "Preparing FIREBALL."
            );

            return;
        }

        currentSpell =
            SpellType.MagicBolt;

        castDuration =
            magicBoltCastDuration;

        LogCombat(
            "Preparing Magic Bolt."
        );
    }

    private bool CanUseFireball()
    {
        if (fireballPrefab == null)
        {
            return false;
        }

        if (fireballCooldownTimer > 0f)
        {
            return false;
        }

        return Random.value <=
               fireballChance;
    }

    private void UpdateCast()
    {
        if (!HasValidTarget())
        {
            return;
        }

        movement.StopAndFace(
            currentTarget
        );

        castElapsed +=
            Time.deltaTime;

        if (!spellReleased &&
            castElapsed >= castDuration)
        {
            ReleaseSpell();
        }

        if (castElapsed >= castDuration)
        {
            ChangeState(
                CombatState.Recovery
            );
        }
    }

    private void ReleaseSpell()
    {
        spellReleased = true;

        EnemyProjectile prefab =
            GetCurrentProjectilePrefab();

        if (prefab == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: " +
                $"No projectile prefab assigned " +
                $"for {currentSpell}.",
                gameObject
            );

            return;
        }

        if (projectileSpawnPoint == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: " +
                "Projectile Spawn Point is missing.",
                gameObject
            );

            return;
        }

        Vector3 targetPosition =
            GetTargetPosition();

        Vector3 direction =
            targetPosition -
            projectileSpawnPoint.position;

        if (direction.sqrMagnitude <=
            0.001f)
        {
            direction =
                transform.forward;
        }

        EnemyProjectile projectile =
            Instantiate(
                prefab,
                projectileSpawnPoint.position,
                Quaternion.identity
            );

        projectile.Initialize(
            direction,
            GetCurrentSpellDamage(),
            gameObject
        );

        if (currentSpell ==
            SpellType.Fireball)
        {
            fireballCooldownTimer =
                fireballCooldown;
        }

        LogCombat(
            $"Released {currentSpell}."
        );
    }

    private EnemyProjectile
        GetCurrentProjectilePrefab()
    {
        return currentSpell switch
        {
            SpellType.MagicBolt =>
                magicBoltPrefab,

            SpellType.Fireball =>
                fireballPrefab,

            _ => null
        };
    }

    private int GetCurrentSpellDamage()
    {
        return currentSpell switch
        {
            SpellType.MagicBolt =>
                magicBoltDamage,

            SpellType.Fireball =>
                fireballDamage,

            _ => 0
        };
    }

    private float
        GetCurrentRecoveryDuration()
    {
        return currentSpell switch
        {
            SpellType.MagicBolt =>
                magicBoltRecoveryDuration,

            SpellType.Fireball =>
                fireballRecoveryDuration,

            _ => magicBoltRecoveryDuration
        };
    }

    private Vector3 GetTargetPosition()
    {
        if (currentTarget == null)
        {
            return transform.position +
                   transform.forward;
        }

        Vector3 targetPosition =
            currentTarget.position;

        targetPosition.y += 1f;

        return targetPosition;
    }

    private void UpdateRecovery()
    {
        if (!HasValidTarget())
        {
            return;
        }

        movement.StopAndFace(
            currentTarget
        );

        stateTimer -=
            Time.deltaTime;

        if (stateTimer > 0f)
        {
            return;
        }

        float distance =
            GetHorizontalDistance(
                currentTarget
            );

        if (distance < minimumDistance)
        {
            StartRetreat();
            return;
        }

        ChangeState(
            CombatState.Approach
        );
    }

    private void UpdateFireballCooldown()
    {
        if (fireballCooldownTimer <= 0f)
        {
            return;
        }

        fireballCooldownTimer -=
            Time.deltaTime;

        if (fireballCooldownTimer < 0f)
        {
            fireballCooldownTimer = 0f;
        }
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
            case CombatState.None:
                break;

            case CombatState.Approach:
                stateTimer = 0f;
                break;

            case CombatState.Retreat:
                stateTimer =
                    retreatDuration;
                break;

            case CombatState.Cast:
                movement.StopAndFace(
                    currentTarget
                );

                castElapsed = 0f;
                spellReleased = false;
                break;

            case CombatState.Recovery:
                stateTimer =
                    GetCurrentRecoveryDuration();

                LogCombat(
                    $"{currentSpell} recovery: " +
                    $"{stateTimer:F2}s."
                );
                break;
        }
    }

    private void LogCombat(
        string message)
    {
        if (!debugCombat)
        {
            return;
        }

        Debug.Log(
            $"[MAGE] {message}",
            gameObject
        );
    }
}