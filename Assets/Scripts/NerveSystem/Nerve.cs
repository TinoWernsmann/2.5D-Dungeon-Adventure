using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Nerve : MonoBehaviour
{
    public event Action OnNerveCut;

    [SerializeField] private Health _health;

    private void OnEnable()
    {
        _health.Died += HandleNerveDeath;
    }

    private void OnDisable()
    {
        _health.Died -= HandleNerveDeath;
    }

    private void HandleNerveDeath()
    {
        OnNerveCut?.Invoke();
        Destroy(this.gameObject);
    }
}
