using UnityEngine;

public enum WeaponAttackType
{
    Melee,
    Projectile
}

[CreateAssetMenu(
    fileName = "New Weapon",
    menuName = "New Item/Weapon"
)]
public class WeaponSO : ItemSO
{
    [Header("Combat")]
    [SerializeField]
    private WeaponAttackType attackType =
        WeaponAttackType.Melee;

    [Min(0)]
    public int WeaponDamage;

    [Min(0f)]
    public float WeaponRange;

    [Min(0.01f)]
    public float AttackDuration = 0.5f;

    [Header("Projectile")]
    [SerializeField]
    private PlayerProjectile projectilePrefab;

    [Min(0.1f)]
    [SerializeField]
    private float projectileSpeed = 20f;

    [Header("Ammunition")]
    [SerializeField]
    private bool usesAmmo;

    [Tooltip("Mögliche Munitionssorten – die erste ist die Standardmunition.")]
    [SerializeField]
    private AmmoSO[] ammoTypes;

    [Min(1)]
    [SerializeField]
    private int ammoPerShot = 1;

    [Header("Visuals")]
    [Tooltip("Wird während des Angriffs gezeigt, danach wieder das Held Sprite.")]
    public Sprite AttackSprite;

    [Tooltip("Optional (Munitionswaffen): Bild in der Hand ohne Munition. Leer = Held Sprite.")]
    public Sprite EmptySprite;

    [Header("Audio")]
    public AudioClip[] AttackSounds;

    public WeaponAttackType AttackType =>
        attackType;

    public PlayerProjectile ProjectilePrefab =>
        projectilePrefab;

    public float ProjectileSpeed =>
        projectileSpeed;

    public bool UsesProjectile =>
        attackType ==
        WeaponAttackType.Projectile;

    public bool UsesAmmo =>
        usesAmmo;

    public AmmoSO[] AmmoTypes =>
        ammoTypes ?? System.Array.Empty<AmmoSO>();

    public int AmmoPerShot =>
        Mathf.Max(1, ammoPerShot);

    /// <summary>Bild in der Hand – bei Munitionswaffen passend zur geladenen Munition (null = leer).</summary>
    public Sprite GetHeldSprite(AmmoSO loadedAmmo)
    {
        if (!usesAmmo)
        {
            return HeldSprite;
        }

        if (loadedAmmo == null)
        {
            return EmptySprite != null ? EmptySprite : HeldSprite;
        }

        return loadedAmmo.LoadedWeaponSprite != null ? loadedAmmo.LoadedWeaponSprite : HeldSprite;
    }
}