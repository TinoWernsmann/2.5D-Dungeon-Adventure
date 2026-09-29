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
    public event Action Died;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
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

        HealthChanged?.Invoke(
            CurrentHealth,
            MaxHealth
        );

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

    private void Die()
    {
        Debug.Log($"{gameObject.name} died.");

        Died?.Invoke();
    }
}