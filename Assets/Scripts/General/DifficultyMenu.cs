using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fenster zur Wahl des Schwierigkeitsgrads (Pause- und Hauptmenü), im Stil des Save-Menüs.
/// Die Stufe gilt sofort und wird mit dem Spielstand gespeichert (<see cref="DifficultySettings"/>).
///
/// Wird per "Tools → Difficulty → Create Difficulty Menu In Scene" erstellt.
/// </summary>
public class DifficultyMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [Tooltip("In der Reihenfolge Easy, Normal, Hard (Hardcore wird bei Bedarf automatisch ergänzt).")]
    [SerializeField] private Button[] difficultyButtons;

    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("Pause-Menü: \"Back\". Hauptmenü: \"Confirm\" – bestätigt die Wahl und startet das Spiel.")]
    [SerializeField] private Button closeButton;

    private static readonly string[] Names = { "Easy", "Normal", "Hard", "Hardcore" };

    private static readonly string[] Descriptions =
    {
        "You take less damage and hit harder.",
        "The intended experience.",
        "Enemies hit much harder, your attacks are weaker.",
        "Like Hard - but only one life and no saving."
    };

    /// <summary>Hardcore wird freigeschaltet, sobald Level 1 auf Schwer geschafft wurde.</summary>
    private const string HardcoreUnlockAchievementId = "L1_Hard";

    /// <summary>Das Menü wurde geschlossen (z.B. um das Pause-Menü wieder zu zeigen).</summary>
    public event Action Closed;

    // Neues Spiel: nach dem Bestätigen weiter (z.B. zur Namenseingabe). Null = nur einstellen.
    private Action onConfirmed;

    public bool IsOpen => panel.activeSelf;

    private void Awake()
    {
        AddMissingButtons();

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            Difficulty difficulty = (Difficulty)i;
            difficultyButtons[i].onClick.AddListener(() => Select(difficulty));
        }

        closeButton.onClick.AddListener(Confirm);
        panel.SetActive(false);
    }

    /// <param name="confirmed">
    /// Optional (Spielstart): wird aufgerufen, wenn die Wahl mit dem Button bestätigt wird.
    /// Ohne Angabe schließt der Button nur das Menü (Pause-Menü).
    /// </param>
    public void Open(Action confirmed = null)
    {
        onConfirmed = confirmed;

        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        onConfirmed = null;
        panel.SetActive(false);
        Closed?.Invoke();
    }

    /// <summary>Ältere Menüs haben nur 3 Buttons: den Hardcore-Button als Kopie des letzten ergänzen.</summary>
    private void AddMissingButtons()
    {
        if (difficultyButtons.Length == 0 || difficultyButtons.Length >= Names.Length)
        {
            return;
        }

        List<Button> buttons = new(difficultyButtons);
        Button last = difficultyButtons[^1];

        while (buttons.Count < Names.Length)
        {
            Button copy = Instantiate(last, last.transform.parent);
            copy.transform.SetSiblingIndex(buttons[^1].transform.GetSiblingIndex() + 1);
            copy.onClick.RemoveAllListeners();
            buttons.Add(copy);
        }

        difficultyButtons = buttons.ToArray();
    }

    private static bool IsHardcoreUnlocked => AchievementManager.IsUnlocked(HardcoreUnlockAchievementId, out _);

    private void Select(Difficulty difficulty)
    {
        DifficultySettings.Current = difficulty;
        Refresh();
    }

    private void Confirm()
    {
        Action confirmed = onConfirmed;

        Close();
        confirmed?.Invoke();
    }

    /// <summary>
    /// Die aktive Stufe wird markiert: "> Normal <".
    /// Hardcore gibt es nur beim Spielstart (und erst nach Freischaltung) – und im Spiel kann man ihn nicht verlassen.
    /// </summary>
    private void Refresh()
    {
        int current = (int)DifficultySettings.Current;
        bool isStartingGame = onConfirmed != null;
        bool isLockedInHardcore = !isStartingGame && DifficultySettings.IsHardcore;

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            bool isHardcoreButton = (Difficulty)i == Difficulty.Hardcore;

            difficultyButtons[i].interactable = isHardcoreButton
                ? isStartingGame && IsHardcoreUnlocked
                : !isLockedInHardcore;

            TMP_Text label = difficultyButtons[i].GetComponentInChildren<TMP_Text>();
            string name = isHardcoreButton && !IsHardcoreUnlocked ? $"{Names[i]} (locked)" : Names[i];
            label.text = i == current ? $"> {name} <" : name;
        }

        descriptionText.text = Descriptions[Mathf.Clamp(current, 0, Descriptions.Length - 1)];
    }
}
