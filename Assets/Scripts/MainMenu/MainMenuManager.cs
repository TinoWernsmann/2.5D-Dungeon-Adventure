using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private const string MAIN_SCENE = "TinoScene";

    public void StartGame()
    {
        SceneManager.LoadScene(MAIN_SCENE);
    }
}
