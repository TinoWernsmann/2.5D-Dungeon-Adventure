using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class QuestCollectible : MonoBehaviour
{
    [SerializeField] private string _targetId;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        QuestManager.Instance.ReportProgress(QuestType.Collect, _targetId);
        gameObject.SetActive(false);
    }
}