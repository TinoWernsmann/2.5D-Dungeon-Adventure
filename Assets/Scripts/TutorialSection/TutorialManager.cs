using TMPro;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    public bool IsShowing { get; private set; }

    [SerializeField] private GameObject _tutorialPanel;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _pageText;
    [SerializeField] private TextMeshProUGUI _continueText;
    [SerializeField] private AudioClip[] _tutorialSounds;

    private AudioSource _audioSource;
    private TutorialSO _currentTutorial;
    private int _pageIndex;
    private AudioClip _previousTutorialSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _audioSource = GetComponent<AudioSource>();

        if (_tutorialPanel != null)
        {
            _tutorialPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        SubscribeToInput();
    }

    private void OnDisable()
    {
        if (IsShowing)
        {
            HideTutorial();
        }

        UnsubscribeFromInput();
    }

    public void StartTutorial(TutorialSO tutorial)
    {
        if (tutorial == null)
        {
            Debug.LogError($"{nameof(TutorialManager)} cannot start a null tutorial.", this);
            return;
        }

        if (tutorial.Pages == null || tutorial.Pages.Length == 0)
        {
            Debug.LogError($"Tutorial '{tutorial.name}' does not contain any pages.", tutorial);
            return;
        }

        if (GameInput.Instance == null)
        {
            Debug.LogError($"{nameof(TutorialManager)} requires an active {nameof(GameInput)}.", this);
            return;
        }

        SubscribeToInput();
        _currentTutorial = tutorial;
        _pageIndex = 0;
        IsShowing = true;

        if (_tutorialPanel != null)
        {
            _tutorialPanel.SetActive(true);
        }

        ShowCurrentPage();
        GameInput.Instance.EnableDialogue();
    }

    public void HideTutorial()
    {
        if (!IsShowing)
        {
            return;
        }

        IsShowing = false;
        _currentTutorial = null;
        _pageIndex = 0;
        _previousTutorialSound = null;

        if (_tutorialPanel != null)
        {
            _tutorialPanel.SetActive(false);
        }

        if (GameInput.Instance != null)
        {
            GameInput.Instance.DisableDialogue();
        }
    }

    private void HandleDialogueInput()
    {
        if (!IsShowing || _currentTutorial == null)
        {
            return;
        }

        if (_pageIndex >= _currentTutorial.Pages.Length - 1)
        {
            HideTutorial();
            return;
        }

        _pageIndex++;
        ShowCurrentPage();
    }

    private void ShowCurrentPage()
    {
        if (_titleText != null)
        {
            _titleText.text = _currentTutorial.Title;
        }

        if (_pageText != null)
        {
            _pageText.text = _currentTutorial.Pages[_pageIndex];
        }

        if (_continueText != null)
        {
            _continueText.text = _pageIndex < _currentTutorial.Pages.Length - 1
                ? "Weiter (E)"
                : "Schließen (E)";
        }

        PlayRandomTutorialSound();
    }

    private void PlayRandomTutorialSound()
    {
        if (_audioSource == null || _tutorialSounds == null || _tutorialSounds.Length == 0)
        {
            return;
        }

        List<AudioClip> availableSounds = new();
        foreach (AudioClip sound in _tutorialSounds)
        {
            if (sound != null && sound != _previousTutorialSound)
            {
                availableSounds.Add(sound);
            }
        }

        if (availableSounds.Count == 0)
        {
            foreach (AudioClip sound in _tutorialSounds)
            {
                if (sound != null)
                {
                    availableSounds.Add(sound);
                }
            }
        }

        if (availableSounds.Count == 0)
        {
            return;
        }

        AudioClip selectedSound = availableSounds[Random.Range(0, availableSounds.Count)];
        _previousTutorialSound = selectedSound;
        _audioSource.PlayOneShot(selectedSound);
    }

    private void SubscribeToInput()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnDialogue -= HandleDialogueInput;
            GameInput.Instance.OnDialogue += HandleDialogueInput;
        }
    }

    private void UnsubscribeFromInput()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnDialogue -= HandleDialogueInput;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromInput();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
