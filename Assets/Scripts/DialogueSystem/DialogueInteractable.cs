using UnityEngine;
using System;
using UnityEngine.UI;

public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueBase _dialogueData;
    [SerializeField] private AudioClip _talkAudio;
    [SerializeField] private SpriteRenderer _sprite;

    public event Action<DialogueContext, SpriteRenderer> OnDialogueInteract;

    public void Interact()
    {
        DialogueContext context = new DialogueContext(_dialogueData.SpeakerName, _dialogueData.SpokenText, _dialogueData.SpeakerSprites, _sprite.sprite, _talkAudio);
        OnDialogueInteract?.Invoke(context, _sprite);
    }

    private void OnDestroy()
    {
        OnDialogueInteract = null;
    }
}

public readonly struct DialogueContext
{
    public string Speaker { get; }
    public string[] Text { get; }
    public AudioClip SpeakAudio { get; }
    public Sprite[] Sprites { get; }
    public Sprite DefaultSprite { get; }

    public DialogueContext(string speaker, string[] texts, Sprite[] sprites, Sprite defaultSprite, AudioClip sound)
    {
        Speaker = speaker;
        Sprites = sprites;
        Text = texts;
        DefaultSprite = defaultSprite;
        SpeakAudio = sound;
    }
}