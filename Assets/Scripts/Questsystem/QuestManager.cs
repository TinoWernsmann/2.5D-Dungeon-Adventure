using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour, ISaveable
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
        QuestProgress started = Begin(data, announce: true);

        if (started != null && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.StartTutorialOnce(data.StartTutorial);
        }
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

    #region Save System

    [Serializable]
    private struct SavedQuest
    {
        public string questId;
        public int current;
    }

    [Serializable]
    private struct SaveState
    {
        public List<SavedQuest> activeQuests;
        public int trackedIndex;
    }

    public string CaptureState()
    {
        SaveState state = new()
        {
            activeQuests = new List<SavedQuest>(),
            trackedIndex = _activeQuests.IndexOf(Tracked)
        };

        SaveRegistry registry = SaveRegistry.Instance;

        foreach (QuestProgress quest in _activeQuests)
        {
            string questId = registry != null ? registry.GetQuestId(quest.Data) : null;

            if (string.IsNullOrEmpty(questId))
            {
                Debug.LogWarning($"[Save] Quest '{quest.Data.name}' fehlt in der SaveRegistry.", this);
                continue;
            }

            state.activeQuests.Add(new SavedQuest { questId = questId, current = quest.Current });
        }

        return JsonUtility.ToJson(state);
    }

    public void RestoreState(string state)
    {
        SaveState saveState = JsonUtility.FromJson<SaveState>(state);
        SaveRegistry registry = SaveRegistry.Instance;

        _activeQuests.Clear();

        if (registry != null && saveState.activeQuests != null)
        {
            foreach (SavedQuest savedQuest in saveState.activeQuests)
            {
                QuestData data = registry.GetQuest(savedQuest.questId);

                if (data == null)
                {
                    continue;
                }

                QuestProgress progress = new(data);
                progress.SetProgress(savedQuest.current);
                _activeQuests.Add(progress);
            }
        }

        bool hasTrackedQuest =
            saveState.trackedIndex >= 0 &&
            saveState.trackedIndex < _activeQuests.Count;

        Tracked = hasTrackedQuest ? _activeQuests[saveState.trackedIndex] : null;
        OnTrackedQuestChanged?.Invoke(Tracked);
    }

    #endregion

    private void Log(string message)
    {
        if (_debugLogs) Debug.Log($"[Quest] {message}");
    }
}