using UnityEngine;

public class QuestStarter : MonoBehaviour
{
    [SerializeField] private QuestData _quest;

    private void Start() => QuestManager.Instance.StartQuest(_quest);
}