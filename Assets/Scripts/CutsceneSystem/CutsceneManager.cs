using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour
{
    public event Action OnCutsceneFinished;
    public bool IsPlaying { get; private set; }

    [SerializeField] private AudioClip _cutsceneAudio;
    [SerializeField] private bool _pauseAtStart;
    [SerializeField] private float _pauseLength;

    private TextMeshProUGUI _text;
    private DialogueData[] _currentCutsceneDialogue;
    private int _dialogueCounter;
    private CutsceneUI _cutsceneUI;
    private VideoClip _video;
    private AudioSource _cutsceneAudioSource;

    private const int WAIT_SECONDS = 5;

    private void Awake()
    {
        _cutsceneAudioSource = GetComponent<AudioSource>();
        if (_cutsceneAudioSource != null)
        {
            _cutsceneAudioSource.clip = _cutsceneAudio;
            _cutsceneAudioSource.Play();
        }
    }

    private void OnEnable()
    {
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _cutsceneUI = GetComponent<CutsceneUI>();
        if (_text == null) Debug.LogError("Error Loading Cutscene Text!");
        if (_cutsceneUI == null) Debug.LogError("Error Loading Cutscene UI Manager!");
    }

    public void StartNewCutscene(CutsceneSO cutscene)
    {
        if (cutscene != null) _currentCutsceneDialogue = cutscene.DialogueLines;
        if (_currentCutsceneDialogue == null || _currentCutsceneDialogue.Length == 0) return;

        if (_pauseAtStart)
        {
            StartCoroutine(WaitAtStart(cutscene));
        }
        else
        {
            _dialogueCounter = 0;
            _video = cutscene.Video;
            IsPlaying = true;
            SetDialogueData();
            WaitUntilNextCutsceneEntry();
        }
    }

    private IEnumerator WaitAtStart(CutsceneSO cutscene)
    {
        yield return new WaitForSeconds(_pauseLength);
        _dialogueCounter = 0;
        _video = cutscene.Video;
        IsPlaying = true;
        SetDialogueData();
        WaitUntilNextCutsceneEntry();
    }

    private void PlayNextCutsceneElement()
    {
        _dialogueCounter++;
        SetDialogueData();
        WaitUntilNextCutsceneEntry();
    }

    private void SetDialogueData()
    {
        string sentence = _currentCutsceneDialogue[_dialogueCounter].Text;
        string fin = sentence.Replace("(name)", GlobalVars.Instance.PlayerName);
        _text.text = fin;
        _cutsceneUI.SetDialogueImage(_currentCutsceneDialogue[_dialogueCounter].DialogueSprite);
    }

    private bool IsCutsceneOver()
    {
        return _dialogueCounter >= _currentCutsceneDialogue.Length - 1;
    }

    private void EndCutscene()
    {
        if (_video != null)
        {
            VideoPlayer player = _cutsceneUI.SetVideo(_video);
            if (player != null)
            {
                _text.text = string.Empty;
                StartCoroutine(PlayVideoRoutine(player));
                return;
            }
        }
        FinishCutscene();
    }

    private IEnumerator PlayVideoRoutine(VideoPlayer player)
    {
        bool finished = false;
        void OnFinished(VideoPlayer vp) => finished = true;
        void OnError(VideoPlayer vp, string msg)
        {
            Debug.LogError($"Video error: {msg}");
            finished = true;
        }

        player.loopPointReached += OnFinished;
        player.errorReceived += OnError;

        player.Prepare();
        yield return new WaitUntil(() => player.isPrepared || finished);

        if (!finished)
        {
            player.Play();
            yield return new WaitUntil(() => finished);
        }

        player.loopPointReached -= OnFinished;
        player.errorReceived -= OnError;

        _cutsceneUI.HideVideo();
        FinishCutscene();
    }

    private void FinishCutscene()
    {
        IsPlaying = false;
        OnCutsceneFinished?.Invoke();
    }

    private void WaitUntilNextCutsceneEntry()
    {
        StartCoroutine(WaitCoroutine());
    }

    private IEnumerator WaitCoroutine()
    {
        yield return new WaitForSeconds(WAIT_SECONDS);
        if (!IsCutsceneOver())
        {
            PlayNextCutsceneElement();
        }
        else
        {
            EndCutscene();
        }
    }
}
