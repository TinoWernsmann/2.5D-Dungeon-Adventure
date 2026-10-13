using UnityEngine;

public class QuestTalkTarget : MonoBehaviour
{
    [SerializeField] private string _targetId;
    [Tooltip("Optional: Quest, die nach dem Gespräch gestartet wird")]
    [SerializeField] private QuestData _questToGive;

    public void OnDialogueFinished()
    {
        // Erst melden, dann vergeben, damit die neue Quest nicht sofort selbst erfüllt wird
        QuestManager.Instance.ReportProgress(QuestType.Talk, _targetId);
        if (_questToGive != null) QuestManager.Instance.StartQuest(_questToGive);
    }
}