using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Ablauf der Intro-Cutscene in der Gefängniszelle:
/// 1. Spieler erwacht langsam (<see cref="WakeUpEffect"/>).
/// 2. Die Wachen laufen zu ihren Waypoints und despawnen (<see cref="CutscenePatrol"/>).
/// 3. Der Trickster spricht den Spieler automatisch an (<see cref="DialogueInteractable"/>).
/// 4. Nach dem Gespräch bebt alles (<see cref="CameraShake"/>), Schwarzblende, nächstes Level.
/// Zwischen Aufwachen und Beben kann sich der Spieler frei bewegen.
/// </summary>
[RequireComponent(typeof(ScreenFader))]
[RequireComponent(typeof(WakeUpEffect))]
public class IntroCutsceneDirector : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Leer lassen = wird automatisch gesucht.")]
    [SerializeField] private PlayerMovement player;

    [Header("Guards")]
    [Tooltip("Bei diesen Patrols 'Start Automatically' ausschalten – sie starten nach dem Aufwachen.")]
    [SerializeField] private CutscenePatrol[] guardPatrols;

    [Header("Trickster")]
    [SerializeField] private DialogueInteractable trickster;

    [Min(0f)]
    [Tooltip("Pause zwischen dem Verschwinden der Wachen und dem Gespräch.")]
    [SerializeField] private float delayBeforeDialogue = 1.5f;

    [Header("Collapse")]
    [Tooltip("Leer lassen = wird automatisch auf der Main Camera angelegt.")]
    [SerializeField] private CameraShake cameraShake;

    [Min(0f)]
    [SerializeField] private float delayBeforeCollapse = 0.5f;

    [Min(0.1f)]
    [SerializeField] private float shakeDuration = 3f;

    [Min(0f)]
    [SerializeField] private float shakeStrength = 1f;

    [SerializeField] private AudioClip collapseSound;

    [Range(0f, 1f)]
    [SerializeField] private float collapseVolume = 1f;

    [Min(0f)]
    [Tooltip("So lange nach Beginn des Bebens startet die Schwarzblende.")]
    [SerializeField] private float fadeOutDelay = 1.5f;

    [Min(0.1f)]
    [SerializeField] private float fadeOutDuration = 1.5f;

    [Header("Next Level")]
    [SerializeField] private string nextScene = "Level 1.2";

    private ScreenFader fader;
    private WakeUpEffect wakeUp;

    private void Awake()
    {
        fader = GetComponent<ScreenFader>();
        wakeUp = GetComponent<WakeUpEffect>();

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerMovement>();
        }

        if (cameraShake == null && Camera.main != null &&
            !Camera.main.TryGetComponent(out cameraShake))
        {
            cameraShake = Camera.main.gameObject.AddComponent<CameraShake>();
        }
    }

    private IEnumerator Start()
    {
        yield return WakeUp();
        yield return LetGuardsLeave();
        yield return TalkToTrickster();
        yield return Collapse();

        SceneLoader.Load(nextScene);
    }

    private IEnumerator WakeUp()
    {
        SetPlayerLocked(true);
        yield return wakeUp.Play();
        SetPlayerLocked(false);
    }

    private IEnumerator LetGuardsLeave()
    {
        CutscenePatrol[] patrols = guardPatrols.Where(patrol => patrol != null).ToArray();

        foreach (CutscenePatrol patrol in patrols)
        {
            patrol.StartPatrol();
        }

        // Zerstört (z.B. besiegt) zählt ebenfalls als fertig.
        yield return new WaitUntil(() => patrols.All(patrol => patrol == null || patrol.IsFinished));
    }

    private IEnumerator TalkToTrickster()
    {
        if (trickster == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(delayBeforeDialogue);

        bool isFinished = false;
        void HandleFinished() => isFinished = true;

        trickster.DialogueFinished += HandleFinished;
        trickster.Interact();

        yield return new WaitUntil(() => isFinished);

        trickster.DialogueFinished -= HandleFinished;
    }

    private IEnumerator Collapse()
    {
        yield return new WaitForSeconds(delayBeforeCollapse);

        SetPlayerLocked(true);
        PlayCollapseSound();

        if (cameraShake != null)
        {
            StartCoroutine(cameraShake.Shake(shakeDuration, shakeStrength));
        }

        yield return new WaitForSeconds(fadeOutDelay);
        yield return fader.FadeTo(1f, fadeOutDuration);
    }

    private void PlayCollapseSound()
    {
        if (collapseSound == null)
        {
            return;
        }

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.PlayOneShot(collapseSound, collapseVolume);
    }

    private void SetPlayerLocked(bool locked)
    {
        if (player != null)
        {
            player.SetControlsLocked(locked);
        }
    }
}
