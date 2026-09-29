using UnityEngine;

public class EnemyPerception : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform detectionOrigin;

    [Header("Vision")]
    [Min(0f)]
    [SerializeField] private float viewDistance = 10f;

    [Range(0f, 360f)]
    [SerializeField] private float viewAngle = 90f;

    [Header("Awareness")]
    [Tooltip("Innerhalb dieser Entfernung wird der Player unabhängig vom Sichtwinkel erkannt.")]
    [Min(0f)]
    [SerializeField] private float proximityDetectionRange = 2f;

    [Header("Line of Sight")]
    [SerializeField] private LayerMask obstacleMask;

    public bool CanSeePlayer()
    {
        if (player == null || detectionOrigin == null)
        {
            return false;
        }

        Vector3 directionToPlayer =
            player.position - detectionOrigin.position;

        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > viewDistance)
        {
            return false;
        }

        if (distanceToPlayer <= proximityDetectionRange)
        {
            return HasLineOfSight(directionToPlayer, distanceToPlayer);
        }

        if (!IsInsideViewAngle(directionToPlayer))
        {
            return false;
        }

        return HasLineOfSight(
            directionToPlayer,
            distanceToPlayer
        );
    }

    private bool IsInsideViewAngle(Vector3 directionToPlayer)
    {
        Vector3 horizontalDirection = directionToPlayer;
        horizontalDirection.y = 0f;

        if (horizontalDirection.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        float angleToPlayer = Vector3.Angle(
            transform.forward,
            horizontalDirection.normalized
        );

        return angleToPlayer <= viewAngle * 0.5f;
    }

    private bool HasLineOfSight(
        Vector3 directionToPlayer,
        float distanceToPlayer)
    {
        Vector3 normalizedDirection =
            directionToPlayer.normalized;

        bool obstacleHit = Physics.Raycast(
            detectionOrigin.position,
            normalizedDirection,
            distanceToPlayer,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        return !obstacleHit;
    }

    private void OnDrawGizmosSelected()
    {
        if (detectionOrigin == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(
            detectionOrigin.position,
            proximityDetectionRange
        );

        Vector3 leftBoundary = Quaternion.Euler(
            0f,
            -viewAngle * 0.5f,
            0f
        ) * transform.forward;

        Vector3 rightBoundary = Quaternion.Euler(
            0f,
            viewAngle * 0.5f,
            0f
        ) * transform.forward;

        Gizmos.DrawRay(
            detectionOrigin.position,
            leftBoundary * viewDistance
        );

        Gizmos.DrawRay(
            detectionOrigin.position,
            rightBoundary * viewDistance
        );
    }
}