public class QuestProgress
{
    public QuestData Data { get; }
    public int Current { get; private set; }
    public bool IsComplete => Current >= Data.RequiredAmount;

    public string GoalText => Data.GoalText;
    public bool HasCounter => Data.HasCounter;
    public string CounterText => $"{Current}/{Data.RequiredAmount}";

    public QuestProgress(QuestData data)
    {
        Data = data;
    }

    public void Advance(int amount)
    {
        Current += amount;
    }
}