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

    [Header("Difficulty")]
    [Tooltip("Wird wie das Lade-Menü geöffnet; blendet dieselben Objekte aus.")]
    [SerializeField] private DifficultyMenu _difficultyMenu;

    [Header("Achievements")]
    [Tooltip("Wird wie das Lade-Menü geöffnet; blendet dieselben Objekte aus.")]
    [SerializeField] private AchievementMenu _achievementMenu;

    [Header("Level Select")]
    [Tooltip("Wird wie das Lade-Menü geöffnet; blendet dieselben Objekte aus.")]
    [SerializeField] private LevelSelectMenu _levelSelectMenu;

    private const string BEGIN_SCENE = "BeginCutscene";

    // Level 1 beginnt immer mit der Anfangs-Cutscene, spätere Levels direkt mit ihrer Szene.
    private string _sceneToStart = BEGIN_SCENE;

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

        if (SaveManager.Instance != null) SaveManager.Instance.SlotsChanged += RefreshContinueButton;
        if (_saveSlotMenu != null) _saveSlotMenu.Closed += HandleLoadMenuClosed;
        if (_difficultyMenu != null) _difficultyMenu.Closed += HandleLoadMenuClosed;
        if (_achievementMenu != null) _achievementMenu.Closed += HandleLoadMenuClosed;

        if (_levelSelectMenu != null)
        {
            _levelSelectMenu.Closed += HandleLoadMenuClosed;
            _levelSelectMenu.LevelChosen += StartLevel;
        }
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null) SaveManager.Instance.SlotsChanged -= RefreshContinueButton;
        if (_saveSlotMenu != null) _saveSlotMenu.Closed -= HandleLoadMenuClosed;
        if (_difficultyMenu != null) _difficultyMenu.Closed -= HandleLoadMenuClosed;
        if (_achievementMenu != null) _achievementMenu.Closed -= HandleLoadMenuClosed;

        if (_levelSelectMenu != null)
        {
            _levelSelectMenu.Closed -= HandleLoadMenuClosed;
            _levelSelectMenu.LevelChosen -= StartLevel;
        }
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

    /// <summary>Button "Difficulty" im Hauptmenü.</summary>
    public void OpenDifficultyMenu()
    {
        if (_difficultyMenu == null) return;

        SetMainMenuVisible(false);
        _difficultyMenu.Open();
    }

    /// <summary>Button "Erfolge" im Hauptmenü.</summary>
    public void OpenAchievementMenu()
    {
        if (_achievementMenu == null) return;

        SetMainMenuVisible(false);
        _achievementMenu.Open();
    }

    /// <summary>Button "Load Level" im Hauptmenü.</summary>
    public void OpenLevelSelect()
    {
        if (_levelSelectMenu == null) return;

        SetMainMenuVisible(false);
        _levelSelectMenu.Open();
    }

    /// <summary>Level aus der Auswahl: wie ein neues Spiel (Schwierigkeit, Name), nur mit anderer Startszene.</summary>
    private void StartLevel(int levelNumber)
    {
        BeginNewGame(levelNumber == 1 ? BEGIN_SCENE : LevelSelectMenu.GetLevelSceneName(levelNumber));
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
        if (SaveManager.Instance != null) SaveManager.Instance.StartNewGame();
        SceneLoader.Load(_sceneToStart);
    }

    /// <summary>Start: erst Schwierigkeit wählen, dann Namen eingeben, dann Cutscene.</summary>
    public void StartGame()
    {
        BeginNewGame(BEGIN_SCENE);
    }

    private void BeginNewGame(string sceneToStart)
    {
        _sceneToStart = sceneToStart;

        if (_difficultyMenu == null)
        {
            ShowNameEntry();
            return;
        }

        SetMainMenuVisible(false);
        _difficultyMenu.Open(ShowNameEntry);
    }

    private void ShowNameEntry()
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
