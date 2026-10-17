using System;
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

    [Tooltip("In der Reihenfolge Easy, Normal, Hard.")]
    [SerializeField] private Button[] difficultyButtons;

    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("Pause-Menü: \"Back\". Hauptmenü: \"Confirm\" – bestätigt die Wahl und startet das Spiel.")]
    [SerializeField] private Button closeButton;

    private static readonly string[] Names = { "Easy", "Normal", "Hard" };

    private static readonly string[] Descriptions =
    {
        "You take less damage and hit harder.",
        "The intended experience.",
        "Enemies hit much harder, your attacks are weaker."
    };

    /// <summary>Das Menü wurde geschlossen (z.B. um das Pause-Menü wieder zu zeigen).</summary>
    public event Action Closed;

    // Neues Spiel: nach dem Bestätigen weiter (z.B. zur Namenseingabe). Null = nur einstellen.
    private Action onConfirmed;

    public bool IsOpen => panel.activeSelf;

    private void Awake()
    {
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

    /// <summary>Die aktive Stufe wird markiert: "> Normal <".</summary>
    private void Refresh()
    {
        int current = (int)DifficultySettings.Current;

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            TMP_Text label = difficultyButtons[i].GetComponentInChildren<TMP_Text>();
            label.text = i == current ? $"> {Names[i]} <" : Names[i];
        }

        descriptionText.text = Descriptions[current];
    }
}
