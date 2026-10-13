using TMPro;
using UnityEngine;

public class DialogueUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _speakingTextArea;
    [SerializeField] private TextMeshProUGUI _speakerNameArea;
    [SerializeField] private GameObject _dialogueBack;

    [Tooltip("Hinweis \"E drücken zum Fortfahren\". Wird bei automatisch ablaufenden Dialogen ausgeblendet.")]
    [SerializeField] private GameObject _continueHint;

    /// <param name="showContinueHint">
    /// false bei Dialogen, die automatisch weiterlaufen (z.B. Boss-Intro).
    /// </param>
    public void ShowSpeakingUI(string text, string speaker, bool showContinueHint = true)
    {
        _dialogueBack.SetActive(true);
        _speakingTextArea.text = text;
        _speakerNameArea.text = speaker;

        SetContinueHintVisible(showContinueHint);
    }

    public void HideSpeakingUI()
    {
        _dialogueBack.SetActive(false);
        _speakingTextArea.text = string.Empty;
        _speakerNameArea.text = string.Empty;

        SetContinueHintVisible(false);
    }

    private void SetContinueHintVisible(bool isVisible)
    {
        if (_continueHint != null)
        {
            _continueHint.SetActive(isVisible);
        }
    }
}
