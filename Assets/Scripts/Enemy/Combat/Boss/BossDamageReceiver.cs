using UnityEngine;

public class BossDamageReceiver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;

    [SerializeField]
    private SpearBossCombatBehaviour combatBehaviour;

    private void Awake()
    {
        FindMissingReferences();
    }

    public void ReceiveDamage(
        int damage,
        Transform attacker)
    {
        if (damage <= 0 ||
            health == null ||
            health.IsDead)
        {
            return;
        }

        if (combatBehaviour != null &&
            combatBehaviour.TryBlockAttack(attacker))
        {
            return;
        }

        health.TakeDamage(damage);
    }

    private void FindMissingReferences()
    {
        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (combatBehaviour == null)
        {
            combatBehaviour =
                GetComponent<
                    SpearBossCombatBehaviour
                >();
        }
    }
}