using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Movement")]
    [Min(0.1f)]
    [SerializeField] private float speed = 8f;

    [Min(0.1f)]
    [SerializeField] private float lifetime = 6f;

    [Header("Collision")]
    [Min(0.01f)]
    [SerializeField] private float hitRadius = 0.2f;

    [SerializeField] private LayerMask collisionMask = ~0;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] shootSounds;

    private Vector3 direction;
    private GameObject owner;

    private int damage;
    private float lifetimeTimer;
    private bool initialized;

    public void Initialize(
        Vector3 travelDirection,
        int projectileDamage,
        GameObject projectileOwner)
    {
        direction = travelDirection.normalized;
        damage = projectileDamage;
        owner = projectileOwner;

        lifetimeTimer = lifetime;
        initialized = true;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(direction);
        }

        PlayRandomShootSound();
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        UpdateLifetime();
        MoveProjectile();
    }

    private void UpdateLifetime()
    {
        lifetimeTimer -= Time.deltaTime;

        if (lifetimeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void MoveProjectile()
    {
        float moveDistance =
            speed * Time.deltaTime;

        Vector3 startPosition =
            transform.position;

        if (TryHitTarget(
            startPosition,
            moveDistance))
        {
            return;
        }

        transform.position +=
            direction * moveDistance;
    }

    private bool TryHitTarget(
        Vector3 startPosition,
        float moveDistance)
    {
        bool hasHit = Physics.SphereCast(
            startPosition,
            hitRadius,
            direction,
            out RaycastHit hit,
            moveDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore
        );

        if (!hasHit)
        {
            return false;
        }

        if (IsOwner(hit.collider))
        {
            transform.position +=
                direction * moveDistance;

            return false;
        }

        Health targetHealth =
            hit.collider.GetComponentInParent<Health>();

        if (targetHealth != null &&
            !targetHealth.IsDead)
        {
            targetHealth.TakeDamage(damage);
        }

        Destroy(gameObject);

        return true;
    }

    private bool IsOwner(Collider hitCollider)
    {
        if (owner == null)
        {
            return false;
        }

        return hitCollider.transform == owner.transform ||
               hitCollider.transform.IsChildOf(
                   owner.transform
               );
    }

    private void PlayRandomShootSound()
    {
        if (audioSource == null ||
            shootSounds == null ||
            shootSounds.Length == 0)
        {
            return;
        }

        int randomIndex =
            Random.Range(0, shootSounds.Length);

        AudioClip randomClip =
            shootSounds[randomIndex];

        if (randomClip == null)
        {
            return;
        }

        audioSource.PlayOneShot(randomClip);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            hitRadius
        );
    }
}