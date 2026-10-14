using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Visuelles und akustisches Feedback für einen Treffer (Partikel + zufälliger Sound).
/// Wird von <see cref="HitFeedback"/> (Gegner, Nerven, Kisten) und <see cref="SurfaceType"/>
/// (Wände/Boden je nach Material) genutzt. Leere Felder werden einfach übersprungen.
/// </summary>
[Serializable]
public class ImpactEffect
{
    [Header("VFX")]
    [SerializeField] private GameObject vfx;

    [Min(0.1f)]
    [SerializeField] private float vfxLifetime = 1f;

    [Tooltip("Zufällige Drehung statt Ausrichtung an der getroffenen Oberfläche.")]
    [SerializeField] private bool randomizeRotation;

    [Header("Sound")]
    [Tooltip("Pro Treffer wird ein zufälliger Clip gespielt.")]
    [SerializeField] private AudioClip[] sounds;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Range(0f, 0.5f)]
    [SerializeField] private float pitchVariation = 0.1f;

    [Min(1f)]
    [Tooltip("Ab dieser Entfernung ist der Sound nicht mehr zu hören.")]
    [SerializeField] private float maxHearingDistance = 20f;

    public void Play(Vector3 position, Vector3 surfaceNormal)
    {
        SpawnVfx(position, surfaceNormal);
        PlaySound(position);
    }

    private void SpawnVfx(Vector3 position, Vector3 surfaceNormal)
    {
        if (vfx == null)
        {
            return;
        }

        Quaternion rotation = GetVfxRotation(surfaceNormal);
        GameObject instance = UnityEngine.Object.Instantiate(vfx, position, rotation);

        UnityEngine.Object.Destroy(instance, vfxLifetime);
    }

    private Quaternion GetVfxRotation(Vector3 surfaceNormal)
    {
        if (randomizeRotation || surfaceNormal.sqrMagnitude < 0.001f)
        {
            return Random.rotation;
        }

        return Quaternion.LookRotation(surfaceNormal);
    }

    private void PlaySound(Vector3 position)
    {
        AudioClip clip = GetRandomClip();

        if (clip == null)
        {
            return;
        }

        // Eigene, kurzlebige 3D-Quelle: Der Sound spielt auch dann zu Ende,
        // wenn das getroffene Objekt im selben Frame zerstört wird.
        GameObject soundObject = new("ImpactSound");
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = maxHearingDistance;
        source.Play();

        UnityEngine.Object.Destroy(soundObject, clip.length / source.pitch + 0.1f);
    }

    private AudioClip GetRandomClip()
    {
        if (sounds == null || sounds.Length == 0)
        {
            return null;
        }

        return sounds[Random.Range(0, sounds.Length)];
    }
}
