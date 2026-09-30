using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waitTimeAtPoint = 2f;

    private EnemyMovement movement;

    private int currentPointIndex;
    private float waitTimer;
    private bool isWaiting;
    private bool isPatrolling;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
    }

    public void StartPatrol()
    {
        if (!HasPatrolPoints())
        {
            return;
        }

        isPatrolling = true;
        isWaiting = false;

        MoveToCurrentPoint();
    }

    public void StopPatrol()
    {
        isPatrolling = false;
    }

    private void Update()
    {
        if (!isPatrolling || !HasPatrolPoints())
        {
            return;
        }

        if (isWaiting)
        {
            HandleWaiting();
            return;
        }

        if (movement.HasReachedDestination)
        {
            StartWaiting();
        }
    }

    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = waitTimeAtPoint;

        movement.Stop();
    }

    private void HandleWaiting()
    {
        waitTimer -= Time.deltaTime;

        if (waitTimer > 0f)
        {
            return;
        }

        isWaiting = false;

        SelectNextPoint();
        MoveToCurrentPoint();
    }

    private void SelectNextPoint()
    {
        currentPointIndex =
            (currentPointIndex + 1) % patrolPoints.Length;
    }

    private void MoveToCurrentPoint()
    {
        Transform patrolPoint =
            patrolPoints[currentPointIndex];

        if (patrolPoint == null)
        {
            return;
        }

        movement.MoveTo(patrolPoint.position);
    }

    private bool HasPatrolPoints()
    {
        return patrolPoints != null &&
               patrolPoints.Length > 0;
    }
}