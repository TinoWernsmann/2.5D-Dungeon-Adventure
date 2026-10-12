using UnityEngine;

/// <summary>
/// Spielt Schrittgeräusche ab. Weiß nicht, WER sich bewegt – das Bewegungs-Script
/// (Player, Gegner, Boss) meldet nur Geschwindigkeit, Sprung und Landung.
///
/// Schritte werden nach gelaufener Strecke getaktet: schneller laufen = schnellere Schritte.
/// Pro Ereignis wird ein zufälliger Clip gewählt, nie zweimal derselbe hintereinander.
///
/// Wichtig: Braucht eine EIGENE AudioSource, weil die Tonhöhe pro Schritt verändert wird.
/// Teilt sie sich die Quelle mit Angriffssounds, klingen diese sonst ebenfalls verstimmt.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class FootstepAudio : MonoBehaviour
{
    private const int SoundTypeCount = 4;

    [Header("Sounds")]
    [SerializeField] private FootstepSoundSet soundSet;
    [SerializeField] private AudioSource audioSource;

    [Header("Hearing Range")]
    [Tooltip("Bis zu dieser Entfernung voll laut (der Player hört seine eigenen Schritte daher immer voll).")]
    [Min(0f)]
    [SerializeField] private float fullVolumeDistance = 2f;

    [Tooltip("Ab dieser Entfernung sind die Schritte nicht mehr zu hören.")]
    [Min(0.1f)]
    [SerializeField] private float maxHearingDistance = 15f;

    [Header("Walk")]
    [Tooltip("Gelaufene Strecke zwischen zwei Schritten (in Metern).")]
    [Min(0.1f)]
    [SerializeField] private float stepDistance = 1.8f;

    [Tooltip("Ab dieser Geschwindigkeit zählt die Bewegung als Laufen.")]
    [Min(0f)]
    [SerializeField] private float minMoveSpeed = 0.5f;

    [Header("Stop")]
    [Tooltip("Der Stopp-Sound kommt nur, wenn vorher mindestens so weit gelaufen wurde.")]
    [Min(0f)]
    [SerializeField] private float minWalkDistanceForStop = 1f;

    private readonly int[] lastPlayedIndex = { -1, -1, -1, -1 };

    private float distanceSinceLastStep;
    private float walkedDistance;
    private bool wasWalking;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ConfigureAsSpatialSound();
    }

    /// <summary>
    /// 3D-Sound mit begrenzter Hörweite. Als 2D-Sound wären die Schritte JEDES Gegners
    /// im ganzen Level gleich laut zu hören – auch wenn er weit weg in einem anderen Raum läuft.
    /// </summary>
    private void ConfigureAsSpatialSound()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = fullVolumeDistance;
        audioSource.maxDistance = Mathf.Max(maxHearingDistance, fullVolumeDistance + 0.1f);
    }

    /// <summary>Jeden Frame vom Bewegungs-Script aufrufen.</summary>
    public void UpdateMovement(float horizontalSpeed, bool isGrounded, float deltaTime)
    {
        bool isWalking = isGrounded && horizontalSpeed >= minMoveSpeed;

        if (isWalking)
        {
            UpdateWalking(horizontalSpeed * deltaTime);
        }
        else if (wasWalking && isGrounded && walkedDistance >= minWalkDistanceForStop)
        {
            Play(FootstepSoundType.Stop);
        }

        wasWalking = isWalking;
    }

    public void PlayJump()
    {
        Play(FootstepSoundType.Jump);
    }

    public void PlayLand()
    {
        Play(FootstepSoundType.Land);
    }

    private void UpdateWalking(float distanceThisFrame)
    {
        if (!wasWalking)
        {
            // Erster Schritt kommt schnell, nicht erst nach einer ganzen Schrittlänge.
            distanceSinceLastStep = stepDistance * 0.5f;
            walkedDistance = 0f;
        }

        distanceSinceLastStep += distanceThisFrame;
        walkedDistance += distanceThisFrame;

        if (distanceSinceLastStep < stepDistance)
        {
            return;
        }

        distanceSinceLastStep -= stepDistance;
        Play(FootstepSoundType.Walk);
    }

    private void Play(FootstepSoundType type)
    {
        if (soundSet == null || audioSource == null)
        {
            return;
        }

        AudioClip clip = PickRandomClip(type);

        if (clip == null)
        {
            return;
        }

        float variation = soundSet.PitchVariation;
        audioSource.pitch = 1f + Random.Range(-variation, variation);

        audioSource.PlayOneShot(clip, soundSet.Volume);
    }

    /// <summary>Zufälliger Clip, aber nie derselbe wie beim letzten Mal.</summary>
    private AudioClip PickRandomClip(FootstepSoundType type)
    {
        AudioClip[] clips = soundSet.GetClips(type);

        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        int typeIndex = (int)type % SoundTypeCount;
        int index = Random.Range(0, clips.Length);

        if (clips.Length > 1 && index == lastPlayedIndex[typeIndex])
        {
            index = (index + 1) % clips.Length;
        }

        lastPlayedIndex[typeIndex] = index;

        return clips[index];
    }
}
