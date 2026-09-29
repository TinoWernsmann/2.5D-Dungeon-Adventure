using UnityEngine;

[CreateAssetMenu(fileName = "New Misc Interactable", menuName = "Interactable/Interactable")]
public class Interactable : ScriptableObject
{
    [SerializeField] private string _name;
    [Tooltip("Can be left empty if interactable does more than just show a description text.")]
    [TextArea]
    [SerializeField] private string _onScreenText;
    [Tooltip("Leave sound empty if no sound will be played upon interact.")]
    [SerializeField] private AudioClip _interactSound;

    public string InteractableName => _name;
    public string InteractText => _onScreenText;
    public AudioClip InteractSound => _interactSound;
}