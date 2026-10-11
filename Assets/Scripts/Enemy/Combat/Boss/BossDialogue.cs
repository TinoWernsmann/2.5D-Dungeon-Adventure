using System.Collections;
using UnityEngine;

/// <summary>
/// Dialoge rund um den Boss-Kampf (nutzt das vorhandene <see cref="DialogueUIManager"/>
/// und <see cref="DialogueBase"/>-Assets).
///
/// Intro: Startet beim ersten Sichtkontakt. Der Boss wartet, der Player bleibt steuerbar,
///        die Zeilen laufen automatisch weiter. Greift der Player an, startet der Kampf sofort.
///
/// Niederlage: Startet, sobald der Boss UND seine Wachen besiegt sind (siehe <see cref="BossEncounter"/>).
/// </summary>
[RequireComponent(typeof(SpearBossCombatBehaviour))]
[RequireComponent(typeof(BossEncounter))]
public class BossDialogue : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpearBossCombatBehaviour boss;
    [SerializeField] private BossEncounter encounter;
    [SerializeField] private BossVisualController visualController;

    [Tooltip("Leer lassen = wird automatisch in der Szene gesucht.")]
    [SerializeField] private DialogueUIManager dialogueUI;

    [Header("Intro (vor dem Kampf)")]
    [SerializeField] private DialogueBase introDialogue;

    [Min(0.5f)]
    [SerializeField] private float introSecondsPerLine = 2.5f;

    [Header("Defeat (nach dem Kampf)")]
    [SerializeField] private DialogueBase defeatDialogue;

    [Tooltip("Wartezeit nach dem letzten besiegten Gegner, damit die Niederlage-Animation erst zu sehen ist.")]
    [Min(0f)]
    [SerializeField] private float defeatDialogueDelay = 1.5f;

    [Header("Despawn (nach dem Dialog)")]
    [Tooltip("So lange bleibt der Boss nach dem Dialog sichtbar.")]
    [Min(0f)]
    [SerializeField] private float despawnDelay = 5f;

    [Min(0.01f)]
    [SerializeField] private float fadeOutDuration = 1.5f;

    private Coroutine introRoutine;

    private bool hasPlayedIntro;
    private bool isDefeatDialogueActive;
    private int defeatLineIndex;

    private void Awake()
    {
        if (boss == null)
        {
            boss = GetComponent<SpearBossCombatBehaviour>();
        }

        if (encounter == null)
        {
            encounter = GetComponent<BossEncounter>();
        }

        if (visualController == null)
        {
            visualController = GetComponentInChildren<BossVisualController>();
        }

        if (dialogueUI == null)
        {
            dialogueUI = FindAnyObjectByType<DialogueUIManager>(FindObjectsInactive.Include);
        }
    }

    private void OnEnable()
    {
        boss.PlayerSpottedFirstTime += StartIntro;
        encounter.Provoked += HandleEncounterProvoked;
        encounter.Completed += HandleEncounterCompleted;
    }

    private void OnDisable()
    {
        boss.PlayerSpottedFirstTime -= StartIntro;
        encounter.Provoked -= HandleEncounterProvoked;
        encounter.Completed -= HandleEncounterCompleted;

        StopListeningForDialogueInput();
    }

    #region Intro

    private void StartIntro()
    {
        // Hat der Kampf schon begonnen (z.B. eine Wache angegriffen), kein Intro mehr.
        if (hasPlayedIntro ||
            encounter.IsProvoked ||
            !CanShow(introDialogue))
        {
            return;
        }

        hasPlayedIntro = true;

        boss.HoldEncounter();
        introRoutine = StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        string[] lines = introDialogue.SpokenText;

        for (int i = 0; i < lines.Length; i++)
        {
            // Läuft automatisch weiter -> kein "E drücken"-Hinweis.
            ShowLine(introDialogue, i, showContinueHint: false);
            yield return new WaitForSeconds(introSecondsPerLine);
        }

        EndIntro();
    }

    /// <summary>Angriff auf den Boss oder eine Wache während des Intros -> Kampf startet sofort.</summary>
    private void HandleEncounterProvoked()
    {
        if (introRoutine == null)
        {
            return;
        }

        StopCoroutine(introRoutine);
        EndIntro();
    }

    private void EndIntro()
    {
        introRoutine = null;

        dialogueUI.HideSpeakingUI();
        boss.ReleaseEncounter();
    }

    #endregion

    #region Defeat

    private void HandleEncounterCompleted()
    {
        StartCoroutine(StartDefeatDialogueAfterDelay());
    }

    private IEnumerator StartDefeatDialogueAfterDelay()
    {
        yield return new WaitForSeconds(defeatDialogueDelay);

        if (!CanShow(defeatDialogue) || GameInput.Instance == null)
        {
            FinishDefeat();
            yield break;
        }

        isDefeatDialogueActive = true;
        defeatLineIndex = 0;

        GameInput.Instance.EnableDialogue();
        GameInput.Instance.OnDialogue += AdvanceDefeatDialogue;

        ShowLine(defeatDialogue, defeatLineIndex, showContinueHint: true);
    }

    private void AdvanceDefeatDialogue()
    {
        defeatLineIndex++;

        if (defeatLineIndex < defeatDialogue.SpokenText.Length)
        {
            ShowLine(defeatDialogue, defeatLineIndex, showContinueHint: true);
            return;
        }

        EndDefeatDialogue();
    }

    private void EndDefeatDialogue()
    {
        StopListeningForDialogueInput();

        GameInput.Instance.DisableDialogue();
        dialogueUI.HideSpeakingUI();

        FinishDefeat();
    }

    /// <summary>DefeatCry zeigen, kurz stehen lassen, dann ausblenden und entfernen.</summary>
    private void FinishDefeat()
    {
        if (visualController == null)
        {
            return;
        }

        visualController.ShowFinalDefeatPose();
        visualController.FadeOutAndDestroy(despawnDelay, fadeOutDuration);
    }

    private void StopListeningForDialogueInput()
    {
        if (!isDefeatDialogueActive)
        {
            return;
        }

        isDefeatDialogueActive = false;

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnDialogue -= AdvanceDefeatDialogue;
        }
    }

    #endregion

    private bool CanShow(DialogueBase dialogue)
    {
        return dialogueUI != null &&
               dialogue != null &&
               dialogue.SpokenText != null &&
               dialogue.SpokenText.Length > 0;
    }

    private void ShowLine(DialogueBase dialogue, int lineIndex, bool showContinueHint)
    {
        dialogueUI.ShowSpeakingUI(
            dialogue.SpokenText[lineIndex],
            dialogue.SpeakerName,
            showContinueHint
        );
    }
}
