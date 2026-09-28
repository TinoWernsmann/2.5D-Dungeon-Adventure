using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject _nameEntryObject;
    [SerializeField] private TMP_InputField _nameInput;

    private const string MAIN_SCENE = "TinoScene";
    public void SubmitName(string name)
    {
        GlobalVars.Instance.SetPlayerName(_nameInput.text);
        SceneManager.LoadScene(MAIN_SCENE);
    }

    public void StartGame()
    {
        _nameEntryObject.SetActive(true);
    }
}
