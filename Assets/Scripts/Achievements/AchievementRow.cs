using System;
using TMPro;
using UnityEngine;

/// <summary>Eine Zeile in der Erfolgs-Liste: aufgedeckt mit Datum oder verborgen.</summary>
public class AchievementRow : MonoBehaviour
{
    private const float LockedAlpha = 0.45f;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private CanvasGroup canvasGroup;

    public void Show(AchievementData achievement)
    {
        bool isUnlocked = AchievementManager.IsUnlocked(achievement.Id, out DateTime unlockedAt);

        titleText.text = isUnlocked ? achievement.Title : "???";
        descriptionText.text = isUnlocked ? achievement.Description : "Verborgener Erfolg";
        dateText.text = isUnlocked ? $"Freigeschaltet am {unlockedAt:dd.MM.yyyy}" : GetProgressText(achievement);
        canvasGroup.alpha = isUnlocked ? 1f : LockedAlpha;
    }

    /// <summary>Zähler-Erfolge zeigen auch verborgen schon ihren Fortschritt (z.B. "Fortschritt 4 / 10").</summary>
    private static string GetProgressText(AchievementData achievement)
    {
        int progress = AchievementManager.GetProgress(achievement.Id);

        return achievement.ProgressTarget > 0 && progress > 0
            ? $"Fortschritt {Mathf.Min(progress, achievement.ProgressTarget)} / {achievement.ProgressTarget}"
            : string.Empty;
    }
}
