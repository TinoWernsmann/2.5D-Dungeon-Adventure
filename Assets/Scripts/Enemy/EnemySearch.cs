using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Sucht den Player, nachdem der Sichtkontakt verloren ging:
///  1. Läuft dorthin, wo der Player vermutlich hingelaufen ist – per NavMesh-Pfad, also um Wände herum.
///  2. Schaut sich dort um.
///  3. Prüft danach weitere Stellen in der Nähe. Bevorzugt werden Verstecke,
///     also Punkte, die er von seiner Position aus NICHT sehen kann (hinter Wänden, um Ecken).
/// </summary>
[Serializable]
public class EnemySearch
{
    private const int HidingSpotCandidates = 8;
    private const float ChestHeight = 1f;
    private const float ArrivalTolerance = 0.6f;
    private const float MinimumTravelTime = 0.3f;

    [Tooltip("Wie weit (in Sekunden) die Laufrichtung des Players vorausberechnet wird.")]
    [Min(0f)]
    [SerializeField] private float predictionTime = 1f;

    [Tooltip("Radius um die letzte bekannte Position, in dem weitere Verstecke geprüft werden.")]
    [Min(0f)]
    [SerializeField] private float searchRadius = 6f;

    [Tooltip("Wie viele zusätzliche Stellen nach dem ersten Punkt geprüft werden.")]
    [Min(0)]
    [SerializeField] private int extraSearchPoints = 2;

    [Min(0f)]
    [SerializeField] private float lookAroundDuration = 1.5f;

    [Min(0f)]
    [SerializeField] private float lookAroundSpeed = 120f;

    private Transform owner;
    private EnemyMovement movement;
    private EnemyPerception perception;

    private Vector3 searchCenter;
    private Vector3 currentDestination;
    private float travelTimer;
    private int remainingSearchPoints;

    private bool isLookingAround;
    private float lookAroundTimer;
    private float lookAroundDirection;

    public bool IsFinished { get; private set; }

    public void Initialize(
        Transform owner,
        EnemyMovement movement,
        EnemyPerception perception)
    {
        this.owner = owner;
        this.movement = movement;
        this.perception = perception;
    }

    public void Begin(Vector3 lastKnownPosition, Vector3 lastKnownVelocity)
    {
        IsFinished = false;
        remainingSearchPoints = extraSearchPoints;

        // Vorhersage ab der letzten Player-Position, nicht durch Wände hindurch –
        // dort kann auch der Player nicht langgelaufen sein.
        Vector3 predictedPosition = movement.GetReachablePoint(
            lastKnownPosition,
            lastKnownPosition + lastKnownVelocity * predictionTime
        );

        searchCenter = predictedPosition;

        GoTo(predictedPosition);
    }

    public void Tick(float deltaTime)
    {
        if (IsFinished)
        {
            return;
        }

        if (isLookingAround)
        {
            TickLookAround(deltaTime);
            return;
        }

        travelTimer += deltaTime;

        if (HasArrived())
        {
            StartLookAround();
        }
    }

    private bool HasArrived()
    {
        Vector3 offset = currentDestination - owner.position;
        offset.y = 0f;

        if (offset.magnitude <= ArrivalTolerance)
        {
            return true;
        }

        // Ziel nicht ganz erreichbar -> Agent bleibt vorher stehen.
        // Kurz warten, weil der Agent direkt nach SetDestination noch alte Werte meldet.
        return travelTimer >= MinimumTravelTime &&
               movement.HasReachedDestination;
    }

    private void GoTo(Vector3 position)
    {
        isLookingAround = false;

        if (!movement.TryGetNavMeshPoint(position, out Vector3 navMeshPoint))
        {
            StartLookAround();
            return;
        }

        currentDestination = navMeshPoint;
        travelTimer = 0f;

        movement.MoveTo(navMeshPoint);
    }

    private void StartLookAround()
    {
        isLookingAround = true;
        lookAroundTimer = lookAroundDuration;
        lookAroundDirection = Random.value < 0.5f ? -1f : 1f;

        movement.Stop();
    }

    private void TickLookAround(float deltaTime)
    {
        float previousTimer = lookAroundTimer;
        lookAroundTimer -= deltaTime;

        // Erst in eine Richtung schauen, ab der Hälfte in die andere.
        float halfTime = lookAroundDuration * 0.5f;

        if (previousTimer > halfTime && lookAroundTimer <= halfTime)
        {
            lookAroundDirection = -lookAroundDirection;
        }

        movement.RotateBy(lookAroundDirection * lookAroundSpeed * deltaTime);

        if (lookAroundTimer <= 0f)
        {
            GoToNextSearchPoint();
        }
    }

    private void GoToNextSearchPoint()
    {
        if (remainingSearchPoints <= 0)
        {
            IsFinished = true;
            return;
        }

        remainingSearchPoints--;

        GoTo(FindSearchPoint());
    }

    /// <summary>Bevorzugt Stellen, die der Gegner gerade nicht einsehen kann (Verstecke).</summary>
    private Vector3 FindSearchPoint()
    {
        Vector3 fallbackPoint = searchCenter;

        for (int i = 0; i < HidingSpotCandidates; i++)
        {
            Vector2 offset = Random.insideUnitCircle * searchRadius;
            Vector3 candidate = searchCenter + new Vector3(offset.x, 0f, offset.y);

            if (!movement.TryGetNavMeshPoint(candidate, out Vector3 navMeshPoint))
            {
                continue;
            }

            bool isHidden =
                !perception.HasLineOfSightTo(navMeshPoint + Vector3.up * ChestHeight);

            if (isHidden)
            {
                return navMeshPoint;
            }

            fallbackPoint = navMeshPoint;
        }

        return fallbackPoint;
    }

    public void DrawGizmos()
    {
        if (owner == null || IsFinished)
        {
            return;
        }

        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.5f);
        Gizmos.DrawWireSphere(searchCenter, searchRadius);
        Gizmos.DrawLine(owner.position, searchCenter);
    }
}
