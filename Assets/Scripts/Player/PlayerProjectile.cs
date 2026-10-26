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
    private SurfaceType defaultSurface;
    private float knockbackDistance;
    private bool initialized;
    private bool hasHit;

    public void Initialize(
        Vector3 shootDirection,
        float projectileSpeed,
        int projectileDamage,
        GameObject projectileOwner,
        SurfaceType fallbackSurface,
        float knockback)
    {
        direction =
            shootDirection.normalized;

        speed =
            projectileSpeed;

        damage =
            projectileDamage;

        owner =
            projectileOwner;

        defaultSurface =
            fallbackSurface;

        knockbackDistance =
            knockback;

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

        // Trigger (Bereiche, Tür-Sensoren …) sollen Pfeile nicht aufhalten.
        if (Physics.Linecast(
                startPosition,
                endPosition,
                out RaycastHit hit,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            HandleHit(hit);
            return;
        }

        transform.position =
            endPosition;
    }

    private void HandleHit(
        RaycastHit hit)
    {
        Collider hitCollider =
            hit.collider;

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

        WeaponHit.Resolve(
            hit,
            damage,
            owner != null ? owner.transform : null,
            defaultSurface,
            knockbackDistance,
            isProjectile: true
        );

        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
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