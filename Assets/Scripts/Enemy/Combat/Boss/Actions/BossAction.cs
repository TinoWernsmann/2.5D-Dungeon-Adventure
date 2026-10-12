using System;
using UnityEngine;

/// <summary>
/// Eine abgeschlossene Aktion des Bosses (Kombo, Sprungangriff, Ausweichen, ...).
/// Der Boss (z.B. <see cref="SpearBossCombatBehaviour"/>) startet eine Aktion, ruft jeden Frame
/// <see cref="Tick"/> auf und entscheidet danach, was als Nächstes passiert.
/// </summary>
[Serializable]
public abstract class BossAction
{
    protected IBossActionContext Boss { get; private set; }

    protected EnemyMovement Movement => Boss.Movement;

    protected Transform BossTransform => Boss.Root;

    public abstract BossVisualState VisualState { get; }

    public virtual bool IsAttacking => false;

    public virtual float AttackProgress => 0f;

    /// <summary>Treffer von vorne werden während dieser Aktion automatisch geblockt.</summary>
    public virtual bool BlocksFrontalDamage => false;

    /// <summary>Während dieser Aktion nimmt der Boss gar keinen Schaden.</summary>
    public virtual bool IsInvulnerable => false;

    public void Bind(IBossActionContext boss)
    {
        Boss = boss;
    }

    public abstract void Enter();

    /// <returns>true, sobald die Aktion abgeschlossen ist.</returns>
    public abstract bool Tick(float deltaTime);

    public virtual void Exit()
    {
    }
}
