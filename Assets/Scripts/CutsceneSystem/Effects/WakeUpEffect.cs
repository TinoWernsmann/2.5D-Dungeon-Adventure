using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Langsames Aufwachen: Die Augen öffnen sich blinzelnd (Schwarzblende nach Kurve),
/// das Bild ist zuerst verschwommen und wird dann klar (URP Depth of Field + Vignette).
/// Das Post-Processing-Volume wird zur Laufzeit erzeugt und danach wieder entfernt.
/// </summary>
[RequireComponent(typeof(ScreenFader))]
public class WakeUpEffect : MonoBehaviour
{
    [Min(0.1f)]
    [SerializeField] private float duration = 6f;

    [Tooltip("Deckkraft des Schwarz über die Zeit (0–1). 1 = Augen zu, 0 = offen. Mehrere Täler = Blinzeln.")]
    [SerializeField] private AnimationCurve eyelidCurve = new(
        new Keyframe(0f, 1f),
        new Keyframe(0.15f, 0.45f),
        new Keyframe(0.25f, 0.95f),
        new Keyframe(0.45f, 0.25f),
        new Keyframe(0.55f, 0.7f),
        new Keyframe(0.75f, 0.1f),
        new Keyframe(1f, 0f));

    [Tooltip("Stärke der Unschärfe über die Zeit (0–1).")]
    [SerializeField] private AnimationCurve blurCurve = new(
        new Keyframe(0f, 1f),
        new Keyframe(0.6f, 0.7f),
        new Keyframe(1f, 0f));

    [Range(0.5f, 1.5f)]
    [SerializeField] private float maxBlurRadius = 1.5f;

    [Range(0f, 1f)]
    [Tooltip("Dunkler Rand wie halb geschlossene Lider – nimmt mit der Unschärfe ab.")]
    [SerializeField] private float vignetteIntensity = 0.55f;

    private ScreenFader fader;

    private void Awake()
    {
        fader = GetComponent<ScreenFader>();
    }

    public IEnumerator Play()
    {
        VolumeProfile profile = CreateProfile();
        Volume volume = CreateVolume(profile);

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = elapsed / duration;

            fader.SetAlpha(eyelidCurve.Evaluate(progress));
            volume.weight = Mathf.Clamp01(blurCurve.Evaluate(progress));

            yield return null;
        }

        fader.SetAlpha(0f);

        Destroy(volume.gameObject);
        Destroy(profile);
    }

    private VolumeProfile CreateProfile()
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

        // Gaussian: alles ab 0 m ist maximal unscharf.
        DepthOfField depthOfField = profile.Add<DepthOfField>(true);
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(0f);
        depthOfField.gaussianEnd.Override(0.5f);
        depthOfField.gaussianMaxRadius.Override(maxBlurRadius);
        depthOfField.highQualitySampling.Override(true);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(1f);

        return profile;
    }

    private Volume CreateVolume(VolumeProfile profile)
    {
        GameObject volumeObject = new("WakeUpVolume");
        volumeObject.transform.SetParent(transform, false);

        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.sharedProfile = profile;
        volume.weight = 1f;

        return volume;
    }
}
