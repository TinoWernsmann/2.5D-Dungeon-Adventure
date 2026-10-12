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
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;
    }

    public void ChangeFPSLock(int fpsLock)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fpsLock;
    }

    public void UnlockFPS()
    {
        QualitySettings.vSyncCount = 0;

        // -1 = kein Limit (0 ist in Unity kein gültiges "unbegrenzt").
        Application.targetFrameRate = -1;
    }

    /// <summary>
    /// FPS = Bildwiederholrate des Monitors, in exakt gleichen Abständen.
    /// Wirkt am flüssigsten, weil jedes Bild genau ein Monitor-Bild lang zu sehen ist.
    /// </summary>
    public void EnableVSync()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;
    }
}
