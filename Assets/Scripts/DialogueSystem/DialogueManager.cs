using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private DialogueUIManager _dialogueUI;
    [SerializeField] private PlayerMovement _movement;
    //[SerializeField] private DialogueAudioManager _audioManager;

    private List<DialogueInteractable> _interactables;

    private int _dialogueIndex = 0;
    private string[] _currentDialogue;
    private Sprite[] _speakerSprites;
    private string _currentSpeaker;
    private Sprite _defaultSprite;
    private bool _isToDespawn;

    private void Awake()
    {
        _interactables = new List<DialogueInteractable>();
        DialogueInteractable[] foundDialogue = FindObjectsByType<DialogueInteractable>();
        _interactables.AddRange(foundDialogue);
    }

    private void OnEnable()
    {
        if (_interactables != null &&  _interactables.Count > 0)
        {
            foreach (DialogueInteractable dialogue in _interactables)
            {
                dialogue.OnDialogueInteract += HandleDialogue;
            }
        }
    }

    private void HandleDialogue(DialogueContext context, SpriteRenderer sprite)
    {
        if (_dialogueUI == null) return;

        if (_dialogueIndex == 0)
        {
            StartDialogue(context, sprite);
        }
        else if (_dialogueIndex >= 0 && _dialogueIndex < _currentDialogue.Length)
        {
            ContinueDialogue(context, sprite);
        }
        else
        {
            EndDialogue(sprite);
        }
    }

    private void StartDialogue(DialogueContext context, SpriteRenderer sprite)
    {
        _movement.SetPlayerDialogueInput(true);
        _currentDialogue = context.Text;
        _currentSpeaker = context.Speaker;
        _speakerSprites = context.Sprites;
        _defaultSprite = context.DefaultSprite;
        _isToDespawn = context.Despawn;
        _dialogueUI.ShowSpeakingUI(_currentDialogue[_dialogueIndex], _currentSpeaker);
        sprite.sprite = _speakerSprites[_dialogueIndex];
        _dialogueIndex++;
    }

    private void ContinueDialogue(DialogueContext context, SpriteRenderer sprite)
    {
        _dialogueUI.ShowSpeakingUI(_currentDialogue[_dialogueIndex], _currentSpeaker);
        sprite.sprite = _speakerSprites[_dialogueIndex];
        _dialogueIndex++;
    }

    private void EndDialogue(SpriteRenderer sprite)
    {
        _movement.SetPlayerDialogueInput(false);
        _dialogueUI.HideSpeakingUI();
        sprite.sprite = _defaultSprite;
        _dialogueIndex = 0;
        _speakerSprites = null;
        _currentDialogue = null;

        if (_isToDespawn) sprite.GetComponentInParent<DialogueInteractable>().gameObject?.SetActive(false);
    }

    private void OnDisable()
    {
        if (_interactables != null && _interactables.Count > 0)
        {
            foreach (DialogueInteractable dialogue in _interactables)
            {
                dialogue.OnDialogueInteract -= HandleDialogue;
            }
        }
    }
}
