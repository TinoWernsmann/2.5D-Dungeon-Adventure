using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Todes-Bildschirm. Der Spieler entscheidet selbst, wie es weitergeht:
/// Level neu starten, einen Spielstand laden oder zurück ins Hauptmenü.
/// </summary>
public class DeathScreenManager : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Wird neu gestartet, falls nicht bekannt ist, in welchem Level der Spieler gestorben ist.")]
    [SerializeField] private string _sceneToLoad = "MainScene";
    [SerializeField] private string _mainMenuScene = "MainMenu";

    [Header("Load Game")]
    [SerializeField] private SaveSlotMenu _saveSlotMenu;
    [Tooltip("Ist ausgegraut, wenn es noch keinen Spielstand gibt.")]
    [SerializeField] private Button _loadButton;
    [Tooltip("Lädt direkt den neuesten Spielstand. Ist ausgegraut, wenn es noch keinen gibt.")]
    [SerializeField] private Button _quickLoadButton;
    [Tooltip("Wird ausgeblendet, solange das Lade-Menü offen ist (z.B. Titel und Buttons).")]
    [SerializeField] private GameObject[] _hideWhileLoadMenuOpen;

    private void Start()
    {
        // Im Spiel ist die Maus gesperrt – hier muss man klicken können.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        RefreshLoadButton();

        if (_saveSlotMenu != null) _saveSlotMenu.Closed += HandleLoadMenuClosed;
        if (SaveManager.Instance != null) SaveManager.Instance.SlotsChanged += RefreshLoadButton;
    }

    private void OnDestroy()
    {
        if (_saveSlotMenu != null) _saveSlotMenu.Closed -= HandleLoadMenuClosed;
        if (SaveManager.Instance != null) SaveManager.Instance.SlotsChanged -= RefreshLoadButton;
    }

    /// <summary>Startet das Level, in dem der Spieler gestorben ist, von vorne.</summary>
    public void RestartLevel()
    {
        string levelScene = SaveManager.Instance != null && !string.IsNullOrEmpty(SaveManager.Instance.LastGameplayScene)
            ? SaveManager.Instance.LastGameplayScene
            : _sceneToLoad;

        SceneLoader.Load(levelScene);
    }

    public void OpenLoadMenu()
    {
        if (_saveSlotMenu == null)
        {
            Debug.LogWarning("DeathScreenManager: Kein SaveSlotMenu zugewiesen.", this);
            return;
        }

        SetMenuVisible(false);
        _saveSlotMenu.Open(SaveSlotMenu.Mode.Load);
    }

    /// <summary>Lädt den neuesten Spielstand (manuell oder Autosave).</summary>
    public void QuickLoad()
    {
        if (SaveManager.Instance == null) return;

        int slot = SaveManager.Instance.GetMostRecentSlot();
        if (slot >= 0) SaveManager.Instance.Load(slot);
    }

    public void GoToMainMenu()
    {
        SceneLoader.Load(_mainMenuScene);
    }

    private void HandleLoadMenuClosed()
    {
        SetMenuVisible(true);
    }

    private void SetMenuVisible(bool isVisible)
    {
        if (_hideWhileLoadMenuOpen == null) return;

        foreach (GameObject menuObject in _hideWhileLoadMenuOpen)
        {
            if (menuObject != null) menuObject.SetActive(isVisible);
        }
    }

    private void RefreshLoadButton()
    {
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasAnySave();

        if (_loadButton != null) _loadButton.interactable = hasSave;
        if (_quickLoadButton != null) _quickLoadButton.interactable = hasSave;
    }
}
