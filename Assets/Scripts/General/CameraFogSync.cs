using UnityEngine;

/// <summary>
/// Lässt alles hinter der Sichtweite (Far Clipping Plane) wie Nebel aussehen.
///
/// Nebel färbt nur Objekte ein, die gerendert werden – hinter "Far" wird nichts gerendert,
/// dort sieht man den Kamera-Hintergrund. Deshalb:
///  1. Kamera-Hintergrund = Nebelfarbe
///  2. Nebel ist linear und kurz VOR "Far" komplett dicht
/// Dann verschwinden Objekte weich im Nebel, bevor sie abgeschnitten werden.
///
/// Auf die Kamera legen (die Kamera, die die Spielwelt rendert). Funktioniert auch im Editor.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraFogSync : MonoBehaviour
{
    [Tooltip("Ab dieser Entfernung beginnt der Nebel.")]
    [Min(0f)]
    [SerializeField] private float fogStartDistance = 12f;

    [Tooltip("So viele Meter vor 'Far' ist der Nebel komplett dicht.")]
    [Min(0f)]
    [SerializeField] private float fullFogMarginBeforeFar = 3f;

    [Tooltip("Nebel- und Hintergrundfarbe.")]
    [SerializeField] private Color fogColor = new(0.62f, 0.41f, 0.39f, 1f);

    private Camera targetCamera;

    private void OnEnable()
    {
        targetCamera = GetComponent<Camera>();
        Apply();
    }

    private void OnValidate()
    {
        targetCamera = GetComponent<Camera>();
        Apply();
    }

    private void Update()
    {
        // Falls Far zur Laufzeit / im Inspector geändert wird.
        Apply();
    }

    private void Apply()
    {
        if (targetCamera == null)
        {
            return;
        }

        float fogEnd = Mathf.Max(
            fogStartDistance + 0.1f,
            targetCamera.farClipPlane - fullFogMarginBeforeFar
        );

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = fogEnd;

        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = fogColor;
    }
}
