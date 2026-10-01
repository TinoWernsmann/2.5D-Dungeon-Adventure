using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Rotation")]
    [Min(0f)]
    [SerializeField] private float combatRotationSpeed = 12f;

    private NavMeshAgent agent;

    private Transform lookTarget;
    private bool useManualRotation;

    public bool HasReachedDestination
    {
        get
        {
            if (!CanUseAgent())
            {
                return true;
            }

            if (agent.pathPending)
            {
                return false;
            }

            if (agent.remainingDistance > agent.stoppingDistance)
            {
                return false;
            }

            return !agent.hasPath ||
                   agent.velocity.sqrMagnitude < 0.01f;
        }
    }

    public bool IsMoving =>
        CanUseAgent() &&
        agent.velocity.sqrMagnitude > 0.01f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void LateUpdate()
    {
        if (!useManualRotation)
        {
            return;
        }

        RotateTowardsLookTarget();
    }

    public void MoveTo(Vector3 destination)
    {
        if (!CanUseAgent())
        {
            return;
        }

        DisableManualRotation();

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    public void MoveToWhileFacing(
        Vector3 destination,
        Transform target)
    {
        if (!CanUseAgent())
        {
            return;
        }

        EnableManualRotation(target);

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    public void FaceTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        EnableManualRotation(target);

        RotateTowardsLookTarget();
    }

    public void Stop()
    {
        if (!CanUseAgent())
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
    }

    public void StopAndFace(Transform target)
    {
        Stop();
        FaceTarget(target);
    }

    public void ResetRotationControl()
    {
        DisableManualRotation();
    }

    private void EnableManualRotation(Transform target)
    {
        if (target == null)
        {
            return;
        }

        lookTarget = target;
        useManualRotation = true;

        if (agent != null)
        {
            agent.updateRotation = false;
        }
    }

    private void DisableManualRotation()
    {
        lookTarget = null;
        useManualRotation = false;

        if (agent != null)
        {
            agent.updateRotation = true;
        }
    }

    private void RotateTowardsLookTarget()
    {
        if (lookTarget == null)
        {
            return;
        }

        Vector3 direction =
            lookTarget.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                combatRotationSpeed * Time.deltaTime
            );
    }

    private bool CanUseAgent()
    {
        return agent != null &&
               agent.enabled &&
               agent.isOnNavMesh;
    }
}