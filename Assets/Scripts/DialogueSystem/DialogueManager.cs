using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Führt NPC-Gespräche: Ein <see cref="DialogueInteractable"/> startet das Gespräch,
/// danach blättert der Manager selbst mit der Dialog-Taste weiter – egal, wohin der
/// Spieler gerade schaut. Am Ende wird das Interactable benachrichtigt
/// (<see cref="DialogueInteractable.DialogueFinished"/>), z.B. für Cutscenes.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [SerializeField] private DialogueUIManager _dialogueUI;

    private List<DialogueInteractable> _interactables;

    private int _dialogueIndex;
    private string[] _currentDialogue;
    private Sprite[] _speakerSprites;
    private string _currentSpeaker;
    private Sprite _defaultSprite;
    private bool _isToDespawn;
    private SpriteRenderer _currentSprite;

    private bool IsDialogueActive => _currentDialogue != null;

    private void Awake()
    {
        _interactables = new List<DialogueInteractable>();
        DialogueInteractable[] foundDialogue = FindObjectsByType<DialogueInteractable>();
        _interactables.AddRange(foundDialogue);
    }

    private void OnEnable()
    {
        foreach (DialogueInteractable dialogue in _interactables)
        {
            dialogue.OnDialogueInteract += HandleDialogue;
        }
    }

    private void OnDisable()
    {
        foreach (DialogueInteractable dialogue in _interactables)
        {
            if (dialogue != null) dialogue.OnDialogueInteract -= HandleDialogue;
        }

        StopListeningForInput();
    }

    private void HandleDialogue(DialogueContext context, SpriteRenderer sprite)
    {
        if (_dialogueUI == null || IsDialogueActive) return;
        if (context.Text == null || context.Text.Length == 0) return;

        StartDialogue(context, sprite);
    }

    private void StartDialogue(DialogueContext context, SpriteRenderer sprite)
    {
        _currentDialogue = context.Text;
        _currentSpeaker = context.Speaker;
        _speakerSprites = context.Sprites;
        _defaultSprite = context.DefaultSprite;
        _isToDespawn = context.Despawn;
        _currentSprite = sprite;
        _dialogueIndex = 0;

        GameInput.Instance.EnableDialogue();
        GameInput.Instance.OnDialogue += AdvanceDialogue;

        ShowCurrentLine();
    }

    private void AdvanceDialogue()
    {
        _dialogueIndex++;

        if (_dialogueIndex < _currentDialogue.Length)
        {
            ShowCurrentLine();
            return;
        }

        EndDialogue();
    }

    private void ShowCurrentLine()
    {
        _dialogueUI.ShowSpeakingUI(_currentDialogue[_dialogueIndex], _currentSpeaker);

        if (_currentSprite != null && _speakerSprites != null && _dialogueIndex < _speakerSprites.Length)
        {
            _currentSprite.sprite = _speakerSprites[_dialogueIndex];
        }
    }

    private void EndDialogue()
    {
        StopListeningForInput();

        GameInput.Instance.DisableDialogue();
        _dialogueUI.HideSpeakingUI();

        SpriteRenderer sprite = _currentSprite;
        bool despawn = _isToDespawn;

        _currentDialogue = null;
        _speakerSprites = null;
        _currentSprite = null;
        _dialogueIndex = 0;

        if (sprite == null) return;

        sprite.sprite = _defaultSprite;
        sprite.GetComponentInParent<QuestTalkTarget>()?.OnDialogueFinished();

        DialogueInteractable interactable = sprite.GetComponentInParent<DialogueInteractable>();
        if (interactable == null) return;

        interactable.NotifyDialogueFinished();

        if (despawn) interactable.gameObject.SetActive(false);
    }

    private void StopListeningForInput()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnDialogue -= AdvanceDialogue;
    }
}
