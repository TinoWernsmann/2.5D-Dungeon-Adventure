using UnityEngine;

[RequireComponent(typeof(Collider))]
public class QuestTriggerZone : MonoBehaviour
{
    [SerializeField] private string _targetId;
    private bool _used;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (_used) return;
        if (other.GetComponentInParent<ItemCollector>() == null) return;

        _used = true;
        QuestManager.Instance.ReportProgress(QuestType.ReachZone, _targetId);
    }
}