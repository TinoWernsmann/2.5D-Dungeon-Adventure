using UnityEngine;

/// <summary>
/// Spielt VFX + Sound, wenn das Objekt Schaden nimmt bzw. zerstört wird.
/// Einfach neben <see cref="Health"/> legen (Gegner, Nerven, Kisten …).
/// Reagiert auf jeden Schaden – Nahkampf, Armbrust und Projektile.
/// </summary>
[RequireComponent(typeof(Health))]
[DisallowMultipleComponent]
public class HitFeedback : MonoBehaviour
{
    [Tooltip("Bei jedem Treffer an der Trefferstelle.")]
    [SerializeField] private ImpactEffect hitEffect = new();

    [Tooltip("Einmalig, wenn das Objekt zerstört wird / stirbt.")]
    [SerializeField] private ImpactEffect deathEffect = new();

    [Tooltip("Optionaler Punkt für den Zerstörungs-Effekt (z.B. Mitte der Kiste).")]
    [SerializeField] private Transform deathEffectPoint;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.DamagedAtPosition += HandleDamaged;
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        health.DamagedAtPosition -= HandleDamaged;
        health.Died -= HandleDied;
    }

    private void HandleDamaged(int damage, Vector3 hitPoint)
    {
        // Der Health-Event liefert keine Oberflächennormale: vom Objekt weg nach außen.
        hitEffect.Play(hitPoint, hitPoint - transform.position);
    }

    private void HandleDied()
    {
        Transform point = deathEffectPoint != null ? deathEffectPoint : transform;

        deathEffect.Play(point.position, Vector3.up);
    }
}
