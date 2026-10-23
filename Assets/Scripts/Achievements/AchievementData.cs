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
    public enum UnlockTrigger
    {
        [Tooltip("Am Level-Ende anhand der Bedingungen unten geprüft.")]
        LevelEnd,

        [Tooltip("Wird im Spiel per Code freigeschaltet (z.B. Boss-Erfolge). Das Level dient nur der Anzeige.")]
        Code
    }

    [SerializeField] private string title;

    [TextArea]
    [SerializeField] private string description;

    [SerializeField] private UnlockTrigger trigger = UnlockTrigger.LevelEnd;

    [Tooltip("Zähler-Erfolg (z.B. 10 Gegner in die Säure): Ziel des Zählers. 0 = kein Zähler.")]
    [Min(0)]
    [SerializeField] private int progressTarget;

    [Header("Level-Bedingungen")]
    [Tooltip("z.B. \"Level 1\" (= Level 1 + Level 1.2). Leer = gehört zu keinem Level.")]
    [SerializeField] private string level;

    [Tooltip("Das ganze Level mindestens auf dieser Stufe (es zählt die leichteste benutzte).")]
    [SerializeField] private Difficulty minDifficulty = Difficulty.Easy;

    [SerializeField] private bool requireNoDamage;
    [SerializeField] private bool requireAllItems;
    [SerializeField] private bool requireAllCrates;

    [Tooltip("Alle Bosse besiegt, ohne einen beschworenen Diener zu töten.")]
    [SerializeField] private bool requireBossesWithoutSummonKills;

    [Tooltip("Alle Bosse des Levels per Rückstoß in die Säure gestoßen.")]
    [SerializeField] private bool requireAllBossesPushedIntoAcid;

    [Tooltip("Nur mit Fäusten angegriffen.")]
    [SerializeField] private bool requireFistsOnly;

    [Tooltip("Kein Heiltrank benutzt.")]
    [SerializeField] private bool requireNoHealing;

    [Tooltip("Kein Tod und kein Spielstand geladen.")]
    [SerializeField] private bool requireNoDeathOrLoad;

    [Tooltip("Level in weniger als so vielen Minuten abschließen. 0 = egal.")]
    [Min(0f)]
    [SerializeField] private float maxMinutes;

    public string Id => name;
    public string Title => title;
    public string Description => description;
    public string Level => level;
    public int ProgressTarget => progressTarget;

    /// <summary>Wird am Ende dieses Levels automatisch geprüft.</summary>
    public bool IsEvaluatedAtEndOf(string levelName) =>
        trigger == UnlockTrigger.LevelEnd && progressTarget == 0 && level == levelName;

    public bool IsMetBy(LevelStats stats, float minutes)
    {
        if (stats.lowestDifficulty < minDifficulty)
        {
            return false;
        }

        // Diese Werte werden erst seit Spielstand-Version 2 von Levelbeginn an gezählt.
        bool needsFullTracking =
            requireNoDamage || requireAllItems || requireAllCrates ||
            requireBossesWithoutSummonKills || requireAllBossesPushedIntoAcid;

        if (needsFullTracking && !stats.isTrackedFromStart)
        {
            return false;
        }

        // Spielweise (Waffen, Heilung, Tode, Laden) wird erst seit diesem Update gezählt.
        bool needsPlayStyleTracking = requireFistsOnly || requireNoHealing || requireNoDeathOrLoad;

        if (needsPlayStyleTracking && !stats.tracksPlayStyle)
        {
            return false;
        }

        return (!requireNoDamage || stats.damageTaken == 0) &&
               (!requireFistsOnly || !stats.usedWeapon) &&
               (!requireNoHealing || !stats.healed) &&
               (!requireNoDeathOrLoad || (stats.deaths == 0 && !stats.loadedSave)) &&
               (!requireAllItems || stats.HasAllItems) &&
               (!requireAllCrates || stats.HasAllCrates) &&
               (!requireBossesWithoutSummonKills || stats.KilledAllBossesWithoutSummons) &&
               (!requireAllBossesPushedIntoAcid || stats.PushedAllBossesIntoAcid) &&
               (maxMinutes <= 0f || minutes < maxMinutes);
    }
}
