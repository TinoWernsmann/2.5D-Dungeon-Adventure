using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class QuestTriggerZone : MonoBehaviour, ISaveable
{
    [SerializeField] private string _targetId;
    private bool _used;

    [Serializable]
    private struct SaveState
    {
        public bool used;
    }

    public string CaptureState()
    {
        return JsonUtility.ToJson(new SaveState { used = _used });
    }

    public void RestoreState(string state)
    {
        _used = JsonUtility.FromJson<SaveState>(state).used;
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (_used) return;
        if (other.GetComponentInParent<ItemCollector>() == null) return;

        _used = true;
        QuestManager.Instance.ReportProgress(QuestType.ReachZone, _targetId);
    }
}