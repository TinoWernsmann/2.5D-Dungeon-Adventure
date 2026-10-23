using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Statistik des aktuellen Levels (z.B. "Level 1" = Level 1 + Level 1.2) für Erfolge
/// und den Abschluss-Screen. Wird mit dem Spielstand gespeichert.
/// </summary>
[Serializable]
public class LevelStats
{
    public Difficulty startDifficulty;
    public Difficulty lowestDifficulty;
    public bool difficultyChanged;

    [Tooltip("False bei alten Spielständen: Items/Kisten/Schaden wurden nicht von Anfang an gezählt.")]
    public bool isTrackedFromStart;

    public int damageTaken;
    public bool summonKilledByPlayer;

    public List<string> countedScenes = new();
    public List<string> knownItemIds = new();
    public List<string> collectedItemIds = new();
    public List<string> knownCrateIds = new();
    public List<string> destroyedCrateIds = new();

    // Ids statt Zähler: nach einem Neustart (Tod) stehen die Bosse wieder da und dürfen nicht doppelt zählen.
    public List<string> knownBossIds = new();
    public List<string> killedBossIds = new();

    /// <summary>Statistik des laufenden Levels (null außerhalb des Spiels).</summary>
    public static LevelStats Current =>
        SaveManager.Instance != null ? SaveManager.Instance.CurrentLevelStats : null;

    public bool HasAllItems => knownItemIds.All(collectedItemIds.Contains);

    public bool HasAllCrates => knownCrateIds.All(destroyedCrateIds.Contains);

    public bool KilledAllBossesWithoutSummons =>
        knownBossIds.Count > 0 &&
        knownBossIds.All(killedBossIds.Contains) &&
        !summonKilledByPlayer;

    public static LevelStats Create(Difficulty difficulty, bool trackedFromStart)
    {
        return new LevelStats
        {
            startDifficulty = difficulty,
            lowestDifficulty = difficulty,
            isTrackedFromStart = trackedFromStart
        };
    }

    /// <summary>Wird die Stufe im Level geändert, zählt die leichteste benutzte.</summary>
    public void TrackDifficulty(Difficulty current)
    {
        if (current != startDifficulty)
        {
            difficultyChanged = true;
        }

        if (current < lowestDifficulty)
        {
            lowestDifficulty = current;
        }
    }

    public void RecordDamage(int damage)
    {
        damageTaken += damage;
    }

    public void RecordItemCollected(Component item)
    {
        if (TryGetSceneId(item, out string id))
        {
            AddUnique(collectedItemIds, id);
        }
    }

    public void RecordCrateDestroyed(Component crate)
    {
        if (TryGetSceneId(crate, out string id))
        {
            AddUnique(destroyedCrateIds, id);
        }
    }

    public void RecordBossKilled(Component boss)
    {
        if (TryGetSceneId(boss, out string id))
        {
            AddUnique(killedBossIds, id);
        }
    }

    public void RecordSummonKilled()
    {
        summonKilledByPlayer = true;
    }

    /// <summary>Merkt sich alle Items, Kisten und Bosse einer Szene – jede Szene nur einmal pro Level.</summary>
    public void RegisterScene(string sceneName)
    {
        if (countedScenes.Contains(sceneName))
        {
            return;
        }

        countedScenes.Add(sceneName);

        foreach (ItemBase item in UnityEngine.Object.FindObjectsByType<ItemBase>(FindObjectsInactive.Exclude))
        {
            if (TryGetSceneId(item, out string id)) AddUnique(knownItemIds, id);
        }

        foreach (DestructibleCrate crate in UnityEngine.Object.FindObjectsByType<DestructibleCrate>(FindObjectsInactive.Exclude))
        {
            if (TryGetSceneId(crate, out string id)) AddUnique(knownCrateIds, id);
        }

        foreach (MageCombatBehaviour mage in UnityEngine.Object.FindObjectsByType<MageCombatBehaviour>(FindObjectsInactive.Exclude))
        {
            if (mage.IsSummoner && TryGetSceneId(mage, out string id)) AddUnique(knownBossIds, id);
        }
    }

    /// <summary>Nur Objekte, die fest in der Szene liegen (mit Save-Id), nicht zur Laufzeit erzeugte.</summary>
    private static bool TryGetSceneId(Component component, out string id)
    {
        id = component != null && component.TryGetComponent(out SaveableEntity entity) && entity.HasId
            ? entity.Id
            : null;

        return id != null;
    }

    private static void AddUnique(List<string> list, string id)
    {
        if (!list.Contains(id))
        {
            list.Add(id);
        }
    }
}
