using System;
using UnityEngine;

/// <summary>
/// Startet beim Betreten die Boss-Musik und schließt optional eine Tür hinter dem Player.
/// Optional mit zugewiesenem Boss: Nach seinem Sieg verschwindet eine Tür
/// und die normale Hintergrundmusik läuft weiter.
/// </summary>
public class BossMusicTrigger : MonoBehaviour, ISaveable, ISaveLoadListener
{
    [SerializeField] private AudioSource _gameMusicSource;
    [SerializeField] private AudioClip _bossMusic;
    [SaveActiveState]
    [SerializeField] private GameObject _closeDoor;

    [Header("Boss besiegt (optional)")]
    [Tooltip("Leer = Musik/Tür werden vom Boss selbst gesteuert (z.B. Speer-Boss).")]
    [SerializeField] private Health _boss;
    [SaveActiveState]
    [SerializeField] private GameObject _doorToDespawn;

    private bool _musicHasStarted = false;
    private AudioClip _backgroundMusic;

    private void Awake()
    {
        if (_gameMusicSource != null) _backgroundMusic = _gameMusicSource.clip;
    }

    private void OnEnable()
    {
        if (_boss != null) _boss.Died += HandleBossDefeated;
    }

    private void OnDisable()
    {
        if (_boss != null) _boss.Died -= HandleBossDefeated;
    }

    private void HandleBossDefeated()
    {
        if (_doorToDespawn != null) _doorToDespawn.SetActive(false);

        PlayMusic(_backgroundMusic);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_musicHasStarted) return;
        if (_boss != null && _boss.IsDead) return;

        _musicHasStarted = true;
        PlayBossMusic();

        if (_closeDoor != null) _closeDoor.SetActive(true);
    }

    private void PlayBossMusic()
    {
        PlayMusic(_bossMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (_gameMusicSource == null) return;

        _gameMusicSource.Stop();
        _gameMusicSource.clip = clip;
        _gameMusicSource.loop = true;
        _gameMusicSource.Play();
    }

    #region Save System

    // Ob die Tür zu ist, speichert die SaveableEntity der Tür ([SaveActiveState]).
    [Serializable]
    private struct SaveState
    {
        public bool musicHasStarted;
    }

    public string CaptureState()
    {
        return JsonUtility.ToJson(new SaveState { musicHasStarted = _musicHasStarted });
    }

    public void RestoreState(string state)
    {
        _musicHasStarted = JsonUtility.FromJson<SaveState>(state).musicHasStarted;
    }

    /// <summary>Boss-Musik nur fortsetzen, wenn der Kampf noch läuft.</summary>
    public void OnGameLoaded()
    {
        bool isBossFightRunning = _boss != null
            ? !_boss.IsDead
            : IsBossEncounterRunning();

        if (_musicHasStarted && isBossFightRunning)
        {
            PlayBossMusic();
        }
    }

    private static bool IsBossEncounterRunning()
    {
        BossEncounter encounter = FindAnyObjectByType<BossEncounter>();
        return encounter != null && !encounter.IsBossFightOver;
    }

    #endregion
}
