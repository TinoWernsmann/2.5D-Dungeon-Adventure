using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [Min(0f)]
    [SerializeField] private float attackRange = 1.5f;

    [Min(1)]
    [SerializeField] private int damage = 20;

    [Min(0.01f)]
    [SerializeField] private float attackDuration = 0.6f;

    [Range(0f, 1f)]
    [SerializeField] private float damageMoment = 0.5f;

    [Min(0f)]
    [SerializeField] private float attackCooldown = 0.6f;

    public bool IsAttacking { get; private set; }

    public float AttackProgress
    {
        get
        {
            if (!IsAttacking)
            {
                return 0f;
            }

            return Mathf.Clamp01(attackTimer / attackDuration);
        }
    }

    private Health currentTarget;
    private float attackTimer;
    private float cooldownTimer;
    private bool damageApplied;

    private void Update()
    {
        UpdateCooldown();
        UpdateAttack();
    }

    public bool IsTargetInRange(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 enemyPosition = transform.position;
        Vector3 targetPosition = target.position;

        enemyPosition.y = 0f;
        targetPosition.y = 0f;

        float distance = Vector3.Distance(
            enemyPosition,
            targetPosition
        );

        return distance <= attackRange;
    }

    public void TryAttack(Health targetHealth)
    {
        if (!CanStartAttack(targetHealth))
        {
            return;
        }

        StartAttack(targetHealth);
    }

    private bool CanStartAttack(Health targetHealth)
    {
        return targetHealth != null &&
               !targetHealth.IsDead &&
               !IsAttacking &&
               cooldownTimer <= 0f;
    }

    private void StartAttack(Health targetHealth)
    {
        currentTarget = targetHealth;

        attackTimer = 0f;
        damageApplied = false;
        IsAttacking = true;
    }

    private void UpdateAttack()
    {
        if (!IsAttacking)
        {
            return;
        }

        attackTimer += Time.deltaTime;

        TryApplyDamage();

        if (attackTimer >= attackDuration)
        {
            FinishAttack();
        }
    }

    private void TryApplyDamage()
    {
        if (damageApplied || currentTarget == null)
        {
            return;
        }

        float damageTime = attackDuration * damageMoment;

        if (attackTimer < damageTime)
        {
            return;
        }

        damageApplied = true;

        if (IsTargetInRange(currentTarget.transform))
        {
            currentTarget.TakeDamage(damage);
        }
    }

    private void FinishAttack()
    {
        IsAttacking = false;

        attackTimer = 0f;
        cooldownTimer = attackCooldown;

        currentTarget = null;
    }

    private void UpdateCooldown()
    {
        if (cooldownTimer <= 0f)
        {
            return;
        }

        cooldownTimer -= Time.deltaTime;
    }
}