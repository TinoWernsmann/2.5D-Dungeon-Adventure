using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Alle Erfolge in Anzeige-Reihenfolge. Liegt unter Assets/Resources/AchievementRegistry.asset.
/// </summary>
[CreateAssetMenu(fileName = "AchievementRegistry", menuName = "Achievements/Achievement Registry")]
public class AchievementRegistry : ScriptableObject
{
    public const string ResourcePath = "AchievementRegistry";

    [SerializeField] private List<AchievementData> achievements = new();

    [Tooltip("Schrift für das Popup \"Erfolg freigeschaltet\".")]
    [SerializeField] private TMP_FontAsset popupFont;

    [Tooltip("Optional: Sound beim Popup \"Erfolg freigeschaltet\".")]
    [SerializeField] private AudioClip popupSound;

    public IReadOnlyList<AchievementData> Achievements => achievements;
    public TMP_FontAsset PopupFont => popupFont;
    public AudioClip PopupSound => popupSound;
}
