using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private ItemManager itemManager;
    [SerializeField] private WeaponUI weaponUI;
    [SerializeField] private WeaponSoundManager weaponSoundManager;

    private Health playerHealth;
    private float nextAttackTime;
    private const string DEATH_SCENE = "DeathScene";
    private const int HEALING_AMOUNT = 20;

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
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

        Attack(weapon);
    }

    private bool CanAttack(WeaponSO weapon)
    {
        return weapon != null &&
               playerCamera != null &&
               Time.time >= nextAttackTime;
    }

    private void Attack(WeaponSO weapon)
    {
        nextAttackTime =
            Time.time + weapon.AttackDuration;

        PlayAttackFeedback(weapon);
        TryDamageTarget(weapon);
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

    private void TryDamageTarget(
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
                    .GetComponentInParent<Health>();

            if (!CanDamage(targetHealth))
            {
                continue;
            }

            ApplyDamage(
                hit.collider,
                targetHealth,
                weapon.WeaponDamage
            );

            return;
        }
    }

    private void ApplyDamage(
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
                $"[PLAYER ATTACK] BossDamageReceiver found on " +
                $"{bossReceiver.gameObject.name}."
            );

            bossReceiver.ReceiveDamage(
                damage,
                transform
            );

            return;
        }

        Debug.Log(
            $"[PLAYER ATTACK] Normal target hit: " +
            $"{targetHealth.gameObject.name}."
        );

        targetHealth.TakeDamage(damage);
    }

    private BossDamageReceiver FindBossDamageReceiver(
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

    private bool CanDamage(Health target)
    {
        return target != null &&
               target != playerHealth &&
               !target.IsDead;
    }
}