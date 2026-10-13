using System;
using UnityEngine;

public class BossMusicTrigger : MonoBehaviour, ISaveable, ISaveLoadListener
{
    [SerializeField] private AudioSource _gameMusicSource;
    [SerializeField] private AudioClip _bossMusic;
    [SaveActiveState]
    [SerializeField] private GameObject _closeDoor;

    private bool _musicHasStarted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_musicHasStarted) return;

        _musicHasStarted = true;
        PlayBossMusic();

        if (_closeDoor != null) _closeDoor.SetActive(true);
    }

    private void PlayBossMusic()
    {
        if (_gameMusicSource == null) return;

        _gameMusicSource.Stop();
        _gameMusicSource.clip = _bossMusic;
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
        BossEncounter encounter = FindAnyObjectByType<BossEncounter>();
        bool isBossFightRunning = encounter != null && !encounter.IsBossFightOver;

        if (_musicHasStarted && isBossFightRunning)
        {
            PlayBossMusic();
        }
    }

    #endregion
}
