using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Spieler erwacht beim Levelstart langsam (z.B. nach dem Sturz in Level 1.2).
/// Während des Aufwachens kann er sich nicht bewegen, danach erscheint optional ein Tutorial.
/// Ob er schon wach ist, wird gespeichert – beim Laden kommt das Aufwachen nicht erneut.
/// </summary>
[RequireComponent(typeof(WakeUpEffect))]
public class LevelWakeUp : MonoBehaviour, ISaveable
{
    [Tooltip("Leer lassen = wird automatisch gesucht.")]
    [SerializeField] private PlayerMovement player;

    [Tooltip("Wird nach dem Aufwachen angezeigt. Leer = kein Tutorial.")]
    [SerializeField] private TutorialSO tutorialAfterWakeUp;

    private WakeUpEffect wakeUp;
    private ScreenFader fader;
    private bool hasWokenUp;

    private void Awake()
    {
        wakeUp = GetComponent<WakeUpEffect>();
        fader = GetComponent<ScreenFader>();

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerMovement>();
        }
    }

    private IEnumerator Start()
    {
        SetPlayerLocked(true);
        fader.SetAlpha(1f);

        // Der Spielstand wird erst nach Start() wiederhergestellt – bis dahin schwarz bleiben.
        yield return new WaitWhile(() => SaveManager.Instance != null && SaveManager.Instance.IsLoadingGame);

        if (hasWokenUp)
        {
            fader.SetAlpha(0f);
            SetPlayerLocked(false);
            yield break;
        }

        yield return wakeUp.Play();
        hasWokenUp = true;
        SetPlayerLocked(false);

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.StartTutorialOnce(tutorialAfterWakeUp);
        }
    }

    private void SetPlayerLocked(bool locked)
    {
        if (player != null)
        {
            player.SetControlsLocked(locked);
        }
    }

    #region Save System

    [Serializable]
    private struct SaveState
    {
        public bool hasWokenUp;
    }

    public string CaptureState()
    {
        return JsonUtility.ToJson(new SaveState { hasWokenUp = hasWokenUp });
    }

    public void RestoreState(string state)
    {
        hasWokenUp = JsonUtility.FromJson<SaveState>(state).hasWokenUp;
    }

    #endregion
}
