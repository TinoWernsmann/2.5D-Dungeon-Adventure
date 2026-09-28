using UnityEngine;
using UnityEngine.UI;

public class CutsceneUI : MonoBehaviour
{
    [SerializeField] private Image _dialogueImage;

    private void OnEnable()
    {
        if (_dialogueImage == null) Debug.LogError("Error Loading Cutscene UI!");
    }

    public void SetDialogueImage(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogError("No Image provided!");
            return;
        }
        Debug.Log("Image Set");
        _dialogueImage.sprite = sprite;
    }
}
