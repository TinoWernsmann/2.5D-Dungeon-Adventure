using UnityEngine;

public class TutorialTriggerZone : MonoBehaviour
{
    [SerializeField] private TutorialSO _tutorial;
    [SerializeField] private bool _triggerOnlyOnce = true;

    private bool _hasBeenTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (_triggerOnlyOnce && _hasBeenTriggered)
        {
            return;
        }

        if (TutorialManager.Instance == null)
        {
            Debug.LogError($"{nameof(TutorialTriggerZone)} requires an active {nameof(TutorialManager)}.", this);
            return;
        }

        if (_tutorial == null)
        {
            Debug.LogError($"{nameof(TutorialTriggerZone)} has no tutorial assigned.", this);
            return;
        }

        _hasBeenTriggered = true;
        TutorialManager.Instance.StartTutorial(_tutorial);
    }
}
