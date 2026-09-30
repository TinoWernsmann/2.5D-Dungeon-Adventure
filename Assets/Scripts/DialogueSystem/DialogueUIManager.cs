using TMPro;
using UnityEngine;

public class DialogueUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _speakingTextArea;

    public void ShowSpeakingUI(string text)
    {
        _speakingTextArea.text = text;
    }
}
