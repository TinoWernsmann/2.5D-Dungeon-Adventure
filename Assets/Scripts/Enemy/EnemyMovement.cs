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

    private float baseMoveSpeed;
    private float speedMultiplier = 1f;

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
        baseMoveSpeed = agent.speed;
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

    /// <summary>Dreht sich (mit Drehgeschwindigkeit) dauerhaft zum Ziel.</summary>
    public void FaceTarget(Transform target)
    {
        EnableManualRotation(target);
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

    /// <summary>Beendet das Ausrichten auf ein Ziel. Die aktuelle Blickrichtung bleibt.</summary>
    public void ResetRotationControl()
    {
        DisableManualRotation();
    }

    /// <summary>Dreht sofort in eine Richtung (z.B. für Ansturm-Angriffe).</summary>
    public void LookInDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        DisableManualRotation();

        transform.rotation =
            Quaternion.LookRotation(direction.normalized);
    }

    /// <summary>Dreht sich um die eigene Achse (z.B. Umschauen bei der Suche).</summary>
    public void RotateBy(float degrees)
    {
        DisableManualRotation();
        transform.Rotate(0f, degrees, 0f);
    }

    /// <summary>
    /// Bewegt den Gegner direkt um <paramref name="displacement"/>, bleibt dabei aber
    /// auf dem NavMesh (läuft also nicht durch Wände).
    /// </summary>
    /// <returns>false, wenn die Bewegung von einer Wand / NavMesh-Kante gebremst wurde.</returns>
    public bool Dash(Vector3 displacement)
    {
        if (!CanUseAgent())
        {
            return false;
        }

        displacement.y = 0f;

        Vector3 startPosition = transform.position;

        agent.Move(displacement);

        Vector3 movedDistance = transform.position - startPosition;
        movedDistance.y = 0f;

        const float BlockedThreshold = 0.5f;

        return movedDistance.magnitude >=
               displacement.magnitude * BlockedThreshold;
    }

    /// <summary>
    /// Liefert den Punkt Richtung <paramref name="desiredPosition"/>,
    /// der vom Gegner aus OHNE Wand dazwischen in gerader Linie erreichbar ist.
    /// Für direkte Bewegungen (Dash, Ausweichen). Für Wege um Wände herum: <see cref="TryGetNavMeshPoint"/>.
    /// </summary>
    public Vector3 GetReachablePoint(Vector3 desiredPosition)
    {
        return GetReachablePoint(transform.position, desiredPosition);
    }

    /// <summary>
    /// Wie weit kommt man von <paramref name="from"/> in gerader Linie Richtung <paramref name="to"/>,
    /// bevor eine Wand / NavMesh-Kante im Weg ist?
    /// </summary>
    public Vector3 GetReachablePoint(Vector3 from, Vector3 to)
    {
        if (!CanUseAgent() ||
            !TryGetNavMeshPoint(from, out Vector3 navMeshFrom))
        {
            return from;
        }

        bool isBlocked = NavMesh.Raycast(
            navMeshFrom,
            to,
            out NavMeshHit hit,
            agent.areaMask
        );

        return isBlocked
            ? hit.position
            : to;
    }

    /// <summary>Nächster begehbarer Punkt auf dem NavMesh (der Agent findet den Weg dorthin selbst).</summary>
    public bool TryGetNavMeshPoint(Vector3 position, out Vector3 navMeshPoint)
    {
        const float MaxSampleDistance = 2f;

        bool found = NavMesh.SamplePosition(
            position,
            out NavMeshHit hit,
            MaxSampleDistance,
            agent != null ? agent.areaMask : NavMesh.AllAreas
        );

        navMeshPoint = found ? hit.position : position;
        return found;
    }

    /// <summary>Abstand von <paramref name="position"/> zur nächsten Wand / NavMesh-Kante.</summary>
    public float GetDistanceToWall(Vector3 position)
    {
        if (!TryGetNavMeshPoint(position, out Vector3 navMeshPoint))
        {
            return 0f;
        }

        bool foundEdge = NavMesh.FindClosestEdge(
            navMeshPoint,
            out NavMeshHit hit,
            agent.areaMask
        );

        return foundEdge ? hit.distance : float.MaxValue;
    }

    /// <summary>Skaliert Lauf- und Drehgeschwindigkeit (z.B. Boss-Phasen).</summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0.01f, multiplier);

        if (agent != null)
        {
            agent.speed = baseMoveSpeed * speedMultiplier;
        }
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
            Quaternion.LookRotation(direction.normalized);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                combatRotationSpeed * speedMultiplier * Time.deltaTime
            );
    }

    private bool CanUseAgent()
    {
        return agent != null &&
               agent.enabled &&
               agent.isOnNavMesh;
    }
}
