using System.Collections;
using UnityEngine;

public class TimedObjectDestroyer : MonoBehaviour
{
    [Header("Destruction")]

    [Tooltip("Time in seconds until this GameObject is destroyed.")]
    [SerializeField] private float destroyDelay = 10f;

    [Tooltip("Time in seconds before destruction when the particle effect starts.")]
    [SerializeField] private float particleStartTime = 7f;

    [Tooltip("Start the timer automatically when the object starts.")]
    [SerializeField] private bool startAutomatically = true;


    [Header("Particle Effect")]

    [Tooltip("Particle System that should play before the object is destroyed.")]
    [SerializeField] private ParticleSystem particleEffect;

    [Tooltip("How long the particle effect remains after the object is destroyed.")]
    [SerializeField] private float particleFadeDuration = 5f;


    [Header("Particle Sound")]

    [Tooltip("Sound played when the particle effect starts.")]
    [SerializeField] private AudioClip particleSound;

    [Tooltip("Optional AudioSource. If empty, a temporary AudioSource is created.")]
    [SerializeField] private AudioSource audioSource;

    [Range(0f, 1f)]
    [SerializeField] private float particleSoundVolume = 1f;


    private Coroutine destroyCoroutine;
    private bool isRunning;


    private void Start()
    {
        if (startAutomatically)
        {
            StartTimer();
        }
    }

    public void StartTimer()
    {
        if (isRunning)
        {
            return;
        }

        destroyCoroutine = StartCoroutine(DestroySequence());
    }

    public void StopTimer()
    {
        if (!isRunning)
        {
            return;
        }

        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
        }

        destroyCoroutine = null;
        isRunning = false;
    }


    private IEnumerator DestroySequence()
    {
        isRunning = true;

        float clampedDestroyDelay =
            Mathf.Max(0f, destroyDelay);

        float clampedParticleStartTime =
            Mathf.Clamp(
                particleStartTime,
                0f,
                clampedDestroyDelay
            );

        float waitBeforeParticle =
            clampedDestroyDelay -
            clampedParticleStartTime;

        if (waitBeforeParticle > 0f)
        {
            yield return new WaitForSeconds(
                waitBeforeParticle
            );
        }

        StartParticleEffect();

        if (clampedParticleStartTime > 0f)
        {
            yield return new WaitForSeconds(
                clampedParticleStartTime
            );
        }

        DetachParticleEffect();

        Destroy(gameObject);

        isRunning = false;
        destroyCoroutine = null;
    }


    private void StartParticleEffect()
    {
        PlayParticleEffect();
        PlayParticleSound();
    }


    private void PlayParticleEffect()
    {
        if (particleEffect == null)
        {
            return;
        }

        particleEffect.gameObject.SetActive(true);

        particleEffect.Clear();

        particleEffect.Play();
    }


    private void PlayParticleSound()
    {
        if (particleSound == null)
        {
            return;
        }

        GameObject soundObject =
            new GameObject("ParticleSound");

        soundObject.transform.position =
            transform.position;

        AudioSource temporaryAudioSource =
            soundObject.AddComponent<AudioSource>();

        temporaryAudioSource.clip =
            particleSound;

        temporaryAudioSource.volume =
            particleSoundVolume;

        temporaryAudioSource.spatialBlend = 1f;

        if (audioSource != null)
        {
            temporaryAudioSource.outputAudioMixerGroup =
                audioSource.outputAudioMixerGroup;

            temporaryAudioSource.pitch =
                audioSource.pitch;

            temporaryAudioSource.minDistance =
                audioSource.minDistance;

            temporaryAudioSource.maxDistance =
                audioSource.maxDistance;

            temporaryAudioSource.rolloffMode =
                audioSource.rolloffMode;
        }

        temporaryAudioSource.Play();

        Destroy(
            soundObject,
            particleSound.length
        );
    }


    private void DetachParticleEffect()
    {
        if (particleEffect == null)
        {
            return;
        }

        particleEffect.transform.SetParent(
            null,
            true
        );

        particleEffect.gameObject.SetActive(true);

        particleEffect.Play();

        StartCoroutine(
            StopParticleEffect()
        );
    }


    private IEnumerator StopParticleEffect()
    {
        if (particleEffect == null)
        {
            yield break;
        }

        float fadeDuration =
            Mathf.Max(
                0f,
                particleFadeDuration
            );

        if (fadeDuration > 0f)
        {
            yield return new WaitForSeconds(
                fadeDuration
            );
        }

        if (particleEffect == null)
        {
            yield break;
        }

        particleEffect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmitting
        );

        yield return new WaitUntil(() =>
            particleEffect == null ||
            !particleEffect.IsAlive(true)
        );

        if (particleEffect != null)
        {
            Destroy(
                particleEffect.gameObject
            );
        }
    }


    private void OnDisable()
    {
        if (destroyCoroutine != null)
        {
            StopCoroutine(
                destroyCoroutine
            );
        }

        destroyCoroutine = null;
        isRunning = false;
    }
}