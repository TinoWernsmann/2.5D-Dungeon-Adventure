using UnityEngine;

public enum Difficulty
{
    Easy,
    Normal,
    Hard
}

/// <summary>
/// Schwierigkeitsgrad über Schadens-Multiplikatoren. Wirkt nur auf neu entstehenden Schaden –
/// deshalb jederzeit umschaltbar, ohne HP-Werte oder laufende Kämpfe zu verfälschen.
/// Statisch, damit die Stufe szenenübergreifend gilt – auch beim direkten Start eines Levels im Editor.
/// Wird mit dem Spielstand gespeichert (<see cref="SaveGameData.difficulty"/>).
/// </summary>
public static class DifficultySettings
{
    public static Difficulty Current { get; set; } = Difficulty.Normal;

    /// <summary>Schaden, den der Spieler austeilt.</summary>
    public static int DamageByPlayer(int damage)
    {
        return Scale(damage, Current switch
        {
            Difficulty.Easy => 1.25f,
            Difficulty.Hard => 0.85f,
            _ => 1f
        });
    }

    /// <summary>Schaden eines Gegners – skaliert nur, wenn er den Spieler trifft (nicht Verbündete).</summary>
    public static int DamageByEnemy(int damage, Component target)
    {
        if (target == null || !target.CompareTag("Player"))
        {
            return damage;
        }

        return Scale(damage, Current switch
        {
            Difficulty.Easy => 0.6f,
            Difficulty.Hard => 1.5f,
            _ => 1f
        });
    }

    private static int Scale(int damage, float multiplier)
    {
        return damage <= 0 ? damage : Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
    }
}
