using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WeaponSoundManager : MonoBehaviour
{
    private readonly List<AudioClip> attackSounds = new();

    private AudioSource audioSource;
    private AudioClip previousSound;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void SetAttackSounds(AudioClip[] sounds)
    {
        attackSounds.Clear();
        previousSound = null;

        if (sounds == null)
        {
            return;
        }

        foreach (AudioClip sound in sounds)
        {
            if (sound != null)
            {
                attackSounds.Add(sound);
            }
        }
    }

    public void ClearAttackSounds()
    {
        attackSounds.Clear();
        previousSound = null;
    }

    public void PlayRandomAttackSound()
    {
        if (audioSource == null || attackSounds.Count == 0)
        {
            return;
        }

        AudioClip sound = GetRandomAttackSound();

        if (sound == null)
        {
            return;
        }

        previousSound = sound;
        audioSource.PlayOneShot(sound);
    }

    private AudioClip GetRandomAttackSound()
    {
        if (attackSounds.Count == 1)
        {
            return attackSounds[0];
        }

        List<AudioClip> availableSounds = new();

        foreach (AudioClip sound in attackSounds)
        {
            if (sound != previousSound)
            {
                availableSounds.Add(sound);
            }
        }

        if (availableSounds.Count == 0)
        {
            return attackSounds[0];
        }

        return availableSounds[
            Random.Range(0, availableSounds.Count)
        ];
    }
}