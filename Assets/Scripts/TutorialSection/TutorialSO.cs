using UnityEngine;

[CreateAssetMenu(fileName = "New Tutorial", menuName = "Tutorial/Tutorial")]
public class TutorialSO : ScriptableObject
{
    [SerializeField] private string _title;
    [TextArea(2, 6)]
    [SerializeField] private string[] _pages;

    public string Title => _title;
    public string[] Pages => _pages;
}
