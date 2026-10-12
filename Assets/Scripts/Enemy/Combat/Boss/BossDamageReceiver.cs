using UnityEngine;

/// <summary>
/// Gibt dem Boss die Chance, eingehende Treffer zu blocken.
/// <see cref="Health"/> fragt diese Komponente automatisch, bevor Schaden abgezogen wird.
/// </summary>
public class BossDamageReceiver : MonoBehaviour, IDamageInterceptor
{
    [Header("References")]
    [SerializeField] private SpearBossCombatBehaviour combatBehaviour;

    private void Awake()
    {
        if (combatBehaviour == null)
        {
            combatBehaviour = GetComponent<SpearBossCombatBehaviour>();
        }
    }

    public bool TryInterceptDamage(int damage, Transform attacker)
    {
        return combatBehaviour != null &&
               combatBehaviour.TryBlockAttack(attacker);
    }
}
