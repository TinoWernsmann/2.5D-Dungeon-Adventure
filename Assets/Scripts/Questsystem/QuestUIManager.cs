using System.Collections;
using TMPro;
using UnityEngine;

public class QuestUIManager : MonoBehaviour
{
    [Header("Goal")]
    [SerializeField] private TMP_Text _goalText;
    [Tooltip("Wird nur bei Quests mit Menge > 1 angezeigt")]
    [SerializeField] private GameObject _counterRoot;
    [SerializeField] private TMP_Text _counterText;

    [Header("Update Popup")]
    [SerializeField] private TMP_Text _updatedText;
    [SerializeField] private CanvasGroup _updatedGroup;
    [SerializeField] private string _updatedMessage = "Quest Updated!";
    [SerializeField] private float _showTime = 2f;
    [SerializeField] private float _fadeTime = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _updateSound;

    private Coroutine _popupRoutine;

    private void Start()
    {
        QuestManager quests = QuestManager.Instance;
        quests.OnQuestStarted += HandleQuestChanged;
        quests.OnQuestUpdated += HandleQuestChanged;
        quests.OnTrackedQuestChanged += Refresh;

        _updatedGroup.alpha = 0f;
        Refresh(quests.Tracked);
    }

    private void OnDestroy()
    {
        QuestManager quests = QuestManager.Instance;
        if (quests == null) return;

        quests.OnQuestStarted -= HandleQuestChanged;
        quests.OnQuestUpdated -= HandleQuestChanged;
        quests.OnTrackedQuestChanged -= Refresh;
    }

    private void HandleQuestChanged(QuestProgress quest)
    {
        if (quest == QuestManager.Instance.Tracked) Refresh(quest);
        ShowUpdatePopup();
    }

    private void Refresh(QuestProgress quest)
    {
        _goalText.text = quest != null ? quest.GoalText : string.Empty;
        RefreshCounter(quest);
    }

    private void RefreshCounter(QuestProgress quest)
    {
        if (_counterRoot == null) return;

        bool showCounter = quest != null && quest.HasCounter;
        _counterRoot.SetActive(showCounter);

        if (showCounter) _counterText.text = quest.CounterText;
    }

    private void ShowUpdatePopup()
    {
        PlayUpdateSound();

        if (_popupRoutine != null) StopCoroutine(_popupRoutine);
        _popupRoutine = StartCoroutine(PopupRoutine());
    }

    private void PlayUpdateSound()
    {
        if (_audioSource != null && _updateSound != null)
            _audioSource.PlayOneShot(_updateSound);
    }

    private IEnumerator PopupRoutine()
    {
        _updatedText.text = _updatedMessage;
        _updatedGroup.alpha = 1f;
        yield return new WaitForSeconds(_showTime);

        for (float t = 0f; t < _fadeTime; t += Time.deltaTime)
        {
            _updatedGroup.alpha = 1f - t / _fadeTime;
            yield return null;
        }

        _updatedGroup.alpha = 0f;
    }
}