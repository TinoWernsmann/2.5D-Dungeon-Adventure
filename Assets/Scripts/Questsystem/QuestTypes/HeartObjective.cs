
using UnityEngine;

[RequireComponent(typeof(Health))]
public class HeartObjective : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private string _questTargetId = "Heart";

    [Header("Game Ending")]
    [SerializeField] private HerzRaumTrigger _endingTrigger;

    private Health _health;
    private bool _destroyed;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        _health.Died += HandleDestroyed;
    }

    private void OnDisable()
    {
        _health.Died -= HandleDestroyed;
    }

    private void HandleDestroyed()
    {
        if (_destroyed)
        {
            return;
        }

        _destroyed = true;

        // Letzte Quest abschließen.
        QuestManager.Report(
            QuestType.Destroy,
            _questTargetId
        );

        // Spielende auslösen und Endszene vorbereiten.
        if (_endingTrigger != null)
        {
            _endingTrigger.TriggerEnding();
        }
        else
        {
            Debug.LogWarning(
                "[HeartObjective] Kein Herzraum-Trigger zugewiesen!",
                gameObject
            );
        }

        Destroy(gameObject);
    }
}
