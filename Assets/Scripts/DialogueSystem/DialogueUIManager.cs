using TMPro;
using UnityEngine;

public class DialogueUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _speakingTextArea;
    [SerializeField] private TextMeshProUGUI _speakerNameArea;
    [SerializeField] private GameObject _dialogueBack;

    public void ShowSpeakingUI(string text, string speaker)
    {
        _dialogueBack.SetActive(true);
        _speakingTextArea.text = text;
        _speakerNameArea.text = speaker;
    }

    public void HideSpeakingUI()
    {
        _dialogueBack.SetActive(false);
        _speakingTextArea.text = string.Empty;
        _speakerNameArea.text = string.Empty;
    }
}
