using UnityEngine;

/// <summary>
/// Markiert ein Objekt der Spielwelt mit seiner Oberflächenart (Stein, Holz …).
/// Gilt für alle Collider dieses Objekts und seiner Kinder – am besten direkt
/// auf die modularen Level-Prefabs legen. Ein Kind mit eigener Surface
/// (z.B. Holzbalken in einer Steinwand) überschreibt die des Elternobjekts.
/// </summary>
[DisallowMultipleComponent]
public class Surface : MonoBehaviour
{
    [SerializeField] private SurfaceType surfaceType;

    public SurfaceType SurfaceType => surfaceType;

    /// <summary>
    /// Spielt den Treffer-Effekt der getroffenen Oberfläche.
    /// Ohne Surface (oder ohne zugewiesene Art) wird <paramref name="fallback"/> genutzt.
    /// </summary>
    public static void PlayImpact(RaycastHit hit, SurfaceType fallback)
    {
        SurfaceType type = FindSurfaceType(hit.collider);

        if (type == null)
        {
            type = fallback;
        }

        if (type != null)
        {
            type.ImpactEffect.Play(hit.point, hit.normal);
        }
    }

    private static SurfaceType FindSurfaceType(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        Surface surface = hitCollider.GetComponentInParent<Surface>();

        return surface != null ? surface.surfaceType : null;
    }
}
