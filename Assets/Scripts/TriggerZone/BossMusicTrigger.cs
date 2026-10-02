using UnityEngine;

public class BossMusicTrigger : MonoBehaviour
{
    [SerializeField] private AudioSource _gameMusicSource;
    [SerializeField] private AudioClip _bossMusic;
    [SerializeField] private GameObject _closeDoor;

    private bool _musicHasStarted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_musicHasStarted) return;

        _gameMusicSource.Stop();
        _gameMusicSource.clip = _bossMusic;
        _gameMusicSource.loop = true;
        _gameMusicSource.Play();

        if (_closeDoor != null) _closeDoor.SetActive(true);
    }
}
