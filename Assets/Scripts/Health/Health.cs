using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [Min(1)]
    [SerializeField] private int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int, int> HealthChanged;

    public event Action<int> Damaged;

    public event Action<int, Vector3> DamagedAtPosition;

    public event Action Died;

    /// <summary>Wird ausgelöst, wenn ein bekannter Angreifer Schaden verursacht hat.</summary>
    public event Action<Transform> DamagedBy;

    private IDamageInterceptor[] damageInterceptors;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        damageInterceptors = GetComponents<IDamageInterceptor>();
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(
            damage,
            transform.position
        );
    }

    public void TakeDamage(
        int damage,
        Vector3 hitPoint)
    {
        TakeDamage(
            damage,
            hitPoint,
            null
        );
    }

    public void TakeDamage(
        int damage,
        Vector3 hitPoint,
        Transform attacker)
    {
        if (damage <= 0 || IsDead)
        {
            return;
        }

        if (IsDamageIntercepted(damage, attacker))
        {
            return;
        }

        CurrentHealth = Mathf.Max(
            CurrentHealth - damage,
            0
        );

        Debug.Log(
            $"{gameObject.name} took {damage} damage. " +
            $"Health: {CurrentHealth}/{MaxHealth}"
        );

        Damaged?.Invoke(damage);

        DamagedAtPosition?.Invoke(
            damage,
            hitPoint
        );

        HealthChanged?.Invoke(
            CurrentHealth,
            MaxHealth
        );

        if (attacker != null)
        {
            DamagedBy?.Invoke(attacker);
        }

        if (IsDead)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        CurrentHealth = Mathf.Min(
            CurrentHealth + amount,
            MaxHealth
        );

        HealthChanged?.Invoke(
            CurrentHealth,
            MaxHealth
        );
    }

    private bool IsDamageIntercepted(
        int damage,
        Transform attacker)
    {
        if (damageInterceptors == null)
        {
            return false;
        }

        foreach (IDamageInterceptor interceptor in damageInterceptors)
        {
            if (interceptor.TryInterceptDamage(damage, attacker))
            {
                return true;
            }
        }

        return false;
    }

    private void Die()
    {
        Debug.Log(
            $"{gameObject.name} died."
        );

        Died?.Invoke();
    }
}