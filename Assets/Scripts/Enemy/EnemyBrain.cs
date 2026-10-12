using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Übergeordnete KI eines Gegners: Patrouillieren -> Verfolgen -> Kämpfen -> Suchen.
/// WIE gekämpft wird, entscheidet das <see cref="EnemyCombatBehaviour"/>.
///
/// Zusätzlich:
/// - Wird der Gegner getroffen, dreht er sich zum Angreifer und greift an.
/// - Entdeckt er den Player, alarmiert er Verbündete in der Nähe.
/// - Verliert er den Player, sucht er ihn (siehe <see cref="EnemySearch"/>).
/// - Passiv (z.B. Wachen eines Bosses): ignoriert den Player, bis er provoziert wird.
/// - "Immer aufmerksam" (z.B. im Boss-Kampf): kennt die Position des Players auch ohne Sicht.
/// </summary>
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyPatrol))]
[RequireComponent(typeof(EnemyPerception))]
public class EnemyBrain : MonoBehaviour
{
    private enum EnemyState
    {
        None,
        Patrol,
        Chase,
        Combat,
        Search
    }

    private static readonly List<EnemyBrain> activeBrains = new();

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Combat")]
    [SerializeField] private EnemyCombatBehaviour combatBehaviour;

    [Header("Search")]
    [SerializeField] private EnemySearch search = new();

    [Header("Alert")]
    [Tooltip("Verbündete in diesem Radius werden alarmiert, wenn der Player entdeckt wird.")]
    [Min(0f)]
    [SerializeField] private float allyAlertRadius = 8f;

    [Tooltip("So lange kennt der Gegner die Position des Players, nachdem er getroffen wurde.")]
    [Min(0f)]
    [SerializeField] private float damageAwarenessDuration = 2f;

    private EnemyMovement movement;
    private EnemyPatrol patrol;
    private EnemyPerception perception;
    private Health ownHealth;
    private Health playerHealth;

    private EnemyState currentState = EnemyState.None;

    private Vector3 lastKnownPlayerPosition;
    private Vector3 lastKnownPlayerVelocity;

    /// <summary>Passiv = greift nicht von selbst an, nur wenn er provoziert wird.</summary>
    public bool IsPassive { get; private set; }

    /// <summary>Kennt die Position des Players immer, auch ohne Sichtkontakt (z.B. im Boss-Kampf).</summary>
    public bool IsAlwaysAware { get; private set; }

    private bool KnowsWherePlayerIs => IsAlwaysAware || perception.IsAwareOfPlayer;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        patrol = GetComponent<EnemyPatrol>();
        perception = GetComponent<EnemyPerception>();
        ownHealth = GetComponent<Health>();

        if (combatBehaviour == null)
        {
            combatBehaviour = GetComponent<EnemyCombatBehaviour>();
        }

