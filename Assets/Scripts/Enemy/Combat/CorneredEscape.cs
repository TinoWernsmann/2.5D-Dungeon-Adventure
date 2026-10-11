using System;
using UnityEngine;

/// <summary>
/// Erkennt, wenn ein Gegner an eine Wand oder in eine Ecke gedrängt wird,
/// und bringt ihn zurück in den freien Raum.
///
/// "Gedrängt" = Wand ist nah UND das Ziel steht dicht vor ihm.
/// Der Fluchtpunkt wird aus mehreren Richtungen gewählt: möglichst viel Platz,
/// weg vom Ziel und nicht mitten durch das Ziel hindurch (also seitlich an der Wand entlang).
/// </summary>
[Serializable]
public class CorneredEscape
{
    private const int DirectionSamples = 12;
    private const float MinimumTravelFraction = 0.4f;
    private const float MinimumPassDistance = 1f;
    private const float MaxUsefulWallDistance = 2.5f;
    private const float ArrivalTolerance = 0.5f;
    private const float CheckInterval = 0.25f;

    [Tooltip("Ab diesem Abstand zur Wand fühlt sich der Gegner eingeengt.")]
    [Min(0f)]
    [SerializeField] private float wallDistanceThreshold = 0.9f;

    [Tooltip("Nur wenn das Ziel näher als dieser Abstand ist, fühlt er sich bedrängt.")]
    [Min(0f)]
    [SerializeField] private float pressureDistance = 2.5f;

    [Min(0.5f)]
    [SerializeField] private float escapeDistance = 3f;

    [Min(0.1f)]
    [SerializeField] private float maxEscapeDuration = 1.2f;

    [Tooltip("Wartezeit, bevor er erneut ausbricht (verhindert Hin-und-her-Zappeln).")]
    [Min(0f)]
    [SerializeField] private float cooldown = 2f;

    private Transform owner;
    private EnemyMovement movement;

    private Vector3 escapePoint;
    private float escapeTimer;
    private float cooldownTimer;
    private float checkTimer;

    public bool IsEscaping { get; private set; }

    public void Initialize(Transform owner, EnemyMovement movement)
    {
        this.owner = owner;
        this.movement = movement;
    }

    /// <returns>true, solange der Gegner ausbricht – das normale Kampfverhalten pausiert dann.</returns>
    public bool Tick(Transform threat, bool canStartEscape, float deltaTime)
    {
        cooldownTimer -= deltaTime;

        if (IsEscaping)
        {
            TickEscape(threat, deltaTime);
            return true;
        }

        if (!canStartEscape || cooldownTimer > 0f)
        {
            return false;
        }

        checkTimer -= deltaTime;

        if (checkTimer > 0f)
        {
            return false;
        }

        checkTimer = CheckInterval;

        if (!IsCornered(threat))
        {
            return false;
        }

        if (!TryFindEscapePoint(threat.position, out escapePoint))
        {
            // Kein Ausweg gefunden -> später erneut versuchen.
            cooldownTimer = cooldown;
            return false;
        }

        StartEscape(threat);
        return true;
    }

    public void Cancel()
    {
        IsEscaping = false;
    }

    public bool IsCornered(Transform threat)
    {
        return HorizontalDistance(owner.position, threat.position) <= pressureDistance &&
               movement.GetDistanceToWall(owner.position) <= wallDistanceThreshold;
    }

    /// <summary>
    /// Sucht den besten erreichbaren Punkt mit viel Platz, weg von <paramref name="threatPosition"/>.
    /// </summary>
    public bool TryFindEscapePoint(Vector3 threatPosition, out Vector3 bestPoint)
    {
        Vector3 origin = owner.position;
        Vector3 awayFromThreat = origin - threatPosition;
        awayFromThreat.y = 0f;

        if (awayFromThreat.sqrMagnitude <= 0.001f)
        {
            awayFromThreat = -owner.forward;
        }

        awayFromThreat.Normalize();

        bestPoint = origin;
        float bestScore = float.MinValue;

        for (int i = 0; i < DirectionSamples; i++)
        {
            float angle = i * (360f / DirectionSamples);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * awayFromThreat;

            Vector3 candidate =
                movement.GetReachablePoint(origin + direction * escapeDistance);

            if (!IsUsableEscapePoint(origin, candidate, threatPosition))
            {
                continue;
            }

            float score = ScoreEscapePoint(origin, candidate, threatPosition);

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = candidate;
            }
        }

        return bestScore > float.MinValue;
    }

    private bool IsUsableEscapePoint(Vector3 origin, Vector3 candidate, Vector3 threatPosition)
    {
        float travelDistance = HorizontalDistance(origin, candidate);

        if (travelDistance < escapeDistance * MinimumTravelFraction)
        {
            return false;
        }

        // Nicht mitten durch das Ziel hindurch fliehen.
        return DistanceFromSegment(threatPosition, origin, candidate) >= MinimumPassDistance;
    }

    private float ScoreEscapePoint(Vector3 origin, Vector3 candidate, Vector3 threatPosition)
    {
        float openSpace = Mathf.Min(
            movement.GetDistanceToWall(candidate),
            MaxUsefulWallDistance
        );

        float distanceFromThreat = HorizontalDistance(candidate, threatPosition);
        float travelDistance = HorizontalDistance(origin, candidate);

        const float OpenSpaceWeight = 2f;
        const float ThreatDistanceWeight = 1f;
        const float TravelWeight = 0.5f;

        return openSpace * OpenSpaceWeight +
               distanceFromThreat * ThreatDistanceWeight +
               travelDistance * TravelWeight;
    }

    private void StartEscape(Transform threat)
    {
        IsEscaping = true;
        escapeTimer = 0f;

        movement.MoveToWhileFacing(escapePoint, threat);
    }

    private void TickEscape(Transform threat, float deltaTime)
    {
        escapeTimer += deltaTime;

        movement.MoveToWhileFacing(escapePoint, threat);

        bool hasArrived =
            HorizontalDistance(owner.position, escapePoint) <= ArrivalTolerance;

        if (hasArrived || escapeTimer >= maxEscapeDuration)
        {
            IsEscaping = false;
            cooldownTimer = cooldown;
        }
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        Vector3 offset = b - a;
        offset.y = 0f;

        return offset.magnitude;
    }

    private static float DistanceFromSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
    {
        Vector3 segment = segmentEnd - segmentStart;
        segment.y = 0f;

        Vector3 toPoint = point - segmentStart;
        toPoint.y = 0f;

        float segmentLengthSqr = segment.sqrMagnitude;

        if (segmentLengthSqr <= 0.0001f)
        {
            return toPoint.magnitude;
        }

        float t = Mathf.Clamp01(Vector3.Dot(toPoint, segment) / segmentLengthSqr);

        return (toPoint - segment * t).magnitude;
    }
}
