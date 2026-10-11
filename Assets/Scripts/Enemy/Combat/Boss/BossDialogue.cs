using System.Collections;
using UnityEngine;

/// <summary>
/// Dialoge rund um den Boss-Kampf (nutzt das vorhandene <see cref="DialogueUIManager"/>
/// und <see cref="DialogueBase"/>-Assets).
///
/// Intro: Startet beim ersten Sichtkontakt. Der Boss wartet, der Player bleibt steuerbar,
///        die Zeilen laufen automatisch weiter. Greift der Player an, startet der Kampf sofort.
///
/// Niederlage: Startet kurz nach dem Tod des Bosses.
/// </summary>
[RequireComponent(typeof(SpearBossCombatBehaviour))]
public class BossDialogue : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpearBossCombatBehaviour boss;
    [SerializeField] private Health health;
    [SerializeField] private BossVisualController visualController;

    [Tooltip("Leer lassen = wird automatisch in der Szene gesucht.")]
    [SerializeField] private DialogueUIManager dialogueUI;

    [Header("Intro (vor dem Kampf)")]
    [SerializeField] private DialogueBase introDialogue;

    [Min(0.5f)]
    [SerializeField] private float introSecondsPerLine = 2.5f;

    [Header("Defeat (nach dem Kampf)")]
    [SerializeField] private DialogueBase defeatDialogue;

    [Tooltip("Wartezeit nach dem Tod, damit die Niederlage-Animation erst zu sehen ist.")]
    [Min(0f)]
    [SerializeField] private float defeatDialogueDelay = 1.5f;

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

        if (health == null)
        {
            health = GetComponent<Health>();
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
        health.Damaged += HandleDamaged;
        health.Died += HandleDeath;
    }

    private void OnDisable()
    {
        boss.PlayerSpottedFirstTime -= StartIntro;
        health.Damaged -= HandleDamaged;
        health.Died -= HandleDeath;

        StopListeningForDialogueInput();
    }

    #region Intro

    private void StartIntro()
    {
        if (hasPlayedIntro || !CanShow(introDialogue))
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

    /// <summary>Angriff während des Intros -> Kampf startet sofort.</summary>
    private void HandleDamaged(int damage)
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

    private void HandleDeath()
    {
        StartCoroutine(StartDefeatDialogueAfterDelay());
    }

    private IEnumerator StartDefeatDialogueAfterDelay()
    {
        yield return new WaitForSeconds(defeatDialogueDelay);

        if (!CanShow(defeatDialogue) || GameInput.Instance == null)
        {
            ShowFinalDefeatPose();
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

        ShowFinalDefeatPose();
    }

    private void ShowFinalDefeatPose()
    {
        if (visualController != null)
        {
            visualController.ShowFinalDefeatPose();
        }
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
