using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private CutsceneSO _cutscene;
    [SerializeField] private Image _dialogueImage;

    private DialogueData[] _currentCutsceneDialogue;
    private int _dialogueCounter = 0;

    private const int WAIT_SECONDS = 5;

    private void Start()
    {
        _currentCutsceneDialogue = _cutscene.DialogueLines;
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
        SetDialogueImage(_currentCutsceneDialogue[_dialogueCounter].DialogueSprite);
    }

    private bool IsCutsceneOver()
    {
        return _dialogueCounter >= _currentCutsceneDialogue.Length - 1;
    }

    private void WaitUntilNextCutsceneEntry()
    {
        StartCoroutine(WaitCoroutine());
    }

    private IEnumerator WaitCoroutine()
    {
        yield return new WaitForSeconds(WAIT_SECONDS);
        if (!IsCutsceneOver()) PlayNextCutsceneElement();
    }

    private void SetDialogueImage(Sprite sprite)
    {
        if (sprite == null) return;
        _dialogueImage.sprite = sprite;
    }
}
