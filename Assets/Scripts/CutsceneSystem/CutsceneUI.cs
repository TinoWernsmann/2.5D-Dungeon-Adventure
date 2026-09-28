using UnityEngine;
using UnityEngine.UI;

public class CutsceneUI : MonoBehaviour
{
    private Image _dialogueImage;

    private void OnEnable()
    {
        _dialogueImage = GetComponentInChildren<Image>();
        if (_dialogueImage == null) Debug.LogError("Error Loading Cutscene UI!");
    }

    public void SetDialogueImage(Sprite sprite)
    {
        if (sprite == null) return;
        _dialogueImage.sprite = sprite;
    }
}
