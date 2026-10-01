using UnityEngine;

public class Settings : MonoBehaviour
{   
    public static Settings Instance { get; private set; }

    [SerializeField] private int _fpsLock;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        Application.targetFrameRate = _fpsLock;
    }
}
