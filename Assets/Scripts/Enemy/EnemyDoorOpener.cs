using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Lässt einen Gegner geschlossene (unverschlossene) Türen öffnen, auf die er zuläuft –
/// z.B. wenn der Player hinter sich eine Tür zuschlägt.
///
/// Arbeitet nur, solange eine Tür in der Nähe ist: Türen melden sich über ihre
/// <see cref="DoorProximityTrigger"/>-Zone an und ab. Ohne Tür in der Nähe kostet das nichts.
/// </summary>
[Serializable]
public class EnemyDoorOpener
{
    private const float CheckInterval = 0.2f;

    // Tür muss ungefähr in Laufrichtung liegen (nicht hinter oder neben dem Gegner).
    private const float MinDirectionDot = 0.3f;

    [SerializeField] private bool canOpenDoors = true;

    private readonly List<Door> nearbyDoors = new();
    private float checkTimer;

    public void SetDoorNearby(Door door, bool isNearby)
    {
        if (isNearby && !nearbyDoors.Contains(door))
        {
            nearbyDoors.Add(door);
        }
        else if (!isNearby)
        {
            nearbyDoors.Remove(door);
        }
    }

    public void Tick(Transform enemy, NavMeshAgent agent, float deltaTime)
    {
        if (!canOpenDoors || nearbyDoors.Count == 0)
        {
            return;
        }

        checkTimer -= deltaTime;

        if (checkTimer > 0f)
        {
            return;
        }

        checkTimer = CheckInterval;

        if (!TryGetMoveDirection(enemy, agent, out Vector3 moveDirection))
        {
            return;
        }

        foreach (Door door in nearbyDoors)
        {
            if (door.CanBeOpenedByEnemy && IsDoorAhead(enemy.position, moveDirection, door))
            {
                door.OpenByEnemy();
            }
        }
    }

    private static bool TryGetMoveDirection(Transform enemy, NavMeshAgent agent, out Vector3 direction)
    {
        // Nächster Wegpunkt des Pfads – zeigt schon zur Tür, bevor der Gegner dort ankommt.
        direction = agent.hasPath
            ? agent.steeringTarget - enemy.position
            : agent.velocity;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return false;
        }

        direction.Normalize();
        return true;
    }

    private static bool IsDoorAhead(Vector3 enemyPosition, Vector3 moveDirection, Door door)
    {
        Vector3 toDoor = door.Center - enemyPosition;
        toDoor.y = 0f;

        // Steht direkt in der Tür -> immer öffnen.
        if (toDoor.sqrMagnitude < 0.25f)
        {
            return true;
        }

        return Vector3.Dot(moveDirection, toDoor.normalized) >= MinDirectionDot;
    }
}
