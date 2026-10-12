using System;
using UnityEngine;

/// <summary>
/// Der Boss verharrt für eine bestimmte Zeit in einer Pose.
/// Wird genutzt für: Kampfhaltung einnehmen, Blocken, Verschnaufen (Angriffsfenster
/// für den Player) und den Phasenwechsel.
/// </summary>
[Serializable]
public class BossPoseAction : BossAction
{
    [Min(0.01f)]
    [SerializeField] private float duration = 0.5f;

    [SerializeField] private BossVisualState visual = BossVisualState.CombatIdle;

    [Tooltip("Dreht sich der Boss währenddessen zum Player? " +
             "Wenn nicht, kann der Player ihn umrunden und von hinten treffen.")]
    [SerializeField] private bool facesTarget = true;

    [Tooltip("Treffer von vorne werden automatisch geblockt.")]
    [SerializeField] private bool blocksFrontalDamage;

    [Tooltip("Der Boss nimmt währenddessen keinen Schaden.")]
    [SerializeField] private bool isInvulnerable;

    private float timer;

    // Für die Unity-Serialisierung.
    public BossPoseAction()
    {
    }

    public BossPoseAction(
        float duration,
        BossVisualState visual,
        bool facesTarget = true,
        bool blocksFrontalDamage = false,
        bool isInvulnerable = false)
    {
        this.duration = duration;
        this.visual = visual;
        this.facesTarget = facesTarget;
        this.blocksFrontalDamage = blocksFrontalDamage;
        this.isInvulnerable = isInvulnerable;
    }

    public override BossVisualState VisualState => visual;

    public override bool BlocksFrontalDamage => blocksFrontalDamage;

    public override bool IsInvulnerable => isInvulnerable;

    public override void Enter()
    {
        timer = 0f;

        Movement.Stop();

        if (facesTarget)
        {
            Movement.FaceTarget(Boss.CurrentTarget);
        }
        else
        {
            Movement.ResetRotationControl();
        }
    }

    public override bool Tick(float deltaTime)
    {
        timer += deltaTime;

        return timer >= Boss.ScaleDuration(duration);
    }
}
