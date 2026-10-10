using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public event Action<QuestProgress> OnQuestStarted;
    public event Action<QuestProgress> OnQuestUpdated;
    public event Action<QuestProgress> OnQuestCompleted;
    public event Action<QuestProgress> OnQuestSkipped;
    public event Action<QuestProgress> OnTrackedQuestChanged;

    [SerializeField] private bool _debugLogs;

    private readonly List<QuestProgress> _activeQuests = new();

    public QuestProgress Tracked { get; private set; }
    public IReadOnlyList<QuestProgress> ActiveQuests => _activeQuests;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Null-sichere Kurzform für alle Objekte, die Fortschritt melden.</summary>
    public static void Report(QuestType type, string targetId, int amount = 1)
    {
        if (Instance == null || string.IsNullOrEmpty(targetId)) return;
        Instance.ReportProgress(type, targetId, amount);
        Debug.Log($"Quest Report: Destroy / {targetId}");
    }

    public void StartQuest(QuestData data)
    {
        Begin(data, announce: true);
    }

    public void ReportProgress(QuestType type, string targetId, int amount = 1)
    {
        Log($"Meldung: {type} / '{targetId}'");

        bool matchedActiveQuest = false;

        foreach (QuestProgress quest in _activeQuests.ToArray())
        {
            if (!quest.Data.Matches(type, targetId)) continue;

            matchedActiveQuest = true;
            Advance(quest, amount);
        }

        if (!matchedActiveQuest) TrySkipAhead(type, targetId, amount);
    }

    private void Advance(QuestProgress quest, int amount)
    {
        quest.Advance(amount);
        Log($"'{quest.Data.name}': {quest.Current}/{quest.Data.RequiredAmount}");

        if (quest.IsComplete) Complete(quest);
        else OnQuestUpdated?.Invoke(quest);
    }

    private void TrySkipAhead(QuestType type, string targetId, int amount)
    {
        foreach (QuestProgress active in _activeQuests.ToArray())
        {
            QuestData target = active.Data.FindSkippableSuccessor(type, targetId);
            if (target == null) continue;

            Skip(active);

            QuestProgress started = Begin(target, announce: false);
            if (started != null) Advance(started, amount);
            return;
        }
    }

    private QuestProgress Begin(QuestData data, bool announce)
    {
        if (data == null || IsActive(data)) return null;

        QuestProgress progress = new QuestProgress(data);
        _activeQuests.Add(progress);
        Tracked = progress;
        Log($"Gestartet: '{data.name}'");

        if (announce) OnQuestStarted?.Invoke(progress);
        OnTrackedQuestChanged?.Invoke(Tracked);
        return progress;
    }

    private void Complete(QuestProgress quest)
    {
        Remove(quest);
        Log($"Abgeschlossen: '{quest.Data.name}'");
        OnQuestCompleted?.Invoke(quest);

        StartQuest(quest.Data.NextQuest);
    }

    private void Skip(QuestProgress quest)
    {
        Remove(quest);
        Log($"Übersprungen: '{quest.Data.name}'");
        OnQuestSkipped?.Invoke(quest);
    }

    private void Remove(QuestProgress quest)
    {
        _activeQuests.Remove(quest);

        if (Tracked != quest) return;

        Tracked = _activeQuests.Count > 0 ? _activeQuests[^1] : null;
        OnTrackedQuestChanged?.Invoke(Tracked);
    }

    private bool IsActive(QuestData data)
    {
        return _activeQuests.Exists(q => q.Data == data);
    }

    private void Log(string message)
    {
        if (_debugLogs) Debug.Log($"[Quest] {message}");
    }
}