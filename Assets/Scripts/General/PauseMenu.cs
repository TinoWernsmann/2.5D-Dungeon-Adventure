using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private GameObject _mainPauseMenu;
    [SerializeField] private GameObject _confirmMenu;
    [SerializeField] private TextMeshProUGUI _confirmMenuText;

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
        if (GameInput.Instance == null) return;

        GameInput.Instance.OnPause -= TryPauseGame;
    }

    public void Start()
    {
        GameInput.Instance.OnPause += TryPauseGame; 
    }

    public void TryPauseGame()
    {
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
        SceneManager.LoadScene(MAIN_MENU_SCENE);
    }
}
