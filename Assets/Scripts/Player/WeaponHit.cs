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
        SurfaceType defaultSurface,
        float knockbackDistance = 0f,
        bool isProjectile = false)
    {
        Health target =
            hit.collider.GetComponentInParent<Health>();

        if (target == null)
        {
            Surface.PlayImpact(hit, defaultSurface);
            return;
        }

        // Boss-Erfolg "nur mit der Armbrust besiegt".
        if (target.TryGetComponent(out MageCombatBehaviour mage))
        {
            mage.NotifyPlayerHit(isProjectile);
        }

        target.TakeDamage(DifficultySettings.DamageByPlayer(damage), hit.point, attacker);

        if (knockbackDistance > 0f && !target.IsDead &&
            target.TryGetComponent(out EnemyMovement movement))
        {
            Vector3 origin = attacker != null ? attacker.position : hit.point;
            movement.Knockback(target.transform.position - origin, knockbackDistance);
        }
    }
}
