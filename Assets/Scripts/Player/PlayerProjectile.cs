using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerProjectile : MonoBehaviour
{
    [Header("Projectile")]
    [Min(0.1f)]
    [SerializeField] private float lifetime = 5f;

    [SerializeField]
    private bool destroyOnImpact = true;

    private Vector3 direction;
    private float speed;
    private int damage;

    private GameObject owner;
    private bool initialized;
    private bool hasHit;

    public void Initialize(
        Vector3 shootDirection,
        float projectileSpeed,
        int projectileDamage,
        GameObject projectileOwner)
    {
        direction =
            shootDirection.normalized;

        speed =
            projectileSpeed;

        damage =
            projectileDamage;

        owner =
            projectileOwner;

        initialized = true;

        IgnoreOwnerColliders();

        Destroy(
            gameObject,
            lifetime
        );
    }

    private void Update()
    {
        if (!initialized ||
            hasHit)
        {
            return;
        }

        MoveProjectile();
    }

    private void MoveProjectile()
    {
        float distance =
            speed * Time.deltaTime;

        Vector3 startPosition =
            transform.position;

        Vector3 endPosition =
            startPosition +
            direction * distance;

        if (Physics.Linecast(
                startPosition,
                endPosition,
                out RaycastHit hit))
        {
            HandleHit(hit.collider);
            return;
        }

        transform.position =
            endPosition;
    }

    private void HandleHit(
        Collider hitCollider)
    {
        if (hasHit ||
            hitCollider == null)
        {
            return;
        }

        if (IsOwnerCollider(hitCollider))
        {
            return;
        }

        hasHit = true;

        Health targetHealth =
            hitCollider
                .GetComponentInParent<Health>();

        if (targetHealth != null)
        {
            ApplyDamage(
                hitCollider,
                targetHealth
            );
        }

        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyDamage(
        Collider hitCollider,
        Health targetHealth)
    {
        BossDamageReceiver bossReceiver =
            FindBossDamageReceiver(
                hitCollider,
                targetHealth
            );

        if (bossReceiver != null)
        {
            bossReceiver.ReceiveDamage(
                damage,
                owner.transform
            );

            return;
        }

        targetHealth.TakeDamage(
            damage
        );
    }

    private BossDamageReceiver
        FindBossDamageReceiver(
            Collider hitCollider,
            Health targetHealth)
    {
        BossDamageReceiver receiver =
            hitCollider.GetComponent<
                BossDamageReceiver>();

        if (receiver != null)
        {
            return receiver;
        }

        receiver =
            hitCollider.GetComponentInParent<
                BossDamageReceiver>();

        if (receiver != null)
        {
            return receiver;
        }

        if (targetHealth == null)
        {
            return null;
        }

        receiver =
            targetHealth.GetComponent<
                BossDamageReceiver>();

        if (receiver != null)
        {
            return receiver;
        }

        receiver =
            targetHealth.GetComponentInChildren<
                BossDamageReceiver>();

        if (receiver != null)
        {
            return receiver;
        }

        return targetHealth
            .GetComponentInParent<
                BossDamageReceiver>();
    }

    private bool IsOwnerCollider(
        Collider hitCollider)
    {
        if (owner == null)
        {
            return false;
        }

        Transform hitTransform =
            hitCollider.transform;

        return hitTransform ==
                   owner.transform ||
               hitTransform.IsChildOf(
                   owner.transform
               );
    }

    private void IgnoreOwnerColliders()
    {
        if (owner == null)
        {
            return;
        }

        Collider projectileCollider =
            GetComponent<Collider>();

        Collider[] ownerColliders =
            owner.GetComponentsInChildren<
                Collider>();

        foreach (Collider ownerCollider
                 in ownerColliders)
        {
            Physics.IgnoreCollision(
                projectileCollider,
                ownerCollider
            );
        }
    }
}