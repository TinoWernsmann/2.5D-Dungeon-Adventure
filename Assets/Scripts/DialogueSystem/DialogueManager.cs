using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private DialogueUIManager _dialogueUI;
    //[SerializeField] private DialogueAudioManager _audioManager;

    [SerializeField] private DialogueInteractable _interactables;
    private AudioClip _currentSpeakerAudio;

    private const float DIALOGUE_TIMER = 4f;
    private bool _isDialogue = false;

    private void OnEnable()
    {
        _interactables.OnDialogueInteract += HandleDialogue;
    }

    private void HandleDialogue(DialogueContext context)
    {
        _isDialogue = true;
        _currentSpeakerAudio = context.SpeakAudio;

        if (_dialogueUI != null)
        {
            //_audioManager.PlayDialogueAudio(_currentSpeakerAudio);
        }

        StartCoroutine(WaitDialoge(context));
    }

    private IEnumerator WaitDialoge(DialogueContext context)
    {
        _dialogueUI.ShowSpeakingUI(context.Text);

        yield return new WaitForSeconds(DIALOGUE_TIMER);

        _dialogueUI.ShowSpeakingUI(string.Empty);
        _isDialogue = false;
    }

    private void OnDisable()
    {
        _interactables.OnDialogueInteract -= HandleDialogue;
    }
}
