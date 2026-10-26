using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Liste aller Erfolge (Pause- und Hauptmenü). Gesperrte bleiben verborgen,
/// freigeschaltete werden mit Datum angezeigt. Die Zeilen entstehen aus einer Vorlage,
/// damit neue Erfolge ohne Umbau des Menüs erscheinen.
///
/// Wird per "Tools → Achievements → Create Achievement Menu In Scene" erstellt.
/// </summary>
public class AchievementMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private Transform rowParent;
    [SerializeField] private AchievementRow rowTemplate;
    [SerializeField] private Button closeButton;

    private readonly List<AchievementRow> rows = new();

    /// <summary>Das Menü wurde geschlossen (z.B. um das Pause-Menü wieder zu zeigen).</summary>
    public event Action Closed;

    public bool IsOpen => panel.activeSelf;

    private void Awake()
    {
        closeButton.onClick.AddListener(Close);
        rowTemplate.gameObject.SetActive(false);
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
        IReadOnlyList<AchievementData> achievements = AchievementManager.All;

        while (rows.Count < achievements.Count)
        {
            rows.Add(Instantiate(rowTemplate, rowParent));
        }

        for (int i = 0; i < rows.Count; i++)
        {
            bool hasAchievement = i < achievements.Count && achievements[i] != null;
            rows[i].gameObject.SetActive(hasAchievement);

            if (hasAchievement)
            {
                rows[i].Show(achievements[i]);
            }
        }

        headerText.text = $"{AchievementManager.UnlockedCount} / {achievements.Count} freigeschaltet";
    }
}
