using UnityEngine;

public class Settings : MonoBehaviour
{   
    public static Settings Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        Application.targetFrameRate = 60;
    }

    public void ChangeFPSLock(int fpsLock)
    {
        Application.targetFrameRate = fpsLock;
    }

    public void UnlockFPS()
    {
        Application.targetFrameRate = 0;
    }
}
