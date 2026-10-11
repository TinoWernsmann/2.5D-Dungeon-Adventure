using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Begrenzt, wie viele Gegner gleichzeitig angreifen dürfen.
/// Gegner ohne Angriffs-Slot umkreisen den Spieler und warten auf ihre Chance.
/// Dadurch bleiben Gruppenkämpfe lesbar und fair, statt dass alle gleichzeitig zuschlagen.
/// </summary>
public static class EnemyAttackCoordinator
{
    public const int MaxSimultaneousAttackers = 2;

    private static readonly HashSet<EnemyCombatBehaviour> attackers = new();

    public static bool TryAcquireSlot(EnemyCombatBehaviour attacker)
    {
        attackers.RemoveWhere(existing => existing == null);

        if (attackers.Contains(attacker))
        {
            return true;
        }

        if (attackers.Count >= MaxSimultaneousAttackers)
        {
            return false;
        }

        attackers.Add(attacker);
        return true;
    }

    public static void ReleaseSlot(EnemyCombatBehaviour attacker)
    {
        attackers.Remove(attacker);
    }

    // Statischer Zustand überlebt sonst einen Play-Mode-Neustart ohne Domain Reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlayModeStart()
    {
        attackers.Clear();
    }
}
