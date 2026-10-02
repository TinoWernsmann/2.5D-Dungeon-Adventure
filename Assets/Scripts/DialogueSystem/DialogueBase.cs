using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Entry", menuName = "Dialogue/Entry")]
public class DialogueBase : ScriptableObject
{
    [SerializeField] private string _speakerName;
    [SerializeField] private string[] _spokenText;
    [Tooltip("Make the Sprites in the order of the written text")]
    [SerializeField] private Sprite[] _speakerSprites;
    [SerializeField] private bool _despawnAtEnd;


    public string[] SpokenText => _spokenText;
    public string SpeakerName => _speakerName;
    public Sprite[] SpeakerSprites => _speakerSprites;
    public bool DespawnAfter => _despawnAtEnd;
}
