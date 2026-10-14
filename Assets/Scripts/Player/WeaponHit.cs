using UnityEngine;

/// <summary>
/// Gemeinsame Treffer-Auswertung für alle Spielerwaffen (Nahkampf und Projektile):
/// Objekte mit <see cref="Health"/> bekommen Schaden (ihr Feedback liefert
/// <see cref="HitFeedback"/>), alles andere spielt den Effekt seiner <see cref="Surface"/>.
/// </summary>
public static class WeaponHit
{
    public static void Resolve(
        RaycastHit hit,
        int damage,
        Transform attacker,
        SurfaceType defaultSurface)
    {
        Health target =
            hit.collider.GetComponentInParent<Health>();

        if (target == null)
        {
            Surface.PlayImpact(hit, defaultSurface);
            return;
        }

        target.TakeDamage(damage, hit.point, attacker);
    }
}