        search.Initialize(transform, movement, perception);
    }

    private void OnEnable()
    {
        activeBrains.Add(this);

        if (ownHealth != null)
        {
            ownHealth.DamagedBy += HandleDamagedBy;
        }
    }

    private void OnDisable()
    {
        activeBrains.Remove(this);

        if (ownHealth != null)
        {
            ownHealth.DamagedBy -= HandleDamagedBy;
        }
    }

    private void Start()
    {
        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        ChangeState(EnemyState.Patrol);
    }

    private void Update()
    {
        if (playerHealth == null || playerHealth.IsDead)
        {
            movement.Stop();
            return;
        }

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrolState();
                break;

            case EnemyState.Chase:
                UpdateChaseState();
                break;

            case EnemyState.Combat:
                UpdateCombatState();
                break;

            case EnemyState.Search:
                UpdateSearchState();
                break;
        }
    }

    #region States

    private void UpdatePatrolState()
    {
        if (IsPassive)
        {
            return;
        }

        if (IsAlwaysAware || perception.CanSeePlayer())
        {
            SpotPlayer();
        }
    }

    private void UpdateChaseState()
    {
        if (!KnowsWherePlayerIs)
        {
            ChangeState(EnemyState.Search);
            return;
        }

        RememberPlayerPosition();

        if (combatBehaviour.IsInCombatRange(player))
        {
            ChangeState(EnemyState.Combat);
            return;
        }

        combatBehaviour.UpdateChase(player);
    }

    private void UpdateCombatState()
    {
        if (!KnowsWherePlayerIs)
        {
            ChangeState(EnemyState.Search);
            return;
        }

        RememberPlayerPosition();

        combatBehaviour.UpdateCombat(player, playerHealth);
    }

    private void UpdateSearchState()
    {
        if (IsAlwaysAware || perception.CanSeePlayer())
        {
            SpotPlayer();
            return;
        }

        search.Tick(Time.deltaTime);

        if (search.IsFinished)
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    private void ChangeState(
        EnemyState newState,
        bool restartIfActive = false)
    {
        if (currentState == newState && !restartIfActive)
        {
            return;
        }

        ExitState(currentState);

        currentState = newState;

        EnterState(currentState);
    }

    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Patrol:
                combatBehaviour.OnTargetLost();
                patrol.StartPatrol();
                break;

            case EnemyState.Combat:
                combatBehaviour.EnterCombat(player, playerHealth);
                break;

            case EnemyState.Search:
                StartSearch();
                break;
        }
    }

    private void ExitState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Patrol:
                patrol.StopPatrol();
                break;

            case EnemyState.Combat:
                combatBehaviour.ExitCombat();
                break;
        }
    }

    #endregion

    #region Detection & Alerts

    private void SpotPlayer()
    {
        bool wasAlreadyHunting =
            currentState == EnemyState.Chase ||
            currentState == EnemyState.Combat;

        RememberPlayerPosition();

        combatBehaviour.OnTargetSpotted(player, playerHealth);

        if (!wasAlreadyHunting)
        {
            AlertNearbyAllies();
            ChangeState(EnemyState.Chase);
        }
    }

    private void HandleDamagedBy(Transform attacker)
    {
        if (attacker != player || !enabled)
        {
            return;
        }

        // Getroffen = weiß sofort, wo der Player ist (auch von hinten).
        Provoke();
    }

    public void SetPassive(bool isPassive)
    {
        IsPassive = isPassive;
    }

    public void SetAlwaysAware(bool isAlwaysAware)
    {
        IsAlwaysAware = isAlwaysAware;
    }

    /// <summary>Beendet den passiven Modus und greift den Player sofort an.</summary>
    public void Provoke()
    {
        if (!enabled || playerHealth == null)
        {
            return;
        }

        IsPassive = false;

        perception.ExtendAwareness(damageAwarenessDuration);
        SpotPlayer();
    }

    private void AlertNearbyAllies()
    {
        float sqrRadius = allyAlertRadius * allyAlertRadius;

        foreach (EnemyBrain ally in activeBrains)
        {
            if (ally == this)
            {
                continue;
            }

            float sqrDistance =
                (ally.transform.position - transform.position).sqrMagnitude;

            if (sqrDistance <= sqrRadius)
            {
                ally.ReceiveAlert(lastKnownPlayerPosition);
            }
        }
    }

    /// <summary>Ein Verbündeter hat den Player gesehen -> dort nachsehen.</summary>
    private void ReceiveAlert(Vector3 playerPosition)
    {
        if (IsPassive)
        {
            return;
        }

        if (currentState != EnemyState.Patrol &&
            currentState != EnemyState.Search)
        {
            return;
        }

        lastKnownPlayerPosition = playerPosition;
        lastKnownPlayerVelocity = Vector3.zero;

        // Auch eine laufende Suche wird mit der neuen Position neu gestartet.
        ChangeState(EnemyState.Search, restartIfActive: true);
    }

    #endregion

    #region Search

    private void StartSearch()
    {
        patrol.StopPatrol();

        search.Begin(lastKnownPlayerPosition, lastKnownPlayerVelocity);
    }

    private void RememberPlayerPosition()
    {
        if (Time.deltaTime > 0f)
        {
            Vector3 velocity =
                (player.position - lastKnownPlayerPosition) / Time.deltaTime;

            velocity.y = 0f;

            // Teleports / erste Sichtung nicht als Geschwindigkeit werten.
            const float MaxPlausibleSpeed = 20f;

            lastKnownPlayerVelocity =
                velocity.magnitude <= MaxPlausibleSpeed
                    ? velocity
                    : Vector3.zero;
        }

        lastKnownPlayerPosition = player.position;
    }

    #endregion

    private bool HasValidSetup()
    {
        if (player == null)
        {
            Debug.LogError($"{gameObject.name}: EnemyBrain has no Player assigned.", gameObject);
            return false;
        }

        playerHealth = player.GetComponent<Health>();

        if (playerHealth == null)
        {
            Debug.LogError($"{gameObject.name}: Player has no Health component.", gameObject);
            return false;
        }

        if (combatBehaviour == null)
        {
            Debug.LogError($"{gameObject.name}: No EnemyCombatBehaviour found.", gameObject);
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, allyAlertRadius);

        if (currentState == EnemyState.Search)
        {
            search.DrawGizmos();
        }
    }
}
