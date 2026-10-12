using System;
using UnityEngine;

public enum BossPhase
{
    PhaseOne,
    PhaseTwo,
    PhaseThree
}

/// <summary>
/// Verhalten des Bosses in einer Kampfphase. Spätere Phasen sind schneller,
/// aggressiver und hängen längere Kombos aneinander.
/// </summary>
[Serializable]
public class BossPhaseSettings
{
    [Range(0f, 1f)]
    [Tooltip("Grundchance, einen Treffer von vorne zu blocken.")]
    [SerializeField] private float blockChance;

    [Range(0f, 1f)]
    [Tooltip("Chance, seitlich auszuweichen und zu kontern statt direkt anzugreifen.")]
    [SerializeField] private float dodgeChance;

    [Range(0f, 1f)]
    [Tooltip("Chance, nach einem Angriff zurückzuweichen statt zu verschnaufen.")]
    [SerializeField] private float backstepChance;

    [Range(0f, 1f)]
    [Tooltip("Chance, aus mittlerer Distanz einen Sprungangriff zu machen.")]
    [SerializeField] private float lungeChance;

    [Min(1)]
    [Tooltip("Anzahl der Stiche in einer Kombo.")]
    [SerializeField] private int comboHits;

    [Min(0f)]
    [Tooltip("Bedenkzeit zwischen zwei Aktionen (kleiner = aggressiver).")]
    [SerializeField] private float decisionDelay;

    [Min(1f)]
    [SerializeField] private float speedMultiplier;

    [Min(1f)]
    [SerializeField] private float damageMultiplier;

    // Für die Unity-Serialisierung.
    public BossPhaseSettings()
    {
    }

    public BossPhaseSettings(
        float blockChance,
        float dodgeChance,
        float backstepChance,
        float lungeChance,
        int comboHits,
        float decisionDelay,
        float speedMultiplier,
        float damageMultiplier)
    {
        this.blockChance = blockChance;
        this.dodgeChance = dodgeChance;
        this.backstepChance = backstepChance;
        this.lungeChance = lungeChance;
        this.comboHits = comboHits;
        this.decisionDelay = decisionDelay;
        this.speedMultiplier = speedMultiplier;
        this.damageMultiplier = damageMultiplier;
    }

    public float BlockChance => blockChance;
    public float DodgeChance => dodgeChance;
    public float BackstepChance => backstepChance;
    public float LungeChance => lungeChance;
    public int ComboHits => comboHits;
    public float DecisionDelay => decisionDelay;
    public float SpeedMultiplier => speedMultiplier;
    public float DamageMultiplier => damageMultiplier;
}
