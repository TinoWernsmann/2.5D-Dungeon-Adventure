using UnityEngine;

/// <summary>
/// Ein Erfolg. Die Id ist der Asset-Name.
///
/// Level-Erfolge (Feld "Level" gesetzt) werden auf dem Level-Abschluss-Screen geprüft:
/// alle gesetzten Bedingungen müssen erfüllt sein. Ohne Level wird der Erfolg per Code
/// freigeschaltet (<see cref="AchievementManager.Unlock"/>).
/// Neue Erfolge: Asset anlegen und in die AchievementRegistry (Resources) ziehen.
/// </summary>
[CreateAssetMenu(fileName = "New Achievement", menuName = "Achievements/Achievement")]
public class AchievementData : ScriptableObject
{
    [SerializeField] private string title;

    [TextArea]
    [SerializeField] private string description;

    [Header("Level-Bedingungen")]
    [Tooltip("z.B. \"Level 1\" (= Level 1 + Level 1.2). Leer = wird per Code freigeschaltet.")]
    [SerializeField] private string level;

    [Tooltip("Das ganze Level mindestens auf dieser Stufe (es zählt die leichteste benutzte).")]
    [SerializeField] private Difficulty minDifficulty = Difficulty.Easy;

    [SerializeField] private bool requireNoDamage;
    [SerializeField] private bool requireAllItems;
    [SerializeField] private bool requireAllCrates;

    [Tooltip("Alle Bosse besiegt, ohne einen beschworenen Diener zu töten.")]
    [SerializeField] private bool requireBossesWithoutSummonKills;

    [Tooltip("Level in weniger als so vielen Minuten abschließen. 0 = egal.")]
    [Min(0f)]
    [SerializeField] private float maxMinutes;

    public string Id => name;
    public string Title => title;
    public string Description => description;
    public string Level => level;

    public bool IsMetBy(LevelStats stats, float minutes)
    {
        if (stats.lowestDifficulty < minDifficulty)
        {
            return false;
        }

        // Diese Werte werden erst seit Spielstand-Version 2 von Levelbeginn an gezählt.
        bool needsFullTracking =
            requireNoDamage || requireAllItems || requireAllCrates || requireBossesWithoutSummonKills;

        if (needsFullTracking && !stats.isTrackedFromStart)
        {
            return false;
        }

        return (!requireNoDamage || stats.damageTaken == 0) &&
               (!requireAllItems || stats.HasAllItems) &&
               (!requireAllCrates || stats.HasAllCrates) &&
               (!requireBossesWithoutSummonKills || stats.KilledAllBossesWithoutSummons) &&
               (maxMinutes <= 0f || minutes < maxMinutes);
    }
}
