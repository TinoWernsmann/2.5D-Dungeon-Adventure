using UnityEngine;

public abstract class EnemyCombatBehaviour : MonoBehaviour
{
    public abstract bool IsPerformingAttack { get; }

    public abstract float AttackProgress { get; }

    public abstract bool IsInCombatRange(Transform target);

    public abstract void EnterCombat(
        Transform target,
        Health targetHealth
    );

    public abstract void UpdateCombat(
        Transform target,
        Health targetHealth
    );

    public abstract void ExitCombat();
}