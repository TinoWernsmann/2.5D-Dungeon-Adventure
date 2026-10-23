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
        dateText.text = isUnlocked ? $"Freigeschaltet am {unlockedAt:dd.MM.yyyy}" : string.Empty;
        canvasGroup.alpha = isUnlocked ? 1f : LockedAlpha;
    }
}
