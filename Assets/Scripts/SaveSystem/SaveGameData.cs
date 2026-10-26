using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Ein kompletter Spielstand – wird als JSON-Datei gespeichert.</summary>
[Serializable]
public class SaveGameData
{
    public const int CurrentVersion = 2; // 2: Level-Statistik für Erfolge

    public int version = CurrentVersion;

    // Info für das Slot-Menü
    public string sceneName;
    public string playerName;
    public Difficulty difficulty = Difficulty.Normal; // fehlt in alten Spielständen -> Normal
    public string questTitle;
    public string savedAtIso;
    public float playTimeSeconds;
    public float levelStartPlayTimeSeconds; // fehlt in alten Spielständen -> 0 (Levelzeit = Gesamtzeit)
    public LevelStats levelStats;

    // Zustand der Welt
    public List<EntitySaveData> entities = new();
    public List<string> destroyedEntityIds = new();
    public List<DroppedItemSaveData> droppedItems = new();

    public DateTime SavedAt =>
        DateTime.TryParse(savedAtIso, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime date)
            ? date
            : DateTime.MinValue;
}

/// <summary>Zustand eines Objekts mit <see cref="SaveableEntity"/>.</summary>
[Serializable]
public class EntitySaveData
{
    public string id;
    public bool isActive;
    public List<ComponentSaveData> components = new();
}

/// <summary>Zustand einer einzelnen <see cref="ISaveable"/>-Komponente.</summary>
[Serializable]
public class ComponentSaveData
{
    public string type;
    public string state;
}

/// <summary>Ein Item, das zur Laufzeit in die Welt gekommen ist (Truhen-Loot, gedroppt, ...).</summary>
[Serializable]
public class DroppedItemSaveData
{
    public string itemId;
    public int amount;
    public Vector3 position;
}
