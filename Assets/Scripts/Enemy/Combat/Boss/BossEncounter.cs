using System;
using UnityEngine;

/// <summary>
/// Der Boss-Kampf als Gruppe: Boss + Wachen (z.B. zwei Magier an seiner Seite).
///
/// - Die Wachen sind passiv, bis der Boss den Kampf beginnt (nach dem Intro)
///   oder der Player vorher einen aus der Gruppe angreift. Dann greifen alle Wachen an.
/// - Der Kampf ist erst abgeschlossen, wenn der Boss UND alle Wachen besiegt sind.
/// </summary>
[DisallowMultipleComponent]
public class BossEncounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpearBossCombatBehaviour boss;
    [SerializeField] private Health bossHealth;

    [Tooltip("Gegner, die zum Boss-Kampf gehören. Sie greifen erst an, " +
             "wenn der Player einen aus der Gruppe trifft.")]
    [SerializeField] private EnemyBrain[] guards;

    private Health[] guardHealths;

    private bool isCompleted;

    /// <summary>Der Kampf hat begonnen: Boss kämpft oder der Player hat jemanden angegriffen.</summary>
    public event Action Provoked;

    /// <summary>Boss und alle Wachen sind besiegt.</summary>
    public event Action Completed;

    public bool IsProvoked { get; private set; }

    private void Awake()
    {
        if (boss == null)
        {
            boss = GetComponent<SpearBossCombatBehaviour>();
        }

        if (bossHealth == null)
        {
            bossHealth = GetComponent<Health>();
        }

        guards ??= Array.Empty<EnemyBrain>();
        guardHealths = new Health[guards.Length];

        for (int i = 0; i < guards.Length; i++)
        {
            if (guards[i] == null)
            {
                continue;
            }

            guards[i].SetPassive(true);
            guardHealths[i] = guards[i].GetComponent<Health>();
        }
    }

    private void OnEnable()
    {
        if (boss != null)
        {
            boss.CombatStarted += Provoke;
        }

        Subscribe(bossHealth);

        foreach (Health guardHealth in guardHealths)
        {
            Subscribe(guardHealth);
        }
    }

    private void OnDisable()
    {
        if (boss != null)
        {
            boss.CombatStarted -= Provoke;
        }

        Unsubscribe(bossHealth);

        foreach (Health guardHealth in guardHealths)
        {
            Unsubscribe(guardHealth);
        }
    }

    private void Subscribe(Health memberHealth)
    {
        if (memberHealth == null)
        {
            return;
        }

        memberHealth.DamagedBy += HandleMemberAttacked;
        memberHealth.Died += HandleMemberDied;
    }

    private void Unsubscribe(Health memberHealth)
    {
        if (memberHealth == null)
        {
            return;
        }

        memberHealth.DamagedBy -= HandleMemberAttacked;
        memberHealth.Died -= HandleMemberDied;
    }

    private void HandleMemberAttacked(Transform attacker)
    {
        // Nur der Player zählt – nicht versehentliche Treffer unter Verbündeten.
        bool isAttackedByEnemy = attacker.GetComponentInParent<EnemyBrain>() != null;

        if (!isAttackedByEnemy)
        {
            Provoke();
        }
    }

    private void Provoke()
    {
        if (IsProvoked)
        {
            return;
        }

        IsProvoked = true;

        foreach (EnemyBrain guard in guards)
        {
            if (guard != null)
            {
                guard.Provoke();
            }
        }

        Provoked?.Invoke();
    }

    private void HandleMemberDied()
    {
        if (isCompleted || !AreAllMembersDefeated())
        {
            return;
        }

        isCompleted = true;
        Completed?.Invoke();
    }

    private bool AreAllMembersDefeated()
    {
        if (bossHealth != null && !bossHealth.IsDead)
        {
            return false;
        }

        // Besiegte Gegner werden nach einer Weile zerstört -> null zählt als besiegt.
        foreach (Health guardHealth in guardHealths)
        {
            if (guardHealth != null && !guardHealth.IsDead)
            {
                return false;
            }
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (guards == null)
        {
            return;
        }

        Gizmos.color = Color.magenta;

        foreach (EnemyBrain guard in guards)
        {
            if (guard != null)
            {
                Gizmos.DrawLine(transform.position, guard.transform.position);
            }
        }
    }
}
