using UnityEngine;

public enum FootstepSoundType
{
    Walk,
    Stop,
    Jump,
    Land
}

/// <summary>
/// Schrittgeräusche für einen Untergrund (z.B. Cobblestone, Dirt, Mud).
/// Pro Ereignis gibt es mehrere Varianten, von denen zufällig eine abgespielt wird.
/// </summary>
[CreateAssetMenu(fileName = "New Footstep Sounds", menuName = "Audio/Footstep Sound Set")]
public class FootstepSoundSet : ScriptableObject
{
    [SerializeField] private AudioClip[] walkClips;
    [SerializeField] private AudioClip[] stopClips;
    [SerializeField] private AudioClip[] jumpClips;
    [SerializeField] private AudioClip[] landClips;

    [Header("Variation")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.6f;

    [Tooltip("Zufällige Tonhöhen-Abweichung, damit Schritte nicht identisch klingen.")]
    [Range(0f, 0.3f)]
    [SerializeField] private float pitchVariation = 0.08f;

    public float Volume => volume;

    public float PitchVariation => pitchVariation;

    public AudioClip[] GetClips(FootstepSoundType type)
    {
        return type switch
        {
            FootstepSoundType.Walk => walkClips,
            FootstepSoundType.Stop => stopClips,
            FootstepSoundType.Jump => jumpClips,
            FootstepSoundType.Land => landClips,
            _ => null
        };
    }
}
