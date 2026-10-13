using UnityEngine;

/// <summary>
/// Basis für alle Kampfverhalten. Der <see cref="EnemyBrain"/> entscheidet WANN gekämpft wird,
/// die Unterklassen entscheiden WIE.
///
/// Ablauf: EnterCombat -> UpdateCombat (jeden Frame) -> ExitCombat.
/// Unterklassen implementieren dafür OnCombatEntered / OnCombatUpdate / OnCombatExited.
///
/// Wird der Gegner an eine Wand / in eine Ecke gedrängt, bricht er automatisch aus
/// (<see cref="CorneredEscape"/>). Das normale Kampfverhalten pausiert solange.
/// </summary>
public abstract class EnemyCombatBehaviour : MonoBehaviour
{
    private const float TargetVelocitySmoothing = 10f;

    [Header("References")]
    [SerializeField] protected EnemyMovement movement;
    [SerializeField] protected AudioSource audioSource;

    [Header("Audio")]
    [SerializeField] protected AudioClip[] attackSounds;

    [Header("Cornered")]
    [SerializeField] private CorneredEscape corneredEscape = new();

    private Vector3 lastTargetPosition;
    private float strafeSign = 1f;

    protected Transform Target { get; private set; }
    protected Health TargetHealth { get; private set; }

    /// <summary>Geschätzte Geschwindigkeit des Ziels – für vorausschauendes Zielen.</summary>
    protected Vector3 TargetVelocity { get; private set; }

    public abstract bool IsPerformingAttack { get; }

    public abstract float AttackProgress { get; }

    public abstract bool IsInCombatRange(Transform target);

    /// <summary>Darf der Gegner gerade aus einer Ecke ausbrechen? (Nicht mitten im Angriff.)</summary>
    protected virtual bool CanEscapeFromCorner => !IsPerformingAttack;

    protected abstract void OnCombatEntered();

    protected abstract void OnCombatUpdate();

    protected abstract void OnCombatExited();

