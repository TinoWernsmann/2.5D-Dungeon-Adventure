using System;
using UnityEngine;

/// <summary>
/// Sprungangriff aus mittlerer Distanz: lange Ankündigung, dann ein schneller Vorstoß
/// in einer festen Richtung. Bestraft Spieler, die nur auf Abstand bleiben.
/// </summary>
[Serializable]
public class BossLungeAction : BossAction
{
    private enum Step
    {
        Windup,
        Lunge
    }

    [Min(0.01f)]
    [SerializeField] private float windupDuration = 0.6f;

    [Min(0.1f)]
    [SerializeField] private float lungeSpeed = 12f;

    [Min(0.01f)]
    [SerializeField] private float maxLungeDuration = 0.45f;

    [Min(0.1f)]
    [SerializeField] private float hitRange = 1.6f;

    [Min(1)]
    [SerializeField] private int damage = 8;

    [Min(0f)]
    [Tooltip("Vorausberechnung der Player-Bewegung in Sekunden.")]
    [SerializeField] private float aimLeadTime = 0.2f;

    private Step step;
    private float timer;
    private Vector3 lungeDirection;
    private bool hasHit;

    public override BossVisualState VisualState =>
        step == Step.Windup
            ? BossVisualState.WindupRight
            : BossVisualState.AttackRight;

    public override bool IsAttacking => true;

    public override float AttackProgress =>
        step == Step.Lunge
            ? Mathf.Clamp01(timer / maxLungeDuration)
            : 0f;

    public override void Enter()
    {
        step = Step.Windup;
        timer = 0f;
        hasHit = false;

        Movement.StopAndFace(Boss.CurrentTarget);
    }

    public override bool Tick(float deltaTime)
    {
        timer += deltaTime;

        return step == Step.Windup
            ? TickWindup()
            : TickLunge(deltaTime);
    }

    private bool TickWindup()
    {
        if (timer < Boss.ScaleDuration(windupDuration))
        {
            return false;
        }

        lungeDirection = Boss.GetDirectionToPredictedTarget(aimLeadTime);
        Movement.LookInDirection(lungeDirection);

        step = Step.Lunge;
        timer = 0f;

        Boss.PlayAttackSound();

        return false;
    }

    private bool TickLunge(float deltaTime)
    {
        bool movedFreely =
            Movement.Dash(lungeDirection * lungeSpeed * deltaTime);

        if (!hasHit)
        {
            hasHit = Boss.TryDealDamage(hitRange, damage);
        }

        return !movedFreely || timer >= maxLungeDuration;
    }
}
