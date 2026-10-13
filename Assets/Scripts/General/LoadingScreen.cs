using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Liegt in der LoadingScene. Lädt die Zielszene (<see cref="SceneLoader.PendingScene"/>)
/// im Hintergrund, während der Ladebildschirm zu sehen ist, und wechselt dann dorthin.
///
/// Der Ladebildschirm bleibt beim Wechsel noch kurz über der neuen Szene liegen –
/// bis ein Spielstand vollständig wiederhergestellt ist. Sonst sähe man kurz den
/// Level-Anfang, bevor der Spieler an die gespeicherte Position springt.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    private const float LoadedProgress = 0.9f; // Unity meldet 0.9, sobald alles geladen ist.
    private const int TopSortingOrder = 1000;

    [Tooltip("So lange bleibt der Ladebildschirm mindestens sichtbar (verhindert kurzes Aufblitzen).")]
    [Min(0f)]
    [SerializeField] private float minimumDisplayTime = 0.75f;

    [Tooltip("Der Canvas des Ladebildschirms. Leer lassen = wird automatisch gesucht.")]
    [SerializeField] private Canvas screenCanvas;

    [Tooltip("Optional: Image mit Image Type 'Filled' als Ladebalken.")]
    [SerializeField] private Image progressBar;

    [Tooltip("Wird geladen, falls die LoadingScene direkt gestartet wurde (z.B. im Editor).")]
    [SerializeField] private string fallbackScene = "MainMenu";

    private IEnumerator Start()
    {
        if (screenCanvas == null)
        {
            screenCanvas = FindAnyObjectByType<Canvas>();
        }

        string targetScene = SceneLoader.TakePendingScene();

        if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = fallbackScene;
        }

        yield return LoadInBackground(targetScene);
        yield return WaitUntilGameIsReady();

        if (screenCanvas != null)
        {
            Destroy(screenCanvas.transform.root.gameObject);
        }

        Destroy(gameObject);
    }

    private IEnumerator LoadInBackground(string targetScene)
    {
        AsyncOperation loading = SceneManager.LoadSceneAsync(targetScene);
        loading.allowSceneActivation = false;

        float elapsed = 0f;

        while (loading.progress < LoadedProgress || elapsed < minimumDisplayTime)
        {
            elapsed += Time.unscaledDeltaTime;
            ShowProgress(loading.progress / LoadedProgress);

            yield return null;
        }

        ShowProgress(1f);
        KeepScreenDuringSceneSwitch();

        loading.allowSceneActivation = true;

        while (!loading.isDone)
        {
            yield return null;
        }
    }

    /// <summary>Ladebildschirm überlebt den Szenenwechsel und liegt über allem.</summary>
    private void KeepScreenDuringSceneSwitch()
    {
        DontDestroyOnLoad(gameObject);

        if (screenCanvas == null)
        {
            return;
        }

        screenCanvas.sortingOrder = TopSortingOrder;
        DontDestroyOnLoad(screenCanvas.transform.root.gameObject);
    }

    /// <summary>Wartet, bis ein geladener Spielstand vollständig wiederhergestellt ist.</summary>
    private static IEnumerator WaitUntilGameIsReady()
    {
        // Ein Frame, damit Awake/Start der neuen Szene laufen.
        yield return null;

        while (SaveManager.Instance != null && SaveManager.Instance.IsLoadingGame)
        {
            yield return null;
        }

        // Noch ein Frame, damit der wiederhergestellte Zustand einmal gerendert wurde.
        yield return null;
    }

    private void ShowProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = Mathf.Clamp01(progress);
        }
    }
}
