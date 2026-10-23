using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Spieler-Fortschritt, der über alle Spielstände erhalten bleibt (achievements.json in
/// Application.persistentDataPath): freigeschaltete Erfolge, Zähler (z.B. "10 Gegner in die Säure")
/// und der beste Abschluss jedes Levels (Schwierigkeit, Zeit).
/// </summary>
public static class AchievementManager
{
    public const string PushIntoAcidId = "PushIntoAcid";
    public const string AcidRainId = "AcidRain";

    private const string FileName = "achievements.json";

    [Serializable]
    private class UnlockedEntry
    {
        public string id;
        public string unlockedAtIso;
    }

    [Serializable]
    private class CounterEntry
    {
        public string id;
        public int value;
    }

    [Serializable]
    public class LevelRecord
    {
        public string level;
        public Difficulty bestDifficulty;
        public float bestTimeSeconds;
    }

    [Serializable]
    private class SaveData
    {
        public List<UnlockedEntry> unlocked = new();
        public List<CounterEntry> counters = new();
        public List<LevelRecord> levels = new();
    }

    private static SaveData data;
    private static AchievementRegistry registry;

    /// <summary>Ein Erfolg wurde gerade freigeschaltet (z.B. für das Popup).</summary>
    public static event Action<AchievementData> Unlocked;

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    private static SaveData Data => data ??= LoadData();

    public static AchievementRegistry Registry =>
        registry != null ? registry : registry = Resources.Load<AchievementRegistry>(AchievementRegistry.ResourcePath);

    public static IReadOnlyList<AchievementData> All =>
        Registry != null ? Registry.Achievements : Array.Empty<AchievementData>();

    public static bool IsUnlocked(string id, out DateTime unlockedAt)
    {
        UnlockedEntry entry = Data.unlocked.Find(e => e.id == id);

        unlockedAt = entry != null &&
                     DateTime.TryParse(entry.unlockedAtIso, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime date)
            ? date
            : DateTime.MinValue;

        return entry != null;
    }

    public static int UnlockedCount => Data.unlocked.Count;

    /// <summary>Schaltet einen Erfolg frei (mehrfaches Aufrufen schadet nicht).</summary>
    public static void Unlock(string id)
    {
        if (string.IsNullOrEmpty(id) || IsUnlocked(id, out _))
        {
            return;
        }

        AchievementData achievement = Find(id);

        if (achievement == null)
        {
            Debug.LogWarning($"[Achievements] Unbekannter Erfolg '{id}' – fehlt er in der AchievementRegistry?");
            return;
        }

        Data.unlocked.Add(new UnlockedEntry { id = id, unlockedAtIso = DateTime.Now.ToString("o") });
        WriteData();

        LevelStats.Current?.RecordAchievementUnlocked(id);

        Debug.Log($"[Achievements] Freigeschaltet: {achievement.Title}");
        Unlocked?.Invoke(achievement);
    }

    /// <summary>Prüft beim Level-Abschluss alle Erfolge dieses Levels. Gibt die Anzahl neu freigeschalteter zurück.</summary>
    public static int EvaluateLevel(string levelName, LevelStats stats, float minutes)
    {
        if (stats == null)
        {
            return 0;
        }

        int before = UnlockedCount;

        foreach (AchievementData achievement in All)
        {
            if (achievement != null && achievement.IsEvaluatedAtEndOf(levelName) && achievement.IsMetBy(stats, minutes))
            {
                Unlock(achievement.Id);
            }
        }

        return UnlockedCount - before;
    }

    #region Zähler

    public static int GetProgress(string id)
    {
        CounterEntry entry = Data.counters.Find(e => e.id == id);
        return entry != null ? entry.value : 0;
    }

    /// <summary>Erhöht einen Zähler; erreicht er das Ziel des gleichnamigen Erfolgs, wird dieser freigeschaltet.</summary>
    public static void AddProgress(string id, int amount = 1)
    {
        CounterEntry entry = Data.counters.Find(e => e.id == id);

        if (entry == null)
        {
            entry = new CounterEntry { id = id };
            Data.counters.Add(entry);
        }

        entry.value += amount;
        WriteData();

        AchievementData achievement = Find(id);

        if (achievement != null && achievement.ProgressTarget > 0 && entry.value >= achievement.ProgressTarget)
        {
            Unlock(id);
        }
    }

    #endregion

    #region Level-Bestleistungen

    public static LevelRecord GetLevelRecord(string levelName) =>
        Data.levels.Find(record => record.level == levelName);

    /// <summary>Merkt sich die schwerste geschaffte Stufe und (unabhängig davon) die schnellste Zeit.</summary>
    public static void RecordLevelCompleted(string levelName, Difficulty difficulty, float seconds)
    {
        if (string.IsNullOrEmpty(levelName))
        {
            return;
        }

        LevelRecord record = GetLevelRecord(levelName);

        if (record == null)
        {
            Data.levels.Add(new LevelRecord { level = levelName, bestDifficulty = difficulty, bestTimeSeconds = seconds });
            WriteData();
            return;
        }

        if (difficulty > record.bestDifficulty)
        {
            record.bestDifficulty = difficulty;
        }

        if (seconds < record.bestTimeSeconds)
        {
            record.bestTimeSeconds = seconds;
        }

        WriteData();
    }

    /// <summary>Freigeschaltete / alle Erfolge eines Levels (z.B. für die Level-Auswahl).</summary>
    public static void CountForLevel(string levelName, out int unlocked, out int total)
    {
        unlocked = 0;
        total = 0;

        foreach (AchievementData achievement in All)
        {
            if (achievement == null || achievement.Level != levelName)
            {
                continue;
            }

            total++;

            if (IsUnlocked(achievement.Id, out _))
            {
                unlocked++;
            }
        }
    }

    #endregion

    private static AchievementData Find(string id)
    {
        foreach (AchievementData achievement in All)
        {
            if (achievement != null && achievement.Id == id)
            {
                return achievement;
            }
        }

        return null;
    }

    private static SaveData LoadData()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) ?? new SaveData();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Achievements] Datei konnte nicht gelesen werden: {exception.Message}");

            // Kaputte Datei sichern, statt sie beim nächsten Freischalten zu überschreiben.
            try { File.Copy(FilePath, FilePath + ".bak", overwrite: true); } catch (Exception) { }
        }

        return new SaveData();
    }

    private static void WriteData()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[Achievements] Datei konnte nicht gespeichert werden: {exception.Message}");
        }
    }
}
