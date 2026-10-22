using System;
using UnityEngine;

/// <summary>
/// Dauerfeuer-Phase: Ab einem bestimmten Lebensanteil zaubert der Gegner
/// regelmäßig für kurze Zeit deutlich schneller. Startet mit einem Ankündigungs-Sound.
/// </summary>
[Serializable]
public class RapidFireMode
{
    [Tooltip("Unter diesem Lebensanteil wird das Dauerfeuer freigeschaltet. 0 = aus.")]
    [Range(0f, 1f)]
    [SerializeField] private float unlockHealthPercent;

    [Min(0.1f)]
    [SerializeField] private float duration = 3f;

    [Tooltip("Pause zwischen zwei Dauerfeuer-Phasen.")]
    [Min(0f)]
    [SerializeField] private float cooldown = 12f;

    [Tooltip("So viel schneller werden Zauber und Erholung während des Dauerfeuers.")]
    [Min(1f)]
    [SerializeField] private float speedMultiplier = 4f;

    [SerializeField] private AudioClip startSound;

    private float activeTimer;
    private float cooldownTimer;

    public bool IsActive => activeTimer > 0f;

    public float SpeedMultiplier => IsActive ? speedMultiplier : 1f;

    /// <summary>Gibt true zurück, wenn das Dauerfeuer in diesem Frame beginnt.</summary>
    public bool Tick(float deltaTime, float healthPercent)
    {
        if (IsActive)
        {
            activeTimer -= deltaTime;
            return false;
        }

        cooldownTimer = Mathf.Max(0f, cooldownTimer - deltaTime);

        bool isUnlocked = unlockHealthPercent > 0f && healthPercent <= unlockHealthPercent;

        if (!isUnlocked || cooldownTimer > 0f)
        {
            return false;
        }

        activeTimer = duration;
        cooldownTimer = cooldown;
        return true;
    }

    public void PlayStartSound(AudioSource audioSource)
    {
        if (audioSource != null && startSound != null)
        {
            audioSource.PlayOneShot(startSound);
        }
    }
}
