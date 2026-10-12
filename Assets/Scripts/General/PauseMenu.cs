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

    private Option _chosenOption = Option.None;

    private const string EXIT_TEXT = "Are you sure you want to exit the game?";
    private const string MENU_TEXT = "Are you sure you want to go to the main menu?";
    private const string MAIN_MENU_SCENE = "MainMenu";
    private bool isPause = false;

    private enum Option
    {
        Exit,
        MainMenu,
        None
    }

    private void OnDisable()
    {
        if (_saveSlotMenu != null) _saveSlotMenu.Closed -= HandleSaveSlotMenuClosed;

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
    }

    public void TryPauseGame()
    {
        // ESC im Speichern/Laden-Menü führt zurück ins Pause-Menü.
        if (_saveSlotMenu != null && _saveSlotMenu.IsOpen)
        {
            _saveSlotMenu.Close();
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

    private void HandleSaveSlotMenuClosed()
    {
        if (!isPause) return;

        _pauseMenu.SetActive(true);
        _mainPauseMenu.SetActive(true);
    }

    public void TryExitGame()
    {
        _confirmMenuText.text = EXIT_TEXT;
        _chosenOption = Option.Exit;
        _mainPauseMenu.SetActive(false);
        _confirmMenu.SetActive(true);
    }

    public void TryMainMenu()
    {
        _confirmMenuText.text = MENU_TEXT;
        _chosenOption = Option.MainMenu;
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
}
