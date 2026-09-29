using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WeaponSoundManager : MonoBehaviour
{
    [SerializeField] private List<AudioClip> _soundEffects;

    private AudioSource _soundSource;
    private AudioClip _previousSound;

    private void Start()
    {
        _soundSource = GetComponent<AudioSource>();
        _previousSound = null;
    }

    public void GetSoundEffects(AudioClip[] effects)
    {
        if (_soundEffects.Count > 0) _soundEffects.Clear();
        _soundEffects.AddRange(effects);
    }

    public void PlayRandomAttackSound()
    {
        List<AudioClip> soundPool = GetValidSounds();

        AudioClip chosen = soundPool[Random.Range(0, soundPool.Count)];

        _previousSound = chosen;
        _soundSource.PlayOneShot(chosen);
    }

    private List<AudioClip> GetValidSounds()
    {
        List<AudioClip> validSounds = new List<AudioClip>();

        foreach (AudioClip sound in _soundEffects)
        {
            if (sound == _previousSound) continue;
            validSounds.Add(sound);
        }

        return validSounds;
    }
}
