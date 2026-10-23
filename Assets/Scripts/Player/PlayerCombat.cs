using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private ItemManager itemManager;

    [Tooltip("Leer lassen = wird am selben Objekt gesucht.")]
    [SerializeField]
    private PlayerEquipment equipment;

    [FormerlySerializedAs("weaponUI")]
    [SerializeField]
    private HeldItemUI heldItemUI;

    [SerializeField]
    private WeaponSoundManager
        weaponSoundManager;

    [Header("Projectile")]
    [SerializeField]
    private Transform projectileSpawnPoint;

    [Header("Impact")]
    [Tooltip("Oberfläche für Objekte ohne Surface-Komponente – " +
             "gilt für Nahkampf und Projektile.")]
    [SerializeField]
    private SurfaceType defaultSurface;

    private Health playerHealth;
    private float nextAttackTime;
    private AmmoSO selectedAmmo;

    /// <summary>Munitionssorte, mit der als Nächstes geschossen wird (für die Anzeige).</summary>
    public AmmoSO SelectedAmmo => GetAmmoToUse(equipment != null ? equipment.CurrentWeapon : null);

    /// <summary>
    /// Ausgerüstete Munition (im Inventar hervorgehoben): zuletzt gewählte Sorte, solange vorhanden –
    /// sonst die erste Munition im Inventar.
    /// </summary>
    public AmmoSO EquippedAmmo
    {
        get
        {
            if (selectedAmmo != null && itemManager.HasItem(selectedAmmo))
            {
                return selectedAmmo;
            }

            for (int slot = 0; slot < itemManager.ItemCount; slot++)
            {
                if (itemManager.GetItemAt(slot) is AmmoSO ammo)
                {
                    return ammo;
                }
            }

            return null;
        }
    }

    public event Action SelectedAmmoChanged;
    private const string DEATH_SCENE = "DeathScene";
    private const int HEALING_AMOUNT = 15;

    private void Awake()
    {
        playerHealth =
            GetComponent<Health>();

        if (equipment == null)
        {
            equipment = GetComponent<PlayerEquipment>();
        }
    }

    private void OnEnable()
    {
        playerHealth.Died += HandlePlayerDeath;
        playerHealth.Damaged += HandleDamaged;
        itemManager.OnHealingUsed += HandleHealing;
    }

    private void OnDisable()
    {
        playerHealth.Died -= HandlePlayerDeath;
        playerHealth.Damaged -= HandleDamaged;
        itemManager.OnHealingUsed -= HandleHealing;
    }

    private void HandleHealing()
    {
        playerHealth.Heal(HEALING_AMOUNT);
        LevelStats.Current?.RecordHealing();
    }

    // Für Erfolge wie "Level ohne Schaden abschließen".
    private void HandleDamaged(int damage)
    {
        LevelStats.Current?.RecordDamage(damage);
    }

    private void HandlePlayerDeath()
    {
        LevelStats.Current?.RecordDeath();
        SceneManager.LoadSceneAsync(DEATH_SCENE);
    }

    public void TryAttack()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        WeaponSO weapon =
            equipment != null ? equipment.CurrentWeapon : null;

        if (!CanAttack(weapon))
        {
            return;
        }

        if (weapon.UsesProjectile)
        {
            TryProjectileAttack(weapon);
            return;
        }

        PerformMeleeAttack(weapon);
    }

    private bool CanAttack(
        WeaponSO weapon)
    {
        return weapon != null &&
               playerCamera != null &&
               Time.time >=
               nextAttackTime;
    }

    private void PerformMeleeAttack(
        WeaponSO weapon)
    {
        StartAttackCooldown(weapon);

        PlayAttackFeedback(weapon);

        TryMeleeDamage(weapon);
    }

    private void TryProjectileAttack(
        WeaponSO weapon)
    {
        if (!CanShootProjectile(
                weapon))
        {
            return;
        }

        if (!TryConsumeAmmo(
                weapon,
                out AmmoSO usedAmmo))
        {
            Debug.Log(
                $"[PLAYER] No ammo for " +
                $"{weapon.ItemName}."
            );

            return;
        }

        StartAttackCooldown(weapon);

        PlayAttackFeedback(weapon);

        ShootProjectile(weapon, usedAmmo);
        SelectedAmmoChanged?.Invoke();
    }

    private bool CanShootProjectile(
        WeaponSO weapon)
    {
        if (weapon.ProjectilePrefab ==
            null)
        {
            Debug.LogWarning(
                $"{weapon.name}: " +
                "No Projectile Prefab " +
                "assigned.",
                weapon
            );

            return false;
        }

        if (projectileSpawnPoint ==
            null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: " +
                "No Projectile Spawn Point " +
                "assigned.",
                gameObject
            );

            return false;
        }

        return true;
    }

    private bool TryConsumeAmmo(
        WeaponSO weapon,
        out AmmoSO usedAmmo)
    {
        usedAmmo = null;

        if (!weapon.UsesAmmo)
        {
            return true;
        }

        if (weapon.AmmoTypes.Length == 0)
        {
            Debug.LogWarning(
                $"{weapon.name}: Uses Ammo is " +
                "enabled but no Ammo Types " +
                "are assigned.",
                weapon
            );

            return false;
        }

        usedAmmo = GetAmmoToUse(weapon);

        return usedAmmo != null &&
               itemManager.TryConsumeItem(
                   usedAmmo,
                   weapon.AmmoPerShot
               );
    }

    /// <summary>Munition im Slot auswählen + Linksklick: diese Sorte wird ab jetzt verschossen.</summary>
    public void EquipAmmo(AmmoSO ammo)
    {
        if (ammo == null || ammo == EquippedAmmo)
        {
            return;
        }

        selectedAmmo = ammo;
        SelectedAmmoChanged?.Invoke();
    }

    /// <summary>R: wechselt zur nächsten Munitionssorte, die der Spieler besitzt.</summary>
    public void SwitchAmmo()
    {
        WeaponSO weapon = equipment != null ? equipment.CurrentWeapon : null;

        if (weapon == null || !weapon.UsesAmmo)
        {
            return;
        }

        AmmoSO[] available = GetAvailableAmmo(weapon);

        if (available.Length < 2)
        {
            return;
        }

        int current = Array.IndexOf(available, GetAmmoToUse(weapon));
        EquipAmmo(available[(current + 1) % available.Length]);
    }

    /// <summary>Ausgerüstete Sorte, falls sie zur Waffe passt – sonst die erste passende, die der Spieler hat.</summary>
    private AmmoSO GetAmmoToUse(WeaponSO weapon)
    {
        if (weapon == null || !weapon.UsesAmmo)
        {
            return null;
        }

        AmmoSO[] available = GetAvailableAmmo(weapon);
        AmmoSO equipped = EquippedAmmo;

        return available.Contains(equipped)
            ? equipped
            : available.FirstOrDefault();
    }

    private AmmoSO[] GetAvailableAmmo(WeaponSO weapon)
    {
        return weapon.AmmoTypes
            .Where(ammo => ammo != null && itemManager.HasItem(ammo, weapon.AmmoPerShot))
            .ToArray();
    }

    private void StartAttackCooldown(
        WeaponSO weapon)
    {
        nextAttackTime =
            Time.time +
            weapon.AttackDuration;
    }

    private void PlayAttackFeedback(
        WeaponSO weapon)
    {
        // Für den Erfolg "Faustkämpfer": jeder echte Angriff mit einer Waffe zählt.
        if (equipment != null && !equipment.IsFists(weapon))
        {
            LevelStats.Current?.RecordWeaponUsed();
        }

        heldItemUI?.PlayAttackAnimation(
            weapon.AttackSprite,
            weapon.HeldSprite,
            weapon.AttackDuration
        );

        weaponSoundManager
            ?.PlayRandomAttackSound();
    }

    private void ShootProjectile(
        WeaponSO weapon,
        AmmoSO ammo)
    {
        Vector3 direction =
            GetProjectileDirection(
                weapon
            );

        // Munition mit eigenem Projektil (z.B. Push-Bolzen) sieht auch anders aus.
        PlayerProjectile prefab =
            ammo != null && ammo.ProjectilePrefab != null
                ? ammo.ProjectilePrefab
                : weapon.ProjectilePrefab;

        PlayerProjectile projectile =
            Instantiate(
                prefab,
                projectileSpawnPoint.position,
                Quaternion.LookRotation(
                    direction
                )
            );

        projectile.Initialize(
            direction,
            weapon.ProjectileSpeed,
            weapon.WeaponDamage,
            gameObject,
            defaultSurface,
            ammo != null ? ammo.KnockbackDistance : 0f
        );
    }

    private Vector3
        GetProjectileDirection(
            WeaponSO weapon)
    {
        Ray aimRay =
            new Ray(
                playerCamera
                    .transform.position,
                playerCamera
                    .transform.forward
            );

        Vector3 targetPoint =
            aimRay.origin +
            aimRay.direction *
            weapon.WeaponRange;

        if (Physics.Raycast(
                aimRay,
                out RaycastHit hit,
                weapon.WeaponRange))
        {
            targetPoint =
                hit.point;
        }

        Vector3 direction =
            targetPoint -
            projectileSpawnPoint.position;

        if (direction.sqrMagnitude <=
            0.001f)
        {
            return playerCamera
                .transform.forward;
        }

        return direction.normalized;
    }

    private void TryMeleeDamage(
        WeaponSO weapon)
    {
        Ray ray =
            new Ray(
                playerCamera
                    .transform.position,
                playerCamera
                    .transform.forward
            );

        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                weapon.WeaponRange,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );

        System.Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(
                    b.distance
                )
        );

        foreach (RaycastHit hit in hits)
        {
            if (IsOwnCollider(hit.collider))
            {
                continue;
            }

            // Der erste fremde Collider zählt: Wände blocken den Schlag.
            WeaponHit.Resolve(
                hit,
                weapon.WeaponDamage,
                transform,
                defaultSurface
            );

            return;
        }
    }

    private bool IsOwnCollider(
        Collider hitCollider)
    {
        return hitCollider.transform
            .IsChildOf(transform);
    }
}