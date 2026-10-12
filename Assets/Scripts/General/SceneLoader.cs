using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Zentrale Stelle für Szenenwechsel mit Ladebildschirm:
/// Zuerst wird die kleine "LoadingScene" angezeigt, die dann die eigentliche Szene
/// im Hintergrund lädt (siehe <see cref="LoadingScreen"/>).
///
/// Statt SceneManager.LoadScene("X") einfach SceneLoader.Load("X") aufrufen.
/// </summary>
public static class SceneLoader
{
    public const string LoadingSceneName = "LoadingScene";

    /// <summary>Die Szene, die der Ladebildschirm als Nächstes laden soll.</summary>
    public static string PendingScene { get; private set; }

    public static void Load(string sceneName)
    {
        Time.timeScale = 1f;

        // Falls die LoadingScene (noch) nicht in den Build Settings ist: direkt laden.
        if (!Application.CanStreamedLevelBeLoaded(LoadingSceneName))
        {
            Debug.LogWarning($"[SceneLoader] '{LoadingSceneName}' fehlt in den Build Settings – lade ohne Ladebildschirm.");
            SceneManager.LoadScene(sceneName);
            return;
        }

        PendingScene = sceneName;
        SceneManager.LoadScene(LoadingSceneName);
    }

    /// <summary>Holt die Zielszene ab (nur einmal gültig).</summary>
    public static string TakePendingScene()
    {
        string sceneName = PendingScene;
        PendingScene = null;
        return sceneName;
    }
}
