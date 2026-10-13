using UnityEngine;

/// <summary>
/// Alles, was eine <see cref="BossAction"/> vom Boss braucht.
/// Dadurch kennen die Aktionen den konkreten Boss nicht und sind für weitere Bosse wiederverwendbar.
/// </summary>
public interface IBossActionContext
{
    Transform Root { get; }

    EnemyMovement Movement { get; }

    Transform CurrentTarget { get; }

    /// <summary>Skaliert eine Dauer mit dem Tempo der aktuellen Phase.</summary>
    float ScaleDuration(float baseDuration);

    /// <summary>Schaden (skaliert mit der aktuellen Phase), falls das Ziel in Reichweite ist.</summary>
    bool TryDealDamage(float range, int baseDamage);

    Vector3 GetDirectionToPredictedTarget(float secondsAhead);

    void PlayAttackSound();
}
