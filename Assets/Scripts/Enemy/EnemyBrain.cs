using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyPatrol))]
[RequireComponent(typeof(EnemyPerception))]
public class EnemyBrain : MonoBehaviour
{
    private enum EnemyState
    {
        None,
        Patrol,
        Chase,
        Combat,
        Search
    }

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Combat")]
    [SerializeField] private EnemyCombatBehaviour combatBehaviour;

    [Header("Search")]
    [Min(0f)]
    [SerializeField] private float searchDuration = 3f;

    private EnemyMovement movement;
    private EnemyPatrol patrol;
    private EnemyPerception perception;

    private Health playerHealth;

    private EnemyState currentState =
        EnemyState.None;

    private Vector3 lastKnownPlayerPosition;

    private float searchTimer;
    private bool hasReachedSearchPosition;

    private SpearBossCombatBehaviour spearBoss;

    private void Awake()
    {
        movement =
            GetComponent<EnemyMovement>();

        patrol =
            GetComponent<EnemyPatrol>();

        perception =
            GetComponent<EnemyPerception>();

        if (combatBehaviour == null)
        {
            combatBehaviour =
                GetComponent<EnemyCombatBehaviour>();
        }

        spearBoss =
            combatBehaviour as
                SpearBossCombatBehaviour;
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError(
                $"{gameObject.name}: " +
                "EnemyBrain has no Player assigned."
            );

            enabled = false;
            return;
        }

        playerHealth =
            player.GetComponent<Health>();

        if (playerHealth == null)
        {
            Debug.LogError(
                $"{gameObject.name}: " +
                "Player has no Health component."
            );

            enabled = false;
            return;
        }

        if (combatBehaviour == null)
        {
            Debug.LogError(
                $"{gameObject.name}: " +
                "No EnemyCombatBehaviour found."
            );

            enabled = false;
            return;
        }

        ChangeState(
            EnemyState.Patrol
        );
    }

    private void Update()
    {
        if (playerHealth == null ||
            playerHealth.IsDead)
        {
            movement.Stop();
            return;
        }

        UpdateCurrentState();
    }

    private void UpdateCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrolState();
                break;

            case EnemyState.Chase:
                UpdateChaseState();
                break;

            case EnemyState.Combat:
                UpdateCombatState();
                break;

            case EnemyState.Search:
                UpdateSearchState();
                break;
        }
    }

    private void UpdatePatrolState()
    {
        if (!perception.CanSeePlayer())
        {
            return;
        }

        RememberPlayerPosition();

        NotifyBossPlayerDetected();

        ChangeState(
            EnemyState.Chase
        );
    }

    private void UpdateChaseState()
    {
        if (!perception.CanSeePlayer())
        {
            ChangeState(
                EnemyState.Search
            );

            return;
        }

        RememberPlayerPosition();

        NotifyBossPlayerDetected();

        if (combatBehaviour.IsInCombatRange(
                player))
        {
            ChangeState(
                EnemyState.Combat
            );

            return;
        }

        if (spearBoss != null &&
            spearBoss.IsCombatPrepared)
        {
            movement.MoveToWhileFacing(
                player.position,
                player
            );

            return;
        }

        // Während PrepareAttack soll der
        // Boss stehen bleiben.
        if (spearBoss != null)
        {
            movement.StopAndFace(player);
            return;
        }

        movement.MoveTo(
            player.position
        );
    }

    private void UpdateCombatState()
    {
        if (!perception.CanSeePlayer())
        {
            ChangeState(
                EnemyState.Search
            );

            return;
        }

        RememberPlayerPosition();

        combatBehaviour.UpdateCombat(
            player,
            playerHealth
        );
    }

    private void UpdateSearchState()
    {
        if (perception.CanSeePlayer())
        {
            RememberPlayerPosition();

            NotifyBossPlayerDetected();

            ChangeState(
                EnemyState.Chase
            );

            return;
        }

        if (!hasReachedSearchPosition)
        {
            CheckSearchDestination();
            return;
        }

        UpdateSearchTimer();
    }

    private void CheckSearchDestination()
    {
        if (!movement.HasReachedDestination)
        {
            return;
        }

        hasReachedSearchPosition = true;

        movement.Stop();
    }

    private void UpdateSearchTimer()
    {
        searchTimer -= Time.deltaTime;

        if (searchTimer > 0f)
        {
            return;
        }

        ChangeState(
            EnemyState.Patrol
        );
    }

    private void RememberPlayerPosition()
    {
        lastKnownPlayerPosition =
            player.position;
    }

    private void NotifyBossPlayerDetected()
    {
        if (spearBoss == null)
        {
            return;
        }

        spearBoss.NotifyPlayerDetected(
            player,
            playerHealth
        );
    }

    private void NotifyBossPlayerLost()
    {
        if (spearBoss == null)
        {
            return;
        }

        spearBoss.NotifyPlayerLost();
    }

    private void ChangeState(
        EnemyState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        ExitState(currentState);

        currentState = newState;

        EnterState(currentState);
    }

    private void EnterState(
        EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Patrol:
                NotifyBossPlayerLost();

                patrol.StartPatrol();
                break;

            case EnemyState.Chase:
                patrol.StopPatrol();

                NotifyBossPlayerDetected();
                break;

            case EnemyState.Combat:
                patrol.StopPatrol();

                NotifyBossPlayerDetected();

                combatBehaviour.EnterCombat(
                    player,
                    playerHealth
                );
                break;

            case EnemyState.Search:
                StartSearch();
                break;
        }
    }

    private void ExitState(
        EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Patrol:
                patrol.StopPatrol();
                break;

            case EnemyState.Combat:
                combatBehaviour.ExitCombat();
                break;
        }
    }

    private void StartSearch()
    {
        patrol.StopPatrol();

        searchTimer = searchDuration;
        hasReachedSearchPosition = false;

        movement.MoveTo(
            lastKnownPlayerPosition
        );
    }
}