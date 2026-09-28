using System.Collections;
using TMPro;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    [SerializeField] private CutsceneSO _cutscene;

    private TextMeshProUGUI _text;
    private DialogueData[] _currentCutsceneDialogue;
    private int _dialogueCounter = 0;
    private CutsceneUI _cutsceneUI;

    private const int WAIT_SECONDS = 5;

    private void OnEnable()
    {
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _cutsceneUI = GetComponent<CutsceneUI>();
        if (_text == null) Debug.LogError("Error Loading Cutscene Text!");
        if (_cutsceneUI == null) Debug.LogError("Error Loading Cutscene UI Manager!");
    }

    private void Start()
    {
        if (_cutscene != null) _currentCutsceneDialogue = _cutscene.DialogueLines;
        StartCutscene();
    }

    private void StartCutscene()
    {
        if (_currentCutsceneDialogue == null) return;
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
        _text.text = _currentCutsceneDialogue[_dialogueCounter].Text;
        _cutsceneUI.SetDialogueImage(_currentCutsceneDialogue[_dialogueCounter].DialogueSprite);
    }

    private bool IsCutsceneOver()
    {
        return _dialogueCounter >= _currentCutsceneDialogue.Length - 1;
    }

    private void EndCutscene()
    {
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
