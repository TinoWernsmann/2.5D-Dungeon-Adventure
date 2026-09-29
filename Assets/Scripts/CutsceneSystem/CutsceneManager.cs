using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    public event Action OnCutsceneFinished;
    public bool IsPlaying { get; private set; }

    private TextMeshProUGUI _text;
    private DialogueData[] _currentCutsceneDialogue;
    private int _dialogueCounter;
    private CutsceneUI _cutsceneUI;

    private const int WAIT_SECONDS = 5;

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

        _dialogueCounter = 0;
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
        IsPlaying = false;
        OnCutsceneFinished?.Invoke();
        this.gameObject.SetActive(false);
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
