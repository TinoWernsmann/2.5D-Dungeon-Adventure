using System;
using UnityEngine;

/// <summary>
/// Schneller Schritt in eine Richtung, während der Boss den Player im Blick behält
/// (Seitschritt / Rückwärtsschritt). Bleibt auf dem NavMesh – läuft nicht in Wände.
/// </summary>
[Serializable]
public class BossDashAction : BossAction
{
    [Min(0.1f)]
    [SerializeField] private float distance = 2.5f;

    [Min(0.01f)]
    [SerializeField] private float duration = 0.45f;

    private Vector3 direction;
    private Vector3 destination;
    private BossVisualState visualState = BossVisualState.CombatIdle;
    private float timer;

    // Für die Unity-Serialisierung.
    public BossDashAction()
    {
    }

    public BossDashAction(float distance, float duration)
    {
        this.distance = distance;
        this.duration = duration;
    }

    public override BossVisualState VisualState => visualState;

    /// <summary>Muss vor dem Start der Aktion gesetzt werden.</summary>
    public void SetDirection(Vector3 worldDirection, BossVisualState visual)
    {
        worldDirection.y = 0f;

        direction = worldDirection.sqrMagnitude > 0.001f
            ? worldDirection.normalized
            : -BossTransform.forward;

        visualState = visual;
    }

    public override void Enter()
    {
        timer = 0f;

        destination = Movement.GetReachablePoint(
            BossTransform.position + direction * distance
        );

        Movement.Stop();
        Movement.FaceTarget(Boss.CurrentTarget);
    }

    public override bool Tick(float deltaTime)
    {
        timer += deltaTime;

        float scaledDuration = Boss.ScaleDuration(duration);

        Vector3 remaining = destination - BossTransform.position;
        remaining.y = 0f;

        float stepLength = Mathf.Min(
            distance / scaledDuration * deltaTime,
            remaining.magnitude
        );

        Movement.Dash(direction * stepLength);

        const float ArrivalTolerance = 0.05f;

        return timer >= scaledDuration ||
               remaining.magnitude <= ArrivalTolerance;
    }
}
