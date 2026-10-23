using UnityEngine;

/// <summary>
/// Fernkämpfer: Hält Abstand, weicht zwischen den Zaubern seitlich aus
/// und zielt mit dem Magic Bolt dorthin, wo der Player gleich sein wird.
/// Wird er in eine Ecke gedrängt, zaubert er sofort statt sinnlos gegen die Wand zu laufen.
/// Steht ein Verbündeter (oder eine Wand) in der Schusslinie, hält er den Zauber zurück
/// und sucht sich seitlich eine freie Schussbahn.
/// Läuft ihm der Player hinterher, schießt er auch im Rückwärtsgehen (nur der schnelle Bolt).
/// Optional (Boss): beschwört Diener und wird bei wenig Leben wütend.
/// </summary>
public class MageCombatBehaviour : EnemyCombatBehaviour
{
    private enum CombatState
    {
        None,
        Approach,
        Retreat,
        Reposition,
        Cast,
        Recovery
    }

    private enum SpellType
    {
        MagicBolt,
        Fireball,
        Summon
    }

    [Header("Projectile")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Combat Distance")]
    [Min(0.1f)]
    [SerializeField] private float combatRange = 12f;

    [Min(0.1f)]
    [SerializeField] private float minimumDistance = 5f;

    [Min(0.1f)]
    [SerializeField] private float preferredDistance = 8f;

    [Min(0.1f)]
    [SerializeField] private float maximumAttackDistance = 10f;

    [Header("Magic Bolt")]
    [SerializeField] private EnemyProjectile magicBoltPrefab;

    [Min(1)]
    [SerializeField] private int magicBoltDamage = 10;

    [Min(0.01f)]
    [SerializeField] private float magicBoltCastDuration = 0.5f;

    [Min(0f)]
    [SerializeField] private float magicBoltRecoveryDuration = 0.65f;

    [Tooltip("Vorausberechnung der Player-Bewegung in Sekunden (0 = direkt auf den Player).")]
    [Min(0f)]
    [SerializeField] private float magicBoltAimLeadTime = 0.35f;

    [Tooltip("So viele Meter wird der Player bei einem Treffer zurückgestoßen. 0 = kein Rückstoß.")]
    [Min(0f)]
    [SerializeField] private float magicBoltKnockback;

    [Header("Fireball")]
    [SerializeField] private EnemyProjectile fireballPrefab;

    [Min(1)]
    [SerializeField] private int fireballDamage = 25;

    [Min(0.01f)]
    [SerializeField] private float fireballCastDuration = 1.2f;

    [Min(0f)]
    [SerializeField] private float fireballRecoveryDuration = 1.4f;

    [Range(0f, 1f)]
    [SerializeField] private float fireballChance = 0.35f;

    [Min(0f)]
    [SerializeField] private float fireballCooldown = 4f;

    [Tooltip("So viele Meter wird der Player bei einem Treffer zurückgestoßen. 0 = kein Rückstoß.")]
    [Min(0f)]
    [SerializeField] private float fireballKnockback;

    [Header("Retreat")]
    [Min(0.1f)]
    [SerializeField] private float retreatDistance = 3f;

    [Min(0.1f)]
    [SerializeField] private float retreatDuration = 1f;

    [Tooltip("Chance, schon beim Start eines Rückzugs im Rückwärtsgehen zu schießen.")]
    [Range(0f, 1f)]
    [SerializeField] private float retreatCastChance = 0.35f;

    [Header("Strafe")]
    [Tooltip("Seitlicher Schritt während der Erholung nach einem Zauber.")]
    [Min(0f)]
    [SerializeField] private float strafeStepDegrees = 25f;

    [Header("Line of Fire")]
    [Tooltip("Breite der Schussbahn, die frei sein muss (etwa so groß wie das Projektil).")]
    [Min(0.01f)]
    [SerializeField] private float lineOfFireRadius = 0.3f;

    [Tooltip("So lange hält er einen fertigen Zauber zurück, wenn die Schussbahn blockiert ist.")]
    [Min(0f)]
    [SerializeField] private float maxSpellHoldDuration = 0.75f;

    [Tooltip("Maximale Dauer, um eine freie Schussbahn zu suchen.")]
    [Min(0.1f)]
    [SerializeField] private float repositionDuration = 0.8f;

    [Min(0f)]
    [SerializeField] private float repositionStepDegrees = 40f;

    [Header("Summon (optional)")]
    [SerializeField] private MinionSummoner summoner = new();

    [Header("Enrage (optional)")]
    [Tooltip("Unter diesem Lebensanteil wird er wütend: häufigere Feuerbälle und Beschwörungen. 0 = aus.")]
    [Range(0f, 1f)]
    [SerializeField] private float enrageHealthPercent;

    [Range(0f, 1f)]
    [SerializeField] private float enragedFireballChance = 0.6f;

    [Tooltip("Wütend wirft er so viele Feuerbälle gleichzeitig im Fächer.")]
    [Min(1)]
    [SerializeField] private int enragedFireballCount = 1;

    [Tooltip("Winkel zwischen zwei Feuerbällen im Fächer.")]
    [Range(0f, 45f)]
    [SerializeField] private float fanSpreadAngle = 20f;

    [Header("Rapid Fire (optional)")]
    [SerializeField] private RapidFireMode rapidFire = new();

    [Header("Debug")]
    [SerializeField] private bool debugCombat;

    private CombatState currentState = CombatState.None;
    private SpellType currentSpell = SpellType.MagicBolt;

    private float stateTimer;
    private float castDuration;
    private float castElapsed;
    private float fireballCooldownTimer;

    private Health health;
    private Vector3 retreatPoint;
    private bool isRetreatCast;

    private float HealthPercent =>
        health != null && health.MaxHealth > 0
            ? (float)health.CurrentHealth / health.MaxHealth
            : 1f;

    private bool IsEnraged =>
        enrageHealthPercent > 0f &&
        HealthPercent <= enrageHealthPercent;

    public override bool IsPerformingAttack =>
        currentState == CombatState.Cast;

    public override float AttackProgress =>
        currentState == CombatState.Cast && castDuration > 0f
            ? Mathf.Clamp01(castElapsed / castDuration)
            : 0f;

    public bool IsCasting => currentState == CombatState.Cast;

    /// <summary>Beschwört Diener -> zählt als Boss (z.B. für Erfolge).</summary>
    public bool IsSummoner => summoner.IsEnabled;

    public bool IsRetreating => currentState == CombatState.Retreat;

    public bool IsCastingFireball =>
        IsCasting && currentSpell == SpellType.Fireball;

    public bool IsCastingMagicBolt =>
        IsCasting && currentSpell == SpellType.MagicBolt;

    protected override void Awake()
    {
        base.Awake();

        health = GetComponent<Health>();
        summoner.Initialize(transform);
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Died += HandleDied;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (health != null)
        {
            health.Died -= HandleDied;
        }
    }

    private void HandleDied()
    {
        summoner.KillAllMinions();

        if (IsSummoner)
        {
            LevelStats.Current?.RecordBossKilled(this);
        }
    }

    private void Update()
    {
        fireballCooldownTimer =
            Mathf.Max(0f, fireballCooldownTimer - Time.deltaTime);

        summoner.Tick(Time.deltaTime);
    }

    public override bool IsInCombatRange(Transform target)
    {
        return GetHorizontalDistance(target) <= combatRange;
    }

    protected override void OnCombatEntered()
    {
        summoner.StartCombat();
        ChangeState(CombatState.Approach);
    }

    protected override void OnCombatUpdate()
    {
        if (rapidFire.Tick(Time.deltaTime, HealthPercent))
        {
            rapidFire.PlayStartSound(audioSource);
            LogCombat("Rapid fire started.");
        }

        switch (currentState)
        {
            case CombatState.None:
            case CombatState.Approach:
                UpdateApproach();
                break;

            case CombatState.Retreat:
                UpdateRetreat();
                break;

            case CombatState.Reposition:
                UpdateReposition();
                break;

            case CombatState.Cast:
                UpdateCast();
                break;

            case CombatState.Recovery:
                UpdateRecovery();
                break;
        }
    }

    protected override void OnCombatExited()
    {
        movement.Stop();
        movement.ResetRotationControl();

        currentState = CombatState.None;
    }

    private void UpdateApproach()
    {
        float distance = DistanceToTarget();

        if (distance < minimumDistance)
        {
            TryStartRetreat();
            return;
        }

        if (distance > maximumAttackDistance)
        {
            movement.MoveTo(Target.position);
            return;
        }

        // Beschwören braucht keine freie Schussbahn.
        if (!summoner.CanSummon && !HasClearShot())
        {
            StartReposition();
            return;
        }

        StartCast();
    }

    private void StartReposition()
    {
        if (Random.value < 0.5f)
        {
            FlipStrafeDirection();
        }

        ChangeState(CombatState.Reposition);

        LogCombat("Line of fire blocked -> Reposition.");
    }

    /// <summary>Seitlich bewegen, bis die Schussbahn frei ist.</summary>
    private void UpdateReposition()
    {
        stateTimer -= Time.deltaTime;

        StrafeAroundTarget(GetPreferredStrafeRadius(), repositionStepDegrees);

        if (HasClearShot() || stateTimer <= 0f)
        {
            ChangeState(CombatState.Approach);
        }
    }

    private bool HasClearShot()
    {
        if (projectileSpawnPoint == null)
        {
            return true;
        }

        return IsLineOfFireClear(
            projectileSpawnPoint.position,
            GetAimPosition(),
            lineOfFireRadius
        );
    }

    private float GetPreferredStrafeRadius()
    {
        return Mathf.Clamp(DistanceToTarget(), minimumDistance, preferredDistance);
    }

    private void TryStartRetreat()
    {
        retreatPoint =
            GetRetreatPoint(retreatDistance, Random.Range(-40f, 40f));

        bool isCornered =
            Vector3.Distance(transform.position, retreatPoint) <
            retreatDistance * 0.3f;

        if (isCornered)
        {
            LogCombat("Cornered -> casting immediately.");
            StartCast(isCloseRange: true);
            return;
        }

        movement.MoveToWhileFacing(retreatPoint, Target);
        ChangeState(CombatState.Retreat);

        LogCombat("Player too close -> Retreat.");

        if (Random.value <= retreatCastChance)
        {
            StartRetreatCast();
        }
    }

    /// <summary>Schießt im Rückwärtsgehen, damit Hinterherlaufen nicht ungestraft bleibt.</summary>
    private void StartRetreatCast()
    {
        StartCast(isCloseRange: true);
        isRetreatCast = true;

        LogCombat("Casting while retreating.");
    }

    private void UpdateRetreat()
    {
        stateTimer -= Time.deltaTime;

        bool isFarEnough = DistanceToTarget() >= preferredDistance;

        if (!isFarEnough &&
            stateTimer > 0f &&
            !movement.HasReachedDestination)
        {
            return;
        }

        // Player läuft hinterher -> nicht nur zurückweichen, sondern zurückschießen.
        if (DistanceToTarget() < minimumDistance)
        {
            StartRetreatCast();
            return;
        }

        ChangeState(CombatState.Approach);
    }

    private void StartCast(bool isCloseRange = false)
    {
        movement.StopAndFace(Target);

        isRetreatCast = false;

        SelectSpell(isCloseRange);
        ChangeState(CombatState.Cast);
    }

    /// <summary>Aus nächster Nähe nur der schnelle Bolt – ein Feuerball wäre dort kaum ausweichbar.</summary>
    private void SelectSpell(bool isCloseRange)
    {
        float currentFireballChance = IsEnraged
            ? enragedFireballChance
            : fireballChance;

        // Im Dauerfeuer nur schnelle Bolts.
        bool isRapidFire = rapidFire.IsActive;

        bool useFireball =
            !isCloseRange &&
            !isRapidFire &&
            fireballPrefab != null &&
            fireballCooldownTimer <= 0f &&
            Random.value <= currentFireballChance;

        currentSpell =
            !isRapidFire && summoner.CanSummon ? SpellType.Summon :
            useFireball ? SpellType.Fireball :
            SpellType.MagicBolt;

        castDuration = currentSpell switch
        {
            SpellType.Summon => summoner.CastDuration,
            SpellType.Fireball => fireballCastDuration,
            _ => magicBoltCastDuration / rapidFire.SpeedMultiplier
        };

        LogCombat($"Preparing {currentSpell}.");
    }

    private void UpdateCast()
    {
        castElapsed += Time.deltaTime;

        if (castElapsed < castDuration)
        {
            // Bolt im Rückzug: weiterlaufen. Beschwörung und Feuerball: stillstehen.
            if (isRetreatCast && currentSpell == SpellType.MagicBolt)
            {
                movement.MoveToWhileFacing(retreatPoint, Target);
            }
            else
            {
                movement.StopAndFace(Target);
            }

            return;
        }

        if (currentSpell == SpellType.Summon || HasClearShot())
        {
            ReleaseSpell();
            ChangeState(CombatState.Recovery);
            return;
        }

        // Verbündeter / Wand im Weg: Zauber halten und zur Seite gehen.
        StrafeAroundTarget(GetPreferredStrafeRadius(), repositionStepDegrees);

        if (castElapsed >= castDuration + maxSpellHoldDuration)
        {
            LogCombat("No clear shot -> spell cancelled.");
            StartReposition();
        }
    }

    private void ReleaseSpell()
    {
        if (currentSpell == SpellType.Summon)
        {
            ReleaseSummon();
            return;
        }

        EnemyProjectile prefab = GetCurrentProjectilePrefab();

        if (prefab == null || projectileSpawnPoint == null)
        {
            Debug.LogWarning(
                $"{gameObject.name}: Projectile prefab or spawn point " +
                $"missing for {currentSpell}.",
                gameObject
            );

            return;
        }

        Vector3 direction =
            GetAimPosition() - projectileSpawnPoint.position;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.forward;
        }

        bool isFireball = currentSpell == SpellType.Fireball;

        int projectileCount = isFireball && IsEnraged
            ? enragedFireballCount
            : 1;

        float knockback = isFireball
            ? fireballKnockback
            : magicBoltKnockback;

        for (int i = 0; i < projectileCount; i++)
        {
            // Fächer symmetrisch um die Zielrichtung, der mittlere fliegt direkt aufs Ziel.
            float angle = (i - (projectileCount - 1) * 0.5f) * fanSpreadAngle;

            EnemyProjectile projectile = Instantiate(
                prefab,
                projectileSpawnPoint.position,
                Quaternion.identity
            );

            projectile.Initialize(
                Quaternion.Euler(0f, angle, 0f) * direction,
                GetCurrentSpellDamage(),
                gameObject,
                knockback
            );
        }

        if (isFireball)
        {
            fireballCooldownTimer = fireballCooldown;
        }

        LogCombat($"Released {currentSpell}.");
    }

