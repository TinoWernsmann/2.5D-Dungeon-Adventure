using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class CutscenePatrol : MonoBehaviour
{
    private enum DespawnMode
    {
        Never,
        AfterSeconds,
        AtLastWaypoint
    }

    [Header("Start")]
    [Min(0f)]
    [SerializeField] private float startDelay;
    [SerializeField] private bool startAutomatically = true;

    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;

    [Header("Despawn")]
    [SerializeField] private DespawnMode despawnMode = DespawnMode.AtLastWaypoint;
    [Min(0f)]
    [SerializeField] private float despawnAfterSeconds = 5f;

    [Header("Optional")]
    [Tooltip("Deaktiviert die normale EnemyBrain-Logik, damit sie die Cutscene-Patrol nicht überschreibt.")]
    [SerializeField] private bool disableEnemyBrain = true;

    private EnemyMovement movement;
    private Coroutine patrolCoroutine;
    private Coroutine despawnCoroutine;
    private bool isRunning;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
    }

    private void Start()
    {
        if (startAutomatically)
        {
            StartPatrol();
        }
    }

    public void StartPatrol()
    {
        if (isRunning)
        {
            return;
        }

        if (!HasValidWaypoints())
        {
            Debug.LogError(
                $"{nameof(CutscenePatrol)} on '{name}' requires at least one valid waypoint.",
                this);
            return;
        }

        DisableEnemyBrainIfNeeded();
        isRunning = true;
        patrolCoroutine = StartCoroutine(PatrolSequence());
    }

    public void StopPatrol()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
        }

        if (despawnCoroutine != null)
        {
            StopCoroutine(despawnCoroutine);
        }

        patrolCoroutine = null;
        despawnCoroutine = null;
        isRunning = false;
        movement.Stop();
    }

    private IEnumerator PatrolSequence()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        if (despawnMode == DespawnMode.AfterSeconds)
        {
            despawnCoroutine = StartCoroutine(DespawnAfterSeconds());
        }

        int lastWaypointIndex = GetLastWaypointIndex();
        for (int waypointIndex = 0; waypointIndex < waypoints.Length; waypointIndex++)
        {
            Transform waypoint = waypoints[waypointIndex];
            if (waypoint == null)
            {
                continue;
            }

            movement.MoveTo(waypoint.position);
            yield return new WaitUntil(() => movement.HasReachedDestination);

            if (waypointIndex == lastWaypointIndex &&
                despawnMode == DespawnMode.AtLastWaypoint)
            {
                Destroy(gameObject);
                yield break;
            }
        }

        isRunning = false;
        patrolCoroutine = null;
    }

    private IEnumerator DespawnAfterSeconds()
    {
        yield return new WaitForSeconds(despawnAfterSeconds);
        despawnCoroutine = null;
        Destroy(gameObject);
    }

    private int GetLastWaypointIndex()
    {
        for (int index = waypoints.Length - 1; index >= 0; index--)
        {
            if (waypoints[index] != null)
            {
                return index;
            }
        }

        return -1;
    }

    private void DisableEnemyBrainIfNeeded()
    {
        if (!disableEnemyBrain)
        {
            return;
        }

        EnemyBrain brain = GetComponent<EnemyBrain>();
        if (brain != null)
        {
            brain.enabled = false;
        }
    }

    private bool HasValidWaypoints()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return false;
        }

        foreach (Transform waypoint in waypoints)
        {
            if (waypoint != null)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDisable()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
        }

        if (despawnCoroutine != null)
        {
            StopCoroutine(despawnCoroutine);
        }

        patrolCoroutine = null;
        despawnCoroutine = null;
        isRunning = false;
    }
}
