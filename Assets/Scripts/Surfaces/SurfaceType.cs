using UnityEngine;

/// <summary>
/// Eine Oberflächenart (z.B. Stein, Holz, Metall) mit ihrem Treffer-Effekt.
/// Wird über <see cref="Surface"/> an Objekte der Spielwelt gehängt.
/// Neue Oberflächen = neues Asset, kein Code und kein Layer nötig.
/// </summary>
[CreateAssetMenu(fileName = "New Surface", menuName = "Audio/Surface Type")]
public class SurfaceType : ScriptableObject
{
    [Tooltip("VFX + Sound, wenn eine Waffe oder ein Pfeil diese Oberfläche trifft.")]
    [SerializeField] private ImpactEffect impactEffect = new();

    public ImpactEffect ImpactEffect => impactEffect;
}
