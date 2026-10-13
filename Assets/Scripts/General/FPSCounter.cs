using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// FPS-Anzeige oben links. Mit F3 ein- und ausblenden.
/// Erzeugt sich beim Spielstart automatisch – muss in keine Szene gelegt werden.
///
/// Zeigt den Durchschnitt der letzten halben Sekunde, die Bildzeit in ms,
/// das langsamste Bild (Ruckler) und das eingestellte FPS-Limit.
/// </summary>
public class FPSCounter : MonoBehaviour
{
    private const float UpdateInterval = 0.5f;

    private bool isVisible;

    private int framesSinceUpdate;
    private float timeSinceUpdate;
    private float slowestFrameSinceUpdate;

    private float averageFps;
    private float averageFrameMs;
    private float slowestFrameMs;

    private GUIStyle labelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstance()
    {
        GameObject counterObject = new("FPSCounter");
        counterObject.AddComponent<FPSCounter>();
        DontDestroyOnLoad(counterObject);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
        {
            isVisible = !isVisible;
        }

        MeasureFrame(Time.unscaledDeltaTime);
    }

    private void MeasureFrame(float frameTime)
    {
        framesSinceUpdate++;
        timeSinceUpdate += frameTime;
        slowestFrameSinceUpdate = Mathf.Max(slowestFrameSinceUpdate, frameTime);

        if (timeSinceUpdate < UpdateInterval)
        {
            return;
        }

        averageFps = framesSinceUpdate / timeSinceUpdate;
        averageFrameMs = timeSinceUpdate / framesSinceUpdate * 1000f;
        slowestFrameMs = slowestFrameSinceUpdate * 1000f;

        framesSinceUpdate = 0;
        timeSinceUpdate = 0f;
        slowestFrameSinceUpdate = 0f;
    }

    private void OnGUI()
    {
        if (!isVisible)
        {
            return;
        }

        labelStyle ??= new GUIStyle(GUI.skin.box)
        {
            fontSize = 18,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = Color.white }
        };

        string limit = Application.targetFrameRate > 0
            ? Application.targetFrameRate.ToString()
            : "Unlocked";

        string vSync = QualitySettings.vSyncCount > 0 ? "On" : "Off";
        double monitorHz = Screen.currentResolution.refreshRateRatio.value;

        string text =
            $"FPS: {averageFps:0}\n" +
            $"Frame: {averageFrameMs:0.0} ms\n" +
            $"Slowest: {slowestFrameMs:0.0} ms\n" +
            $"Limit: {limit}   VSync: {vSync}\n" +
            $"Monitor: {monitorHz:0} Hz";

        GUI.Box(new Rect(10f, 10f, 280f, 125f), text, labelStyle);
    }
}
