using UnityEngine;

public enum QuestType
{
    ReachZone,
    Destroy,
    Collect,
    Talk,
    Kill
}

[CreateAssetMenu(fileName = "New Quest", menuName = "Quests/Quest")]
public class QuestData : ScriptableObject
{
    private const int MaxChainDepth = 20;

    [Header("Display")]
    [SerializeField] private string _title;
    [SerializeField] private string _goalText;
    [Tooltip("Zeigt den Zähler (z.B. 2/5) bei Quests mit Menge > 1")]
    [SerializeField] private bool _showCounter = true;

    [Header("Objective")]
    [SerializeField] private QuestType _type;
    [Tooltip("Muss exakt der ID des meldenden Objekts entsprechen")]
    [SerializeField] private string _targetId;
    [SerializeField, Min(1)] private int _requiredAmount = 1;

    [Header("Flow")]
    [SerializeField] private QuestData _nextQuest;
    [Tooltip("Wird übersprungen, sobald eine spätere Quest der Kette erfüllt wird")]
    [SerializeField] private bool _canBeSkipped;

    public string Title => _title;
    public string GoalText => _goalText;
    public QuestType Type => _type;
    public string TargetId => _targetId;
    public int RequiredAmount => _requiredAmount;
    public QuestData NextQuest => _nextQuest;
    public bool HasCounter => _showCounter && _requiredAmount > 1;

    public bool Matches(QuestType type, string targetId)
    {
        return _type == type && _targetId == targetId;
    }

    /// <summary>
    /// Läuft die Next-Quest-Kette ab. Liefert die erste Quest, die zur Meldung passt,
    /// sofern alle Quests davor (inklusive dieser) überspringbar sind.
    /// </summary>
    public QuestData FindSkippableSuccessor(QuestType type, string targetId)
    {
        QuestData current = this;

        for (int depth = 0; depth < MaxChainDepth; depth++)
        {
            if (!current._canBeSkipped) return null;

            current = current._nextQuest;
            if (current == null) return null;
            if (current.Matches(type, targetId)) return current;
        }

        return null;
    }
}