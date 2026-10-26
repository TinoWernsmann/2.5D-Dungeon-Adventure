using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

/// <summary>
/// Beschwört Verbündete (z.B. EasyAntibody) in der Nähe des Besitzers.
/// Fair gehalten: lange Ankündigung, Abklingzeit und eine harte Obergrenze lebender Diener.
/// Ohne Prefab ist die Fähigkeit deaktiviert.
/// </summary>
[Serializable]
public class MinionSummoner
{
    [Tooltip("Gegner, der beschworen wird. Leer = keine Beschwörung.")]
    [SerializeField] private EnemyBrain minionPrefab;

    [Tooltip("Ankündigung: So lange steht der Beschwörer still (Angriffsfenster für den Player).")]
    [Min(0.01f)]
    [SerializeField] private float castDuration = 1.6f;

    [Tooltip("Frühestens so viele Sekunden nach Kampfbeginn wird beschworen.")]
    [Min(0f)]
    [SerializeField] private float firstSummonDelay = 6f;

    [Min(0f)]
    [SerializeField] private float cooldown = 18f;

    [Tooltip("Abklingzeit, sobald der Beschwörer wütend ist (wenig Leben).")]
    [Min(0f)]
    [SerializeField] private float enragedCooldown = 12f;

    [Min(1)]
    [SerializeField] private int minionsPerSummon = 2;

    [Tooltip("Mehr Diener leben nie gleichzeitig.")]
    [Min(1)]
    [SerializeField] private int maxAliveMinions = 3;

    [Min(0.5f)]
    [SerializeField] private float spawnRadius = 2.5f;

    [SerializeField] private AudioClip summonSound;

    private readonly List<Health> aliveMinions = new();

    private Transform owner;
    private Health ownerHealth;
    private float cooldownTimer;

    public bool IsEnabled => minionPrefab != null;

    public float CastDuration => castDuration;

    public AudioClip SummonSound => summonSound;

    public bool CanSummon =>
        minionPrefab != null &&
        cooldownTimer <= 0f &&
        CountAliveMinions() < maxAliveMinions;

    public void Initialize(Transform summoner)
    {
        owner = summoner;
        ownerHealth = summoner.GetComponent<Health>();
    }

    public void Tick(float deltaTime)
    {
        cooldownTimer = Mathf.Max(0f, cooldownTimer - deltaTime);
    }

    /// <summary>Nicht sofort beim Kampfbeginn beschwören.</summary>
    public void StartCombat()
    {
        cooldownTimer = Mathf.Max(cooldownTimer, firstSummonDelay);
    }

    public void Summon(bool isEnraged)
    {
        cooldownTimer = isEnraged ? enragedCooldown : cooldown;

        int count = Mathf.Min(minionsPerSummon, maxAliveMinions - CountAliveMinions());

        // Zufälliger Startwinkel, danach gleichmäßig im Kreis verteilt.
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + i * 360f / count;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * spawnRadius;

            if (!NavMesh.SamplePosition(owner.position + offset, out NavMeshHit hit, spawnRadius, NavMesh.AllAreas))
            {
                continue;
            }

            EnemyBrain minion = Object.Instantiate(minionPrefab, hit.position, owner.rotation);
            minion.SetAlwaysAware(true);

            if (minion.TryGetComponent(out Health minionHealth))
            {
                aliveMinions.Add(minionHealth);
                minionHealth.Died += HandleMinionDied;
            }
        }
    }

    /// <summary>
    /// Ein Diener stirbt, während der Beschwörer noch lebt -> der Player hat ihn getötet
    /// (beim Tod des Beschwörers sterben die Diener über <see cref="KillAllMinions"/>, das zählt nicht).
    /// shortcut: trifft der Boss seinen eigenen Diener tödlich, zählt das auch; bei Bedarf den Angreifer prüfen.
    /// </summary>
    private void HandleMinionDied()
    {
        if (ownerHealth != null && !ownerHealth.IsDead)
        {
            LevelStats.Current?.RecordSummonKilled();
        }
    }

    /// <summary>Stirbt der Beschwörer, sterben auch seine Diener.</summary>
    public void KillAllMinions()
    {
        foreach (Health minion in aliveMinions)
        {
            if (minion != null && !minion.IsDead)
            {
                minion.TakeDamage(minion.CurrentHealth);
            }
        }

        aliveMinions.Clear();
    }

    private int CountAliveMinions()
    {
        aliveMinions.RemoveAll(minion => minion == null || minion.IsDead);
        return aliveMinions.Count;
    }
}
