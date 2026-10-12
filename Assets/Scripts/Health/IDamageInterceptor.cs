using UnityEngine;

/// <summary>
/// Komponenten auf demselben GameObject wie <see cref="Health"/> können
/// eingehenden Schaden abfangen (z.B. Blocken, Unverwundbarkeit).
/// </summary>
public interface IDamageInterceptor
{
    /// <returns>true, wenn der Schaden vollständig abgefangen wurde.</returns>
    bool TryInterceptDamage(int damage, Transform attacker);
}
