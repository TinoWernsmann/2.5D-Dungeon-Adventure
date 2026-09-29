using UnityEngine;

[System.Serializable]
public struct DialogueData
{
    [TextArea(2, 5)]
    public string Text;
    public AudioClip VoiceClip;
    public Sprite DialogueSprite;
}

[CreateAssetMenu(fileName = "Cutscene", menuName = "New Cutscene/Cutscene")]
public class CutsceneSO : ScriptableObject
{
    [SerializeField] private DialogueData[] _dialogueLines;

    public DialogueData[] DialogueLines => _dialogueLines;
}
