using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private ItemManager itemManager;
    [SerializeField] private WeaponUI weaponUI;
    [SerializeField] private WeaponSoundManager weaponSoundManager;

    [Header("Projectile")]
    [SerializeField] private Transform projectileSpawnPoint;

    private Health playerHealth;
    private float nextAttackTime;

    private void Awake()
    {
        playerHealth =
            GetComponent<Health>();
    }

    public void TryAttack()
    {
        WeaponSO weapon =
            itemManager?.SelectedWeapon;

        if (!CanAttack(weapon))
        {
            return;
        }

        Attack(weapon);
    }

    private bool CanAttack(
        WeaponSO weapon)
    {
        return weapon != null &&
               playerCamera != null &&
               Time.time >= nextAttackTime;
    }

    private void Attack(
        WeaponSO weapon)
    {
        nextAttackTime =
            Time.time +
            weapon.AttackDuration;

        PlayAttackFeedback(weapon);

        if (weapon.UsesProjectile)
        {
            ShootProjectile(weapon);
            return;
        }

        TryMeleeDamage(weapon);
    }

    private void PlayAttackFeedback(
        WeaponSO weapon)
    {
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
        if (weapon.ProjectilePrefab == null)
        {
            Debug.LogWarning(
                $"{weapon.name}: " +
                "No Projectile Prefab assigned.",
                weapon
            );

            return;
        }

        if (projectileSpawnPoint == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: " +
                "No Projectile Spawn Point assigned.",
                gameObject
            );

            return;
        }

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

    private Vector3 GetProjectileDirection(
        WeaponSO weapon)
    {
        Ray aimRay =
            new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
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
        Ray ray = new(
            playerCamera.transform.position,
            playerCamera.transform.forward
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

            if (!CanDamage(targetHealth))
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
            Debug.Log(
                "[PLAYER ATTACK] " +
                "BossDamageReceiver found on " +
                $"{bossReceiver.gameObject.name}."
            );

            bossReceiver.ReceiveDamage(
                damage,
                transform
            );

            return;
        }

        Debug.Log(
            "[PLAYER ATTACK] " +
            "Normal target hit: " +
            $"{targetHealth.gameObject.name}."
        );

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

        if (targetHealth != null)
        {
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

            receiver =
                targetHealth.GetComponentInParent<
                    BossDamageReceiver>();
        }

        return receiver;
    }

    private bool CanDamage(
        Health target)
    {
        return target != null &&
               target != playerHealth &&
               !target.IsDead;
    }
}