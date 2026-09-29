using UnityEngine;
using System;

public class DialogueInteractable : InteractableController
{
    [SerializeField] private DialogueBase _dialogueData;
    [SerializeField] private AudioClip _talkAudio;

    public event Action<DialogueContext> OnDialogueInteract;

    public override void Interact()
    {
        DialogueContext context = new DialogueContext(_dialogueData.SpeakerName, _dialogueData.SpokenText, _talkAudio);
        OnDialogueInteract?.Invoke(context);
    }

    private void OnDestroy()
    {
        OnDialogueInteract = null;
    }
}

public readonly struct DialogueContext
{
    public string Speaker { get; }
    public string Text { get; }
    public AudioClip SpeakAudio { get; }

    public DialogueContext(string speaker, string text, AudioClip sound)
    {
        Speaker = speaker;
        Text = text;
        SpeakAudio = sound;
    }
}