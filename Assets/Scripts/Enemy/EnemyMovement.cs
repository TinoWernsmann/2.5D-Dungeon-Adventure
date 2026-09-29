using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent agent;

    public bool HasReachedDestination
    {
        get
        {
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

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void MoveTo(Vector3 destination)
    {
        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    public void Stop()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }
    public bool IsMoving =>
    agent != null &&
    agent.isOnNavMesh &&
    agent.velocity.sqrMagnitude > 0.01f;
}