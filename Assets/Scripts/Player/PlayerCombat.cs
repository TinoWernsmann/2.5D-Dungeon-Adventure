using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private ItemManager itemManager;

    [SerializeField]
    private WeaponUI weaponUI;

    [SerializeField]
    private WeaponSoundManager
        weaponSoundManager;

    [Header("Projectile")]
    [SerializeField]
    private Transform projectileSpawnPoint;

    private Health playerHealth;
    private float nextAttackTime;
    private const string DEATH_SCENE = "DeathScene";
    private const int HEALING_AMOUNT = 20;

    private void Awake()
    {
        playerHealth =
            GetComponent<Health>();
    }

    private void OnEnable()
    {
        playerHealth.Died += HandlePlayerDeath;
        itemManager.OnHealingUsed += HandleHealing;
    }



    private void OnDisable()
    {
        playerHealth.Died -= HandlePlayerDeath;
        itemManager.OnHealingUsed -= HandleHealing;
    }

    private void HandleHealing()
    {
        playerHealth.Heal(HEALING_AMOUNT);
    }

    private void HandlePlayerDeath()
    {
        SceneManager.LoadSceneAsync(DEATH_SCENE);
    }

    public void TryAttack()
    {
        WeaponSO weapon =
            itemManager?.SelectedWeapon;

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
                weapon))
        {
            Debug.Log(
                $"[PLAYER] No ammo for " +
                $"{weapon.ItemName}."
            );

            return;
        }

        StartAttackCooldown(weapon);

        PlayAttackFeedback(weapon);

        ShootProjectile(weapon);
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
        WeaponSO weapon)
    {
        if (!weapon.UsesAmmo)
        {
            return true;
        }

        if (weapon.AmmoItem == null)
        {
            Debug.LogWarning(
                $"{weapon.name}: Uses Ammo is " +
                "enabled but no Ammo Item " +
                "is assigned.",
                weapon
            );

            return false;
        }

        return itemManager.TryConsumeItem(
            weapon.AmmoItem,
            weapon.AmmoPerShot
        );
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
        if (weapon.ItemName == "Key") return;

        weaponUI?.PlayAttackAnimation(
            weapon.AttackSprite,
            weapon.IdleSprite,
            weapon.AttackDuration
        );

        weaponSoundManager
            ?.PlayRandomAttackSound();
    }

    private void ShootProjectile(
        WeaponSO weapon)
    {
        Vector3 direction =
            GetProjectileDirection(
                weapon
            );

        PlayerProjectile projectile =
            Instantiate(
                weapon.ProjectilePrefab,
                projectileSpawnPoint.position,
                Quaternion.LookRotation(
                    direction
                )
            );

        projectile.Initialize(
            direction,
            weapon.ProjectileSpeed,
            weapon.WeaponDamage,
            gameObject
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
                weapon.WeaponRange
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
            Health targetHealth =
                hit.collider
                    .GetComponentInParent<
                        Health>();

            if (!CanDamage(
                    targetHealth))
            {
                continue;
            }

            ApplyMeleeDamage(
                hit.collider,
                targetHealth,
                weapon.WeaponDamage
            );

            return;
        }
    }

    private void ApplyMeleeDamage(
        Collider hitCollider,
        Health targetHealth,
        int damage)
    {
        BossDamageReceiver bossReceiver =
            FindBossDamageReceiver(
                hitCollider,
                targetHealth
            );

        if (bossReceiver != null)
        {
            bossReceiver.ReceiveDamage(
                damage,
                transform
            );

            return;
        }

        targetHealth.TakeDamage(
            damage
        );
    }

    private BossDamageReceiver
        FindBossDamageReceiver(
            Collider hitCollider,
            Health targetHealth)
    {
        BossDamageReceiver receiver =
            hitCollider.GetComponent<
                BossDamageReceiver>();

        if (receiver != null)
        {
            return receiver;
        }

        receiver =
            hitCollider.GetComponentInParent<
                BossDamageReceiver>();

        if (receiver != null)
        {
            return receiver;
        }

        if (targetHealth == null)
        {
            return null;
        }

        receiver =
            targetHealth.GetComponent<
                BossDamageReceiver>();

        if (receiver != null)
        {
            return receiver;
        }

        receiver =
            targetHealth.GetComponentInChildren<
                BossDamageReceiver>();

        if (receiver != null)
        {
            return receiver;
        }

        return targetHealth
            .GetComponentInParent<
                BossDamageReceiver>();
    }

    private bool CanDamage(
        Health target)
    {
        return target != null &&
               target != playerHealth &&
               !target.IsDead;
    }
}