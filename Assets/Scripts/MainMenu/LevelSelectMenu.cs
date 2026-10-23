using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level-Auswahl im Hauptmenü. Ein Level ist spielbar, sobald seine erste Szene ("Level N")
/// in den Build Settings steht – neue Levels erscheinen also automatisch, ohne Code-Änderung.
///
/// Wird per "Tools → Level Select → Create Level Select Menu In Scene" erstellt.
/// </summary>
public class LevelSelectMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private Button buttonTemplate;
    [SerializeField] private Button closeButton;

    [Tooltip("So viele Levels werden angezeigt (noch nicht gebaute als \"Bald verfügbar\").")]
    [Min(1)]
    [SerializeField] private int levelCount = 6;

    private const float ButtonHeightWithProgress = 90f;

    private readonly List<Button> buttons = new();

    /// <summary>Ein Level wurde gewählt (Nummer ab 1). Das Menü ist dann schon ausgeblendet.</summary>
    public event Action<int> LevelChosen;

    /// <summary>Das Menü wurde ohne Auswahl geschlossen (Zurück).</summary>
    public event Action Closed;

    public bool IsOpen => panel.activeSelf;

    public static string GetLevelSceneName(int levelNumber) => $"Level {levelNumber}";

    public static bool IsLevelAvailable(int levelNumber) =>
        Application.CanStreamedLevelBeLoaded(GetLevelSceneName(levelNumber));

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        buttonTemplate.gameObject.SetActive(false);
        panel.SetActive(false);
    }

    public void Open()
    {
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        panel.SetActive(false);
        Closed?.Invoke();
    }

    private void Refresh()
    {
        while (buttons.Count < levelCount)
        {
            int levelNumber = buttons.Count + 1;

            Button button = Instantiate(buttonTemplate, buttonParent);
            button.onClick.AddListener(() => Choose(levelNumber));
            buttons.Add(button);

            // Zweizeilig (Name + Fortschritt) braucht mehr Höhe als die Vorlage.
            if (button.TryGetComponent(out LayoutElement layout))
            {
                layout.preferredHeight = Mathf.Max(layout.preferredHeight, ButtonHeightWithProgress);
            }
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            int levelNumber = i + 1;
            bool isAvailable = IsLevelAvailable(levelNumber);

            buttons[i].gameObject.SetActive(levelNumber <= levelCount);
            buttons[i].interactable = isAvailable;
            buttons[i].GetComponentInChildren<TMP_Text>().text = isAvailable
                ? $"{GetLevelSceneName(levelNumber)}\n<size=70%>{FormatProgress(GetLevelSceneName(levelNumber))}</size>"
                : $"{GetLevelSceneName(levelNumber)} - Bald verfügbar";
        }
    }

    /// <summary>"Erfolge 7/12 · Schwer · 12:34" – zeigt, was in diesem Level noch zu holen ist.</summary>
    private static string FormatProgress(string levelName)
    {
        AchievementManager.CountForLevel(levelName, out int unlocked, out int total);
        AchievementManager.LevelRecord record = AchievementManager.GetLevelRecord(levelName);

        string achievements = $"Erfolge {unlocked}/{total}";

        if (record == null)
        {
            return $"{achievements} · noch nicht abgeschlossen";
        }

        TimeSpan time = TimeSpan.FromSeconds(record.bestTimeSeconds);

        return $"{achievements} · {DifficultySettings.GetDisplayName(record.bestDifficulty)} · " +
               $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
    }

    private void Choose(int levelNumber)
    {
        // Ohne "Closed": das Hauptmenü bleibt ausgeblendet, es geht mit der Schwierigkeitswahl weiter.
        panel.SetActive(false);
        LevelChosen?.Invoke(levelNumber);
    }
}