    private void ReleaseSummon()
    {
        summoner.Summon(IsEnraged);

        if (audioSource != null && summoner.SummonSound != null)
        {
            audioSource.PlayOneShot(summoner.SummonSound);
        }

        LogCombat("Summoned minions.");
    }

    /// <summary>
    /// Der schnelle Bolt zielt voraus, der langsame Feuerball direkt auf den Player
    /// (der Feuerball soll ausweichbar bleiben).
    /// </summary>
    private Vector3 GetAimPosition()
    {
        float leadTime = currentSpell == SpellType.MagicBolt
            ? magicBoltAimLeadTime
            : 0f;

        return PredictTargetPosition(leadTime) + Vector3.up;
    }

    private void UpdateRecovery()
    {
        stateTimer -= Time.deltaTime;

        // Nicht still stehen: seitlich versetzen, damit der Player nicht einfach draufhält.
        StrafeAroundTarget(GetPreferredStrafeRadius(), strafeStepDegrees);

        if (stateTimer > 0f)
        {
            return;
        }

        if (DistanceToTarget() < minimumDistance)
        {
            TryStartRetreat();
            return;
        }

        ChangeState(CombatState.Approach);
    }

    private EnemyProjectile GetCurrentProjectilePrefab()
    {
        return currentSpell == SpellType.Fireball
            ? fireballPrefab
            : magicBoltPrefab;
    }

    private int GetCurrentSpellDamage()
    {
        return currentSpell == SpellType.Fireball
            ? fireballDamage
            : magicBoltDamage;
    }

    private float GetCurrentRecoveryDuration()
    {
        // Nach der Beschwörung so lange verwundbar wie nach dem Feuerball.
        return currentSpell == SpellType.MagicBolt
            ? magicBoltRecoveryDuration
            : fireballRecoveryDuration;
    }

    private void ChangeState(CombatState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case CombatState.Retreat:
                stateTimer = retreatDuration;
                break;

            case CombatState.Reposition:
                stateTimer = repositionDuration;
                break;

            case CombatState.Cast:
                castElapsed = 0f;
                break;

            case CombatState.Recovery:
                stateTimer = GetCurrentRecoveryDuration() / rapidFire.SpeedMultiplier;
                break;
        }
    }

    private void LogCombat(string message)
    {
        if (debugCombat)
        {
            Debug.Log($"[MAGE] {message}", gameObject);
        }
    }
}
