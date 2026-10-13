using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject _nameEntryObject;
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private AudioSource _mainAmbience;
    [SerializeField] private AudioClip _ambience;
    [SerializeField] private GameObject _settingsMenu;

    [Header("Save System")]
    [SerializeField] private SaveSlotMenu _saveSlotMenu;
    [Tooltip("Lädt den zuletzt gespeicherten Spielstand. Ist ausgegraut, wenn es keinen gibt.")]
    [SerializeField] private Button _continueButton;
    [Tooltip("Wird ausgeblendet, solange das Lade-Menü offen ist (z.B. MainMenuScreen).")]
    [FormerlySerializedAs("_mainMenuObjects")]
    [SerializeField] private GameObject[] _hideWhileLoadMenuOpen;

    private const string BEGIN_SCENE = "BeginCutscene";

    private void Awake()
    {
        _mainAmbience.clip = _ambience;
        _mainAmbience.loop = true;
        _mainAmbience.Play();
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    private void Start()
    {
        RefreshContinueButton();

        DeleteGameAssets();

        if (SaveManager.Instance != null) SaveManager.Instance.SlotsChanged += RefreshContinueButton;
        if (_saveSlotMenu != null) _saveSlotMenu.Closed += HandleLoadMenuClosed;
    }

    private void DeleteGameAssets()
    {
        DontDestroyer[] gameAssets = FindObjectsByType<DontDestroyer>();

        foreach (DontDestroyer asset in gameAssets)
        {
            Destroy(asset.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null) SaveManager.Instance.SlotsChanged -= RefreshContinueButton;
        if (_saveSlotMenu != null) _saveSlotMenu.Closed -= HandleLoadMenuClosed;
    }

    public void ContinueGame()
    {
        int slot = SaveManager.Instance.GetMostRecentSlot();
        if (slot >= 0) SaveManager.Instance.Load(slot);
    }

    public void OpenLoadMenu()
    {
        if (_saveSlotMenu == null) return;

        SetMainMenuVisible(false);
        _saveSlotMenu.Open(SaveSlotMenu.Mode.Load);
    }

    private void HandleLoadMenuClosed()
    {
        SetMainMenuVisible(true);
    }

    private void SetMainMenuVisible(bool isVisible)
    {
        if (_hideWhileLoadMenuOpen == null) return;

        foreach (GameObject menuObject in _hideWhileLoadMenuOpen)
        {
            if (menuObject != null) menuObject.SetActive(isVisible);
        }
    }

    private void RefreshContinueButton()
    {
        if (_continueButton == null) return;
        _continueButton.interactable = SaveManager.Instance != null && SaveManager.Instance.HasAnySave();
    }

    public void SubmitName(string name)
    {
        GlobalVars.Instance.SetPlayerName(_nameInput.text);
        SceneLoader.Load(BEGIN_SCENE);
    }

    public void StartGame()
    {
        _nameEntryObject.SetActive(true);
    }

    public void ShowSettings()
    {
        _settingsMenu.SetActive(true);
    }

    public void HideSettings()
    {
        _settingsMenu.SetActive(false);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
