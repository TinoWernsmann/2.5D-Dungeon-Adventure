using UnityEngine;

[RequireComponent(typeof(Health))]
public class DestructibleCrate : MonoBehaviour
{
    [Header("Drop")]
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform dropPoint;

    [Header("Drop Physics")]
    [SerializeField] private float upwardForce = 4f;
    [SerializeField] private float horizontalForce = 1.5f;
    [SerializeField] private float torqueForce = 3f;

    [Header("Hit VFX")]
    [Tooltip("Particle effect played at the exact hit position.")]
    [SerializeField] private GameObject hitVfx;

    [Tooltip("Time before the hit VFX is destroyed.")]
    [SerializeField] private float hitVfxLifetime = 2f;

    [Tooltip("Randomizes the rotation of the hit VFX.")]
    [SerializeField] private bool randomizeVfxRotation = true;

    private Health health;
    private bool destroyed;


    private void Awake()
    {
        health = GetComponent<Health>();
    }


    private void OnEnable()
    {
        health.DamagedAtPosition += HandleDamaged;
        health.Died += HandleDestroyed;
    }


    private void OnDisable()
    {
        health.DamagedAtPosition -= HandleDamaged;
        health.Died -= HandleDestroyed;
    }


    private void HandleDamaged(
        int damage,
        Vector3 hitPoint)
    {
        if (destroyed)
        {
            return;
        }

        SpawnHitVfx(hitPoint);
    }


    private void HandleDestroyed()
    {
        if (destroyed)
        {
            return;
        }

        destroyed = true;

        SpawnItem();

        Destroy(gameObject);
    }


    private void SpawnHitVfx(
        Vector3 hitPoint)
    {
        if (hitVfx == null)
        {
            return;
        }

        Quaternion rotation =
            GetVfxRotation();

        GameObject vfx = Instantiate(
            hitVfx,
            hitPoint,
            rotation
        );

        Destroy(
            vfx,
            hitVfxLifetime
        );
    }


    private Quaternion GetVfxRotation()
    {
        if (!randomizeVfxRotation)
        {
            return Quaternion.identity;
        }

        return Random.rotation;
    }


    private void SpawnItem()
    {
        if (itemPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition =
            dropPoint != null
                ? dropPoint.position
                : transform.position +
                  Vector3.up * 0.5f;

        GameObject droppedItem =
            Instantiate(
                itemPrefab,
                spawnPosition,
                Quaternion.identity
            );

        LaunchItem(droppedItem);
    }


    private void LaunchItem(
        GameObject item)
    {
        if (!item.TryGetComponent(
                out Rigidbody rigidbody))
        {
            return;
        }

        Vector2 randomDirection =
            Random.insideUnitCircle.normalized;

        Vector3 launchDirection = new(
            randomDirection.x *
                horizontalForce,
            upwardForce,
            randomDirection.y *
                horizontalForce
        );

        rigidbody.AddForce(
            launchDirection,
            ForceMode.Impulse
        );

        rigidbody.AddTorque(
            Random.insideUnitSphere *
                torqueForce,
            ForceMode.Impulse
        );
    }
}