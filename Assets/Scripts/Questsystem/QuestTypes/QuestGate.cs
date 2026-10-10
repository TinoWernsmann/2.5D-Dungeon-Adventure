using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Zerstört ein Tor, sobald eine Quest einen Fortschritt erreicht oder abgeschlossen ist.</summary>
public class QuestGate : MonoBehaviour
{
    [Header("Condition")]
    [SerializeField] private QuestData _quest;
    [Tooltip("Das Tor öffnet bei diesem Fortschritt. 0 = erst beim Abschluss der Quest")]
    [FormerlySerializedAs("_requiredProgress")]
    [SerializeField, Min(0)] private int _openAtProgress = 2;

    [Header("Gate")]
    [SerializeField] private GameObject _gateObject;
    [SerializeField] private AudioClip _openClip;
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;

    private bool _isOpen;

    private void Start()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("QuestGate: Kein QuestManager in der Szene.", this);
            return;
        }

        QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;
        QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;

        WarnIfMisconfigured();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void HandleQuestUpdated(QuestProgress quest)
    {
        if (quest.Data != _quest) return;
        if (_openAtProgress > 0 && quest.Current >= _openAtProgress) Open();
    }

    private void HandleQuestCompleted(QuestProgress quest)
    {
        if (quest.Data == _quest) Open();
    }

    private void Open()
    {
        if (_isOpen) return;

        _isOpen = true;
        Unsubscribe();

        if (_gateObject == null) return;

        if (_openClip != null)
            AudioSource.PlayClipAtPoint(_openClip, _gateObject.transform.position, _volume);

        Destroy(_gateObject);
    }

    private void Unsubscribe()
    {
        if (QuestManager.Instance == null) return;

        QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
        QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
    }

    private void WarnIfMisconfigured()
    {
        if (_quest == null || _openAtProgress <= _quest.RequiredAmount) return;

        Debug.LogWarning(
            $"QuestGate: Öffnet bei {_openAtProgress}, aber '{_quest.name}' braucht nur " +
            $"{_quest.RequiredAmount}. Das Tor öffnet erst beim Abschluss der Quest.", this);
    }
}