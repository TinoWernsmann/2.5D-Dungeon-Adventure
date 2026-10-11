using UnityEngine;

/// <summary>
/// Sieht der Gegner den Player? Prüft Distanz, Sichtwinkel und Sichtlinie.
///
/// Die Sichtlinie wird auf Körpermitte und Kopf des Player-Colliders geprüft –
/// NICHT auf player.position. Der Pivot des Players liegt auf Fußhöhe (bzw. darunter),
/// dadurch würde der Strahl den Boden treffen, sobald dieser auf dem Obstacle-Layer liegt.
/// </summary>
public class EnemyPerception : MonoBehaviour
{
    private const float HeadHeightFactor = 0.85f;

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

    [Tooltip("So lange bleibt der Gegner aufmerksam, nachdem der Sichtkontakt abgerissen ist " +
             "(verhindert Flackern, wenn z.B. eine Säule kurz die Sicht verdeckt).")]
    [Min(0f)]
    [SerializeField] private float sightMemoryDuration = 0.75f;

    [Header("Line of Sight")]
    [Tooltip("Layer, die die Sicht blockieren (z.B. Obstacle). " +
             "Der Layer des Players wird automatisch ignoriert.")]
    [SerializeField] private LayerMask obstacleMask;

    private Collider playerCollider;

    private float awareUntilTime = float.NegativeInfinity;

    private int cachedFrame = -1;
    private bool cachedCanSeePlayer;

    /// <summary>Sieht der Gegner den Player in diesem Frame direkt?</summary>
    public bool CanSeePlayer()
    {
        if (cachedFrame != Time.frameCount)
        {
            cachedFrame = Time.frameCount;
            cachedCanSeePlayer = EvaluateSight();

            if (cachedCanSeePlayer)
            {
                ExtendAwareness(sightMemoryDuration);
            }
        }

        return cachedCanSeePlayer;
    }

    /// <summary>Sieht den Player oder hat ihn gerade eben noch gesehen.</summary>
    public bool IsAwareOfPlayer =>
        CanSeePlayer() ||
        Time.time <= awareUntilTime;

    /// <summary>
    /// Macht den Gegner für eine Zeit auf den Player aufmerksam,
    /// z.B. wenn er getroffen wurde.
    /// </summary>
    public void ExtendAwareness(float duration)
    {
        awareUntilTime = Mathf.Max(
            awareUntilTime,
            Time.time + duration
        );
    }

    /// <summary>Ist der Punkt von den Augen des Gegners aus sichtbar (keine Wand dazwischen)?</summary>
    public bool HasLineOfSightTo(Vector3 point)
    {
        return detectionOrigin != null &&
               HasLineOfSight(detectionOrigin.position, point);
    }

    private void Awake()
    {
        if (player == null)
        {
            return;
        }

        playerCollider =
            player.GetComponent<Collider>();

        // Der Player selbst darf die Sichtlinie nie blockieren.
        obstacleMask &= ~(1 << player.gameObject.layer);
    }

    private bool EvaluateSight()
    {
        if (!enabled ||
            player == null ||
            detectionOrigin == null)
        {
            return false;
        }

        Vector3 origin = detectionOrigin.position;
        Vector3 bodyCenter = GetPlayerBodyCenter();

        Vector3 directionToPlayer = bodyCenter - origin;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > viewDistance)
        {
            return false;
        }

        bool isVeryClose =
            distanceToPlayer <= proximityDetectionRange;

        if (!isVeryClose &&
            !IsInsideViewAngle(directionToPlayer))
        {
            return false;
        }

        return HasLineOfSight(origin, bodyCenter) ||
               HasLineOfSight(origin, GetPlayerHeadPosition());
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
            horizontalDirection
        );

        return angleToPlayer <= viewAngle * 0.5f;
    }

    private bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        return !Physics.Linecast(
            from,
            to,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private Vector3 GetPlayerBodyCenter()
    {
        if (playerCollider != null)
        {
            return playerCollider.bounds.center;
        }

        return player.position + Vector3.up;
    }

    private Vector3 GetPlayerHeadPosition()
    {
        if (playerCollider == null)
        {
            return player.position + Vector3.up * 1.6f;
        }

        Bounds bounds = playerCollider.bounds;

        return bounds.center +
               Vector3.up * (bounds.extents.y * HeadHeightFactor);
    }

    private void OnDrawGizmosSelected()
    {
        if (detectionOrigin == null)
        {
            return;
        }

        Vector3 origin = detectionOrigin.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, proximityDetectionRange);

        Vector3 leftBoundary =
            Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;

        Vector3 rightBoundary =
            Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;

        Gizmos.DrawRay(origin, leftBoundary * viewDistance);
        Gizmos.DrawRay(origin, rightBoundary * viewDistance);

        if (!Application.isPlaying || player == null)
        {
            return;
        }

        Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
        Gizmos.DrawLine(origin, GetPlayerBodyCenter());
        Gizmos.DrawLine(origin, GetPlayerHeadPosition());
    }
}
