using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Screen nach einem abgeschlossenen Level: Titel und benötigte Zeit für dieses Level,
/// danach weiter zum nächsten Level oder zurück ins Hauptmenü.
///
/// Level-Name und nächstes Level ergeben sich aus der zuletzt gespielten Szene:
/// "Level 1.2" -> "Level 1 erfolgreich abgeschlossen!" -> weiter mit "Level 2".
///
/// Wird per "Tools → Level Complete → Create Level Complete Scene" erstellt.
/// </summary>
public class LevelCompleteScreen : MonoBehaviour
{
    private const string LevelPrefix = "Level ";

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text difficultyText;

    [Header("Scenes")]
    [Tooltip("Wird geladen, wenn es kein nächstes Level gibt (z.B. am Ende des Spiels).")]
    [SerializeField] private string noNextLevelScene = "CreditScene";
    [SerializeField] private string mainMenuScene = "MainMenu";

    private string nextScene;

    private void Start()
    {
        // Im Spiel ist die Maus gesperrt – hier muss man klicken können.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        SaveManager saveManager = SaveManager.Instance;
        string levelName = SceneLoader.GetLevelName(saveManager != null ? saveManager.LastGameplayScene : null);

        titleText.text = string.IsNullOrEmpty(levelName)
            ? "Level erfolgreich abgeschlossen!"
            : $"{levelName} erfolgreich abgeschlossen!";

        TimeSpan time = TimeSpan.FromSeconds(saveManager != null ? saveManager.LevelPlayTimeSeconds : 0f);
        timeText.text = $"Zeit: {(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";

        LevelStats stats = LevelStats.Current;

        if (difficultyText != null && stats != null)
        {
            difficultyText.text = FormatDifficulty(stats);
        }

        AchievementManager.EvaluateLevel(levelName, stats, (float)time.TotalMinutes);

        nextScene = GetNextLevelScene(levelName);
    }

    /// <summary>Wurde die Stufe im Level geändert, zählt die leichteste benutzte.</summary>
    private static string FormatDifficulty(LevelStats stats)
    {
        string text = $"Schwierigkeit: {GetDifficultyName(stats.lowestDifficulty)}";

        return stats.difficultyChanged
            ? $"{text}\n<size=70%>Während des Levels geändert – es zählt die leichteste Stufe.</size>"
            : text;
    }

    private static string GetDifficultyName(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => "Einfach",
            Difficulty.Hard => "Schwer",
            _ => "Mittel"
        };
    }

    public void Continue()
    {
        SceneLoader.Load(nextScene);
    }

    public void GoToMainMenu()
    {
        SceneLoader.Load(mainMenuScene);
    }

    /// <summary>"Level 1" -> "Level 2", falls diese Szene in den Build Settings ist.</summary>
    private string GetNextLevelScene(string levelName)
    {
        if (!levelName.StartsWith(LevelPrefix) ||
            !int.TryParse(levelName.Substring(LevelPrefix.Length), out int levelNumber))
        {
            return noNextLevelScene;
        }

        string nextLevel = $"{LevelPrefix}{levelNumber + 1}";

        return Application.CanStreamedLevelBeLoaded(nextLevel)
            ? nextLevel
            : noNextLevelScene;
    }
}
