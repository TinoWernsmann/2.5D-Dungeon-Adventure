using System.Collections;
using UnityEngine;

/// <summary>
/// Kamerawackeln (z.B. Einsturz, Erdbeben). Liegt auf der Kamera.
/// Der Versatz wird in LateUpdate aufgelegt und zu Beginn des nächsten Frames wieder
/// entfernt (läuft vor allen anderen Skripten) – so driftet die Kamera nicht und die
/// normale Blicksteuerung arbeitet ungestört weiter.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class CameraShake : MonoBehaviour
{
    [Tooltip("Maximaler Versatz in Metern bei Stärke 1.")]
    [Min(0f)]
    [SerializeField] private float positionAmplitude = 0.12f;

    [Tooltip("Maximale Neigung in Grad bei Stärke 1.")]
    [Min(0f)]
    [SerializeField] private float rollAmplitude = 3f;

    [Tooltip("Wie schnell gewackelt wird.")]
    [Min(0.1f)]
    [SerializeField] private float frequency = 22f;

    private float shakeDuration;
    private float shakeStrength;
    private float remainingTime;

    private Vector3 appliedPosition;
    private Quaternion appliedRotation = Quaternion.identity;

    public bool IsShaking => remainingTime > 0f;

    /// <summary>Wackelt für <paramref name="duration"/> Sekunden, wird zum Ende hin schwächer.</summary>
    public IEnumerator Shake(float duration, float strength)
    {
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeStrength = strength;
        remainingTime = shakeDuration;

        yield return new WaitWhile(() => IsShaking);
    }

    private void Update()
    {
        RemoveOffset();
    }

    private void LateUpdate()
    {
        if (!IsShaking)
        {
            return;
        }

        remainingTime -= Time.deltaTime;

        float intensity = shakeStrength * Mathf.Clamp01(remainingTime / shakeDuration);
        float time = Time.time * frequency;

        appliedPosition = new Vector3(
            Noise(time, 0f),
            Noise(0f, time),
            0f) * (positionAmplitude * intensity);

        appliedRotation = Quaternion.Euler(0f, 0f, Noise(time, time) * rollAmplitude * intensity);

        transform.localPosition += appliedPosition;
        transform.localRotation *= appliedRotation;
    }

    private void OnDisable()
    {
        RemoveOffset();
        remainingTime = 0f;
    }

    private void RemoveOffset()
    {
        transform.localPosition -= appliedPosition;
        transform.localRotation *= Quaternion.Inverse(appliedRotation);

        appliedPosition = Vector3.zero;
        appliedRotation = Quaternion.identity;
    }

    /// <summary>Perlin-Noise im Bereich -1 bis 1.</summary>
    private static float Noise(float x, float y)
    {
        return Mathf.PerlinNoise(x, y) * 2f - 1f;
    }
}
