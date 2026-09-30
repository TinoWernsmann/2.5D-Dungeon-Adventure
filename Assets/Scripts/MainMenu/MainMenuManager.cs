using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject _nameEntryObject;
    [SerializeField] private TMP_InputField _nameInput;

    private const string BEGIN_SCENE = "BeginCutscene";

    public void SubmitName(string name)
    {
        GlobalVars.Instance.SetPlayerName(_nameInput.text);
        SceneManager.LoadScene(BEGIN_SCENE);
    }

    public void StartGame()
    {
        _nameEntryObject.SetActive(true);
    }
}
