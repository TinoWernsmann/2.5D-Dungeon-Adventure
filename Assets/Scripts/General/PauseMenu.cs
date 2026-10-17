using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private GameObject _mainPauseMenu;
    [SerializeField] private GameObject _confirmMenu;
    [SerializeField] private TextMeshProUGUI _confirmMenuText;

    [Header("Save System")]
    [SerializeField] private SaveSlotMenu _saveSlotMenu;

    [Header("Difficulty")]
    [SerializeField] private DifficultyMenu _difficultyMenu;

    private Option _chosenOption = Option.None;

    private const string EXIT_TEXT = "Are you sure you want to exit the game?";
    private const string MENU_TEXT = "Are you sure you want to go to the main menu?";
    private const string RESTART_TEXT = "Are you sure you want to restart the level?\nUnsaved progress will be lost.";
    private const string MAIN_MENU_SCENE = "MainMenu";
    private bool isPause = false;

    private enum Option
    {
        Exit,
        MainMenu,
        Restart,
        None
    }

    private void OnDisable()
    {
        if (_saveSlotMenu != null) _saveSlotMenu.Closed -= HandleSaveSlotMenuClosed;
        if (_difficultyMenu != null) _difficultyMenu.Closed -= HandleSaveSlotMenuClosed;

        if (GameInput.Instance == null) return;

        GameInput.Instance.OnPause -= TryPauseGame;
    }

    public void Start()
    {
        // Falls das Menü zum Bearbeiten in der Szene aktiv gespeichert wurde.
        _pauseMenu.SetActive(false);
        _confirmMenu.SetActive(false);

        GameInput.Instance.OnPause += TryPauseGame;

        if (_saveSlotMenu != null) _saveSlotMenu.Closed += HandleSaveSlotMenuClosed;
        if (_difficultyMenu != null) _difficultyMenu.Closed += HandleSaveSlotMenuClosed;
    }

    public void TryPauseGame()
    {
        // ESC im Speichern/Laden-Menü führt zurück ins Pause-Menü.
        if (_saveSlotMenu != null && _saveSlotMenu.IsOpen)
        {
            _saveSlotMenu.Close();
            return;
        }

        if (_difficultyMenu != null && _difficultyMenu.IsOpen)
        {
            _difficultyMenu.Close();
            return;
        }

        if (isPause)
        {
            PresumeGame();
        }
        else
        {
            isPause = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
            Time.timeScale = 0;
            _pauseMenu.SetActive(true);         
        }
    }

    public void PresumeGame()
    {
        Time.timeScale = 1;
        isPause = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _pauseMenu.SetActive(false);
        _confirmMenu.SetActive(false);    
        _mainPauseMenu.SetActive(true);

        if (_saveSlotMenu != null && _saveSlotMenu.IsOpen) _saveSlotMenu.Close();
        if (_difficultyMenu != null && _difficultyMenu.IsOpen) _difficultyMenu.Close();
    }

    public void OpenSaveMenu()
    {
        OpenSaveSlotMenu(SaveSlotMenu.Mode.Save);
    }

    public void OpenLoadMenu()
    {
        OpenSaveSlotMenu(SaveSlotMenu.Mode.Load);
    }

    private void OpenSaveSlotMenu(SaveSlotMenu.Mode mode)
    {
        if (_saveSlotMenu == null)
        {
            Debug.LogWarning("PauseMenu: Kein SaveSlotMenu zugewiesen.", this);
            return;
        }

        // Das komplette Pause-Menü (Hintergrund, Buttons, Controls) ausblenden.
        _pauseMenu.SetActive(false);
        _saveSlotMenu.Open(mode);
    }

    /// <summary>Button "Change Difficulty" im Pause-Menü.</summary>
    public void OpenDifficultyMenu()
    {
        if (_difficultyMenu == null)
        {
            Debug.LogWarning("PauseMenu: Kein DifficultyMenu zugewiesen.", this);
            return;
        }

        _pauseMenu.SetActive(false);
        _difficultyMenu.Open();
    }

    // Gilt für Save- und Schwierigkeits-Menü: zurück ins Pause-Menü.
    private void HandleSaveSlotMenuClosed()
    {
        if (!isPause) return;

        _pauseMenu.SetActive(true);
        _mainPauseMenu.SetActive(true);
    }

    public void TryExitGame()
    {
        ShowConfirm(EXIT_TEXT, Option.Exit);
    }

    public void TryMainMenu()
    {
        ShowConfirm(MENU_TEXT, Option.MainMenu);
    }

    public void TryRestartLevel()
    {
        ShowConfirm(RESTART_TEXT, Option.Restart);
    }

    /// <summary>Zeigt den gemeinsamen Bestätigungsdialog ("Sure") für die gewählte Aktion.</summary>
    private void ShowConfirm(string question, Option option)
    {
        _confirmMenuText.text = question;
        _chosenOption = option;
        _mainPauseMenu.SetActive(false);
        _confirmMenu.SetActive(true);
    }

    public void CancelDecision()
    {
        _confirmMenuText.text = string.Empty;
        _chosenOption = Option.None;
        _mainPauseMenu.SetActive(true);
        _confirmMenu.SetActive(false); 
    }

    public void ConfirmDecision()
    {
        switch (_chosenOption)
        {
            case Option.Exit:
                ExitGame();
                break;
            case Option.MainMenu:
                GoToMainMenu();
                break;
            case Option.Restart:
                RestartLevel();
                break;
            case Option.None: // This is just a fall back, it just cancels the menu on confirm.
                CancelDecision();
                break;
        }
    }

    private void ExitGame()
    {
        Application.Quit();
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1;
        SceneLoader.Load(MAIN_MENU_SCENE);
    }

    /// <summary>Lädt das aktuelle Level komplett neu (ohne Spielstand).</summary>
    private void RestartLevel()
    {
        Time.timeScale = 1;
        SceneLoader.Load(SceneManager.GetActiveScene().name);
    }
}
