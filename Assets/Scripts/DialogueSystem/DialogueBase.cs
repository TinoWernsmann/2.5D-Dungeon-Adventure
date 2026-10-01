using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Entry", menuName = "Dialogue/Entry")]
public class DialogueBase : ScriptableObject
{
    [SerializeField] private string _speakerName;
    [SerializeField] private string[] _spokenText;

    public string[] SpokenText => _spokenText;
    public string SpeakerName => _speakerName;
}
