using UnityEngine;

/// <summary>
/// Munition für Fernkampfwaffen (z.B. Armbrustbolzen). Mehrere Sorten pro Waffe möglich –
/// der Spieler wechselt mit R zwischen den Sorten, die er gerade besitzt.
/// </summary>
[CreateAssetMenu(
    fileName = "New Ammo",
    menuName = "New Item/Ammo"
)]
public class AmmoSO : ItemSO
{
    [Header("Ammo")]
    [Min(0f)]
    [Tooltip("So weit wird ein getroffener Gegner zurückgestoßen (0 = kein Rückstoß). " +
             "Steht er an einer Kante zur Säure, fällt er hinein.")]
    [SerializeField] private float knockbackDistance;

    [Tooltip("Optional: eigenes Projektil (Aussehen). Leer = Projektil der Waffe.")]
    [SerializeField] private PlayerProjectile projectilePrefab;

    [Tooltip("Optional: Bild der Waffe in der Hand, wenn diese Munition geladen ist. Leer = Held Sprite der Waffe.")]
    [SerializeField] private Sprite loadedWeaponSprite;

    public float KnockbackDistance => knockbackDistance;

    public Sprite LoadedWeaponSprite => loadedWeaponSprite;

    public PlayerProjectile ProjectilePrefab => projectilePrefab;
}
