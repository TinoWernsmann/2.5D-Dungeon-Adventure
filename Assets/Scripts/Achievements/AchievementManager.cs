using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Freigeschaltete Erfolge – unabhängig vom Spielstand in einer eigenen Datei
/// (achievements.json in Application.persistentDataPath), damit sie über neue Spiele erhalten bleiben.
/// </summary>
public static class AchievementManager
{
    public const string PushIntoAcidId = "PushIntoAcid";

    private const string FileName = "achievements.json";

    [Serializable]
    private class UnlockedEntry
    {
        public string id;
        public string unlockedAtIso;
    }

    [Serializable]
    private class SaveData
    {
        public List<UnlockedEntry> unlocked = new();
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

        Debug.Log($"[Achievements] Freigeschaltet: {achievement.Title}");
        Unlocked?.Invoke(achievement);
    }

    /// <summary>Prüft beim Level-Abschluss alle Erfolge dieses Levels.</summary>
    public static void EvaluateLevel(string levelName, LevelStats stats, float minutes)
    {
        if (stats == null)
        {
            return;
        }

        foreach (AchievementData achievement in All)
        {
            if (achievement != null && achievement.Level == levelName && achievement.IsMetBy(stats, minutes))
            {
                Unlock(achievement.Id);
            }
        }
    }

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
