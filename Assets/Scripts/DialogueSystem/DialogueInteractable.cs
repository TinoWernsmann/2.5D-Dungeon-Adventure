using UnityEngine;
using System;
using UnityEngine.UI;

// NPCs können nach dem Gespräch verschwinden -> Aktiv-Zustand speichern.
[SaveActiveState]
public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueBase _dialogueData;
    [SerializeField] private AudioClip _talkAudio;
    [SerializeField] private SpriteRenderer _sprite;

    public event Action<DialogueContext, SpriteRenderer> OnDialogueInteract;

    /// <summary>Wird ausgelöst, sobald der Spieler das Gespräch komplett durchgeklickt hat.</summary>
    public event Action DialogueFinished;

    public void Interact()
    {
        DialogueContext context = new DialogueContext(_dialogueData.SpeakerName, _dialogueData.SpokenText, _dialogueData.SpeakerSprites, _sprite.sprite, _dialogueData.DespawnAfter, _talkAudio);
        OnDialogueInteract?.Invoke(context, _sprite);
    }

    public void NotifyDialogueFinished()
    {
        DialogueFinished?.Invoke();
    }

    private void OnDestroy()
    {
        OnDialogueInteract = null;
        DialogueFinished = null;
    }
}

public readonly struct DialogueContext
{
    public string Speaker { get; }
    public string[] Text { get; }
    public AudioClip SpeakAudio { get; }
    public Sprite[] Sprites { get; }
    public Sprite DefaultSprite { get; }
    public bool Despawn { get; }

    public DialogueContext(string speaker, string[] texts, Sprite[] sprites, Sprite defaultSprite, bool despawn, AudioClip sound)
    {
        Speaker = speaker;
        Sprites = sprites;
        Despawn = despawn;
        Text = texts;
        DefaultSprite = defaultSprite;
        SpeakAudio = sound;
    }
}