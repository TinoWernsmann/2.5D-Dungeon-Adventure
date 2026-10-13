using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Liest und schreibt Spielstände als JSON-Dateien in Application.persistentDataPath.
/// (Windows: %USERPROFILE%/AppData/LocalLow/&lt;Firma&gt;/&lt;Spiel&gt;/Saves)
/// </summary>
public static class SaveFileStorage
{
    private const string FolderName = "Saves";
    private const string FilePrefix = "slot_";
    private const string FileExtension = ".json";

    public static string SaveFolder =>
        Path.Combine(Application.persistentDataPath, FolderName);

    public static bool Exists(int slot)
    {
        return File.Exists(GetPath(slot));
    }

    public static void Write(int slot, SaveGameData data)
    {
        Directory.CreateDirectory(SaveFolder);

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        string path = GetPath(slot);
        string tempPath = path + ".tmp";

        // Erst in eine temporäre Datei schreiben, dann ersetzen:
        // Ein Absturz beim Speichern zerstört so nie den alten Spielstand.
        File.WriteAllText(tempPath, json);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        File.Move(tempPath, path);
    }

    public static bool TryRead(int slot, out SaveGameData data)
    {
        data = null;
        string path = GetPath(slot);

        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(path));
            return data != null;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Save] Slot {slot} ist beschädigt: {exception.Message}");
            return false;
        }
    }

    public static void Delete(int slot)
    {
        string path = GetPath(slot);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string GetPath(int slot)
    {
        return Path.Combine(SaveFolder, FilePrefix + slot + FileExtension);
    }
}
