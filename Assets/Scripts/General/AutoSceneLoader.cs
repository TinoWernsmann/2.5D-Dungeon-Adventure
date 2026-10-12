using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Lädt nach einer Wartezeit automatisch die nächste Szene (z.B. Credits → Hauptmenü).
/// </summary>
public class AutoSceneLoader : MonoBehaviour
{
    [FormerlySerializedAs("_sceneToLoad")]
    [SerializeField] private string sceneToLoad = "MainMenu";

    [FormerlySerializedAs("TIME_TO_WAIT")]
    [Min(0f)]
    [SerializeField] private float delaySeconds = 10f;

    private IEnumerator Start()
    {
        // Echtzeit: läuft auch weiter, falls die Spielzeit noch angehalten ist (Pause).
        yield return new WaitForSecondsRealtime(delaySeconds);

        SceneLoader.Load(sceneToLoad);
    }
}
