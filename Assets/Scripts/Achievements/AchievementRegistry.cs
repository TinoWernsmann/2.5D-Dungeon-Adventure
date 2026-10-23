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

    public IReadOnlyList<AchievementData> Achievements => achievements;
    public TMP_FontAsset PopupFont => popupFont;
}
