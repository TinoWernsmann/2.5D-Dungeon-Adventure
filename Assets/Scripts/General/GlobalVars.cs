using UnityEngine;

public class GlobalVars : MonoBehaviour
{
    public static GlobalVars Instance { get; private set; }
    public string PlayerName { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void SetPlayerName(string playerName)
    {
        PlayerName = playerName;
    }
}
