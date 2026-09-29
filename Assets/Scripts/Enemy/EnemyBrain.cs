using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyPatrol))]
[RequireComponent(typeof(EnemyPerception))]
[RequireComponent(typeof(EnemyAttack))]
public class EnemyBrain : MonoBehaviour
{
    private enum EnemyState
    {
        None,
        Patrol,
        Chase,
        Attack,
        Search
    }

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Search")]
    [Min(0f)]
    [SerializeField] private float searchDuration = 3f;

    private EnemyMovement movement;
    private EnemyPatrol patrol;
    private EnemyPerception perception;
    private EnemyAttack attack;

    private Health playerHealth;

    private EnemyState currentState = EnemyState.None;

    private Vector3 lastKnownPlayerPosition;

    private float searchTimer;
    private bool hasReachedSearchPosition;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        patrol = GetComponent<EnemyPatrol>();
        perception = GetComponent<EnemyPerception>();
        attack = GetComponent<EnemyAttack>();
    }

    private void Start()
    {
        playerHealth = player.GetComponent<Health>();

        if (playerHealth == null)
        {
            Debug.LogError(
                "EnemyBrain could not find a Health component on the Player."
            );
        }

        ChangeState(EnemyState.Patrol);
    }

    private void Update()
    {
        if (playerHealth == null || playerHealth.IsDead)
        {
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

            case EnemyState.Attack:
                UpdateAttackState();
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
        ChangeState(EnemyState.Chase);
    }

    private void UpdateChaseState()
    {
        if (!perception.CanSeePlayer())
        {
            ChangeState(EnemyState.Search);
            return;
        }

        RememberPlayerPosition();

        if (attack.IsTargetInRange(player))
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        movement.MoveTo(player.position);
    }

    private void UpdateAttackState()
    {
        if (!perception.CanSeePlayer())
        {
            ChangeState(EnemyState.Search);
            return;
        }

        RememberPlayerPosition();

        if (!attack.IsTargetInRange(player))
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        movement.Stop();
        attack.TryAttack(playerHealth);
    }

    private void UpdateSearchState()
    {
        if (perception.CanSeePlayer())
        {
            RememberPlayerPosition();
            ChangeState(EnemyState.Chase);
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

        ChangeState(EnemyState.Patrol);
    }

    private void RememberPlayerPosition()
    {
        lastKnownPlayerPosition = player.position;
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        ExitState(currentState);

        currentState = newState;

        Debug.Log($"{gameObject.name} state: {currentState}");

        EnterState(currentState);
    }

    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Patrol:
                patrol.StartPatrol();
                break;

            case EnemyState.Chase:
                patrol.StopPatrol();
                break;

            case EnemyState.Attack:
                patrol.StopPatrol();
                movement.Stop();
                break;

            case EnemyState.Search:
                StartSearch();
                break;
        }
    }

    private void ExitState(EnemyState state)
    {
        if (state == EnemyState.Patrol)
        {
            patrol.StopPatrol();
        }
    }

    private void StartSearch()
    {
        patrol.StopPatrol();

        searchTimer = searchDuration;
        hasReachedSearchPosition = false;

        movement.MoveTo(lastKnownPlayerPosition);
    }
}