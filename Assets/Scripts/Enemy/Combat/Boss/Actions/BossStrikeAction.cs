using System;
using UnityEngine;

/// <summary>
/// Ein oder mehrere Speerstiche hintereinander (Kombo).
/// Jeder Stich wird angekündigt (Windup-Sprite), danach ist die Richtung fest –
/// der Player kann also seitlich ausweichen oder hinter den Boss gelangen.
/// </summary>
[Serializable]
public class BossStrikeAction : BossAction
{
    private enum Step
    {
        Windup,
        Strike
    }

    [Min(0.01f)]
    [SerializeField] private float windupDuration = 0.45f;

    [Range(0.1f, 1f)]
    [Tooltip("Folgestiche einer Kombo werden schneller angekündigt.")]
    [SerializeField] private float followUpWindupFactor = 0.6f;

    [Min(0.01f)]
    [SerializeField] private float strikeDuration = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float damageMoment = 0.55f;

    [Min(0.1f)]
    [SerializeField] private float range = 3f;

    [Min(1)]
    [SerializeField] private int damage = 5;

    [Min(0f)]
    [Tooltip("Kleiner Schritt nach vorne bei jedem Stich – gibt dem Angriff Gewicht.")]
    [SerializeField] private float stepDistance = 0.5f;

    private Step step;
    private float timer;
    private float currentWindupDuration;
    private int hitCount = 1;
    private int hitsDone;
    private bool strikeFromLeft;
    private bool damageApplied;

    // Für die Unity-Serialisierung.
    public BossStrikeAction()
    {
    }

    public BossStrikeAction(
        float windupDuration,
        float strikeDuration,
        float range,
        int damage)
    {
        this.windupDuration = windupDuration;
        this.strikeDuration = strikeDuration;
        this.range = range;
        this.damage = damage;
    }

    public override BossVisualState VisualState
    {
        get
        {
            if (step == Step.Windup)
            {
                return strikeFromLeft
                    ? BossVisualState.WindupLeft
                    : BossVisualState.WindupRight;
            }

            return strikeFromLeft
                ? BossVisualState.AttackLeft
                : BossVisualState.AttackRight;
        }
    }

    public override bool IsAttacking => true;

    public override float AttackProgress =>
        step == Step.Strike
            ? Mathf.Clamp01(timer / Boss.ScaleDuration(strikeDuration))
            : 0f;

    public void SetHitCount(int hits)
    {
        hitCount = Mathf.Max(1, hits);
    }

    public override void Enter()
    {
        hitsDone = 0;
        strikeFromLeft = UnityEngine.Random.value < 0.5f;

        BeginWindup(windupDuration);
    }

    public override bool Tick(float deltaTime)
    {
        timer += deltaTime;

        return step == Step.Windup
            ? TickWindup()
            : TickStrike(deltaTime);
    }

    public override void Exit()
    {
        hitCount = 1;
    }

    private void BeginWindup(float duration)
    {
        step = Step.Windup;
        timer = 0f;
        currentWindupDuration = duration;

        Movement.StopAndFace(Boss.CurrentTarget);
    }

    private bool TickWindup()
    {
        if (timer >= Boss.ScaleDuration(currentWindupDuration))
        {
            BeginStrike();
        }

        return false;
    }

    private void BeginStrike()
    {
        step = Step.Strike;
        timer = 0f;
        damageApplied = false;

        // Richtung festlegen: ab jetzt dreht sich der Boss nicht mehr mit.
        Movement.Stop();
        Movement.ResetRotationControl();

        Boss.PlayAttackSound();
    }

    private bool TickStrike(float deltaTime)
    {
        float duration = Boss.ScaleDuration(strikeDuration);
        float hitTime = duration * damageMoment;

        if (timer < hitTime && hitTime > 0f)
        {
            Movement.Dash(BossTransform.forward * (stepDistance / hitTime) * deltaTime);
        }

        if (!damageApplied && timer >= hitTime)
        {
            damageApplied = true;
            Boss.TryDealDamage(range, damage);
        }

        if (timer < duration)
        {
            return false;
        }

        hitsDone++;

        if (hitsDone >= hitCount)
        {
            return true;
        }

        strikeFromLeft = !strikeFromLeft;
        BeginWindup(windupDuration * followUpWindupFactor);

        return false;
    }
}
