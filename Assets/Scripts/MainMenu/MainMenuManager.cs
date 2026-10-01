using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject _nameEntryObject;
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private AudioSource _mainAmbience;
    [SerializeField] private AudioClip _ambience;

    private const string BEGIN_SCENE = "BeginCutscene";

    private void Awake()
    {
        _mainAmbience.clip = _ambience;
        _mainAmbience.loop = true;
        _mainAmbience.Play();
    }

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