    protected virtual void Awake()
    {
        if (movement == null)
        {
            movement = GetComponent<EnemyMovement>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        strafeSign = Random.value < 0.5f ? -1f : 1f;

        corneredEscape.Initialize(transform, movement);
    }

    protected virtual void OnDisable()
    {
        ReleaseAttackSlot();
    }

    public void EnterCombat(Transform target, Health targetHealth)
    {
        SetTarget(target, targetHealth);

        lastTargetPosition = target.position;
        TargetVelocity = Vector3.zero;

        OnCombatEntered();
    }

    public void UpdateCombat(Transform target, Health targetHealth)
    {
        SetTarget(target, targetHealth);
        UpdateTargetVelocity();

        if (!HasValidTarget())
        {
            return;
        }

        bool isEscapingCorner = corneredEscape.Tick(
            Target,
            CanEscapeFromCorner,
            Time.deltaTime
        );

        if (isEscapingCorner)
        {
            return;
        }

        OnCombatUpdate();
    }

    public void ExitCombat()
    {
        ReleaseAttackSlot();
        corneredEscape.Cancel();

        OnCombatExited();

        Target = null;
        TargetHealth = null;
    }

    #region Brain Hooks

    /// <summary>Der Brain hat den Player entdeckt.</summary>
    public virtual void OnTargetSpotted(Transform target, Health targetHealth)
    {
    }

    /// <summary>Der Brain hat die Suche aufgegeben und kehrt zur Patrouille zurück.</summary>
    public virtual void OnTargetLost()
    {
    }

    /// <summary>Bewegung, solange der Player gesehen wird, aber noch nicht in Kampfreichweite ist.</summary>
    public virtual void UpdateChase(Transform target)
    {
        movement.MoveTo(target.position);
    }

    #endregion

    #region Target Helpers

    protected bool HasValidTarget()
    {
        return Target != null &&
               TargetHealth != null &&
               !TargetHealth.IsDead;
    }

    protected float GetHorizontalDistance(Transform target)
    {
        if (target == null)
        {
            return float.MaxValue;
        }

        Vector3 offset = target.position - transform.position;
        offset.y = 0f;

        return offset.magnitude;
    }

    protected float DistanceToTarget()
    {
        return GetHorizontalDistance(Target);
    }

    /// <summary>Horizontale Richtung zum Ziel (normalisiert).</summary>
    protected Vector3 DirectionToTarget()
    {
        return HorizontalDirectionTo(Target.position);
    }

    /// <summary>Wo das Ziel in <paramref name="secondsAhead"/> Sekunden vermutlich sein wird.</summary>
    protected Vector3 PredictTargetPosition(float secondsAhead)
    {
        Vector3 velocity = TargetVelocity;
        velocity.y = 0f;

        return Target.position + velocity * secondsAhead;
    }

    protected Vector3 HorizontalDirectionTo(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;

        return direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : transform.forward;
    }

    /// <summary>Fügt dem Ziel Schaden zu, falls es in Reichweite ist.</summary>
    protected bool TryHitTarget(float range, int damage)
    {
        if (!HasValidTarget() ||
            DistanceToTarget() > range)
        {
            return false;
        }

        TargetHealth.TakeDamage(
            damage,
            Target.position,
            transform
        );

        return true;
    }

    /// <summary>
    /// Ist die Schusslinie frei? Der erste fremde Collider auf dem Weg entscheidet:
    /// Ist es das Ziel -> frei. Ist es ein Verbündeter oder eine Wand -> blockiert.
    /// </summary>
    protected bool IsLineOfFireClear(Vector3 from, Vector3 to, float radius)
    {
        Vector3 offset = to - from;
        float distance = offset.magnitude;

        if (distance <= 0.01f)
        {
            return true;
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            from,
            radius,
            offset / distance,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;

            if (hitTransform.IsChildOf(transform))
            {
                continue;
            }

            return Target != null && hitTransform.IsChildOf(Target);
        }

        return true;
    }

    #endregion

    #region Movement Helpers

    /// <summary>
    /// Umkreist das Ziel seitlich im Abstand <paramref name="radius"/>.
    /// Wechselt die Richtung, wenn eine Wand im Weg ist.
    /// </summary>
    protected void StrafeAroundTarget(float radius, float stepDegrees = 35f)
    {
        Vector3 fromTarget = -DirectionToTarget();

        Vector3 desiredOffset =
            Quaternion.Euler(0f, stepDegrees * strafeSign, 0f) *
            fromTarget * radius;

        Vector3 desiredPosition = Target.position + desiredOffset;
        Vector3 reachablePosition = movement.GetReachablePoint(desiredPosition);

        const float BlockedTolerance = 0.5f;

        if (Vector3.Distance(desiredPosition, reachablePosition) > BlockedTolerance)
        {
            FlipStrafeDirection();
        }

        movement.MoveToWhileFacing(reachablePosition, Target);
    }

    protected void FlipStrafeDirection()
    {
        strafeSign = -strafeSign;
    }

    /// <summary>
    /// Erreichbarer Punkt weg vom Ziel, optional schräg zur Seite.
    /// Blockiert eine Wand den Rückzug, wird stattdessen der beste Fluchtpunkt gewählt
    /// (z.B. seitlich an der Wand entlang).
    /// </summary>
    protected Vector3 GetRetreatPoint(float distance, float sideAngle = 0f)
    {
        Vector3 awayFromTarget =
            Quaternion.Euler(0f, sideAngle, 0f) * -DirectionToTarget();

        Vector3 retreatPoint = movement.GetReachablePoint(
            transform.position + awayFromTarget * distance
        );

        const float BlockedFraction = 0.5f;

        bool isBlocked =
            Vector3.Distance(transform.position, retreatPoint) < distance * BlockedFraction;

        if (isBlocked &&
            corneredEscape.TryFindEscapePoint(Target.position, out Vector3 escapePoint))
        {
            return escapePoint;
        }

        return retreatPoint;
    }

    #endregion

    #region Attack Slot

    protected bool TryAcquireAttackSlot()
    {
        return EnemyAttackCoordinator.TryAcquireSlot(this);
    }

    protected void ReleaseAttackSlot()
    {
        EnemyAttackCoordinator.ReleaseSlot(this);
    }

    #endregion

    protected void PlayRandomAttackSound()
    {
        if (audioSource == null ||
            attackSounds == null ||
            attackSounds.Length == 0)
        {
            return;
        }

        AudioClip clip =
            attackSounds[Random.Range(0, attackSounds.Length)];

        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    protected void SetTarget(Transform target, Health targetHealth)
    {
        Target = target;
        TargetHealth = targetHealth;
    }

    private void UpdateTargetVelocity()
    {
        if (Target == null || Time.deltaTime <= 0f)
        {
            return;
        }

        Vector3 currentVelocity =
            (Target.position - lastTargetPosition) / Time.deltaTime;

        TargetVelocity = Vector3.Lerp(
            TargetVelocity,
            currentVelocity,
            TargetVelocitySmoothing * Time.deltaTime
        );

        lastTargetPosition = Target.position;
    }
}
