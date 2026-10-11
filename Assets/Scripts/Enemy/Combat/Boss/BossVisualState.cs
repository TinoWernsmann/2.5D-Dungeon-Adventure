/// <summary>
/// Was der Boss gerade tut – der <see cref="BossVisualController"/> wählt daraus die Sprites.
/// Idle, Walk und CombatIdle werden zusätzlich abhängig vom Blickwinkel der Kamera
/// (vorne / hinten / links / rechts) dargestellt.
/// </summary>
public enum BossVisualState
{
    Idle,
    Walk,

    PrepareCombat,
    CombatIdle,

    WindupLeft,
    WindupRight,

    AttackLeft,
    AttackRight,

    Block,

    Defeated
}
