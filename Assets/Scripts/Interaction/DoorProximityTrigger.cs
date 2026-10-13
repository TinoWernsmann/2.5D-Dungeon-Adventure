using UnityEngine;

/// <summary>
/// Unsichtbare Zone um eine Tür. Meldet Gegnern, wenn sie in die Nähe der Tür kommen
/// oder sie wieder verlassen. So prüfen Gegner nur dann, ob sie eine Tür öffnen müssen,
/// wenn wirklich eine in der Nähe ist.
///
/// Wird von <see cref="Door"/> zur Laufzeit automatisch erzeugt.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class DoorProximityTrigger : MonoBehaviour
{
    // "Ignore Raycast": Die Zone darf keine Raycasts abfangen (Interagieren, Angriffe, Sicht).
    private const int IgnoreRaycastLayer = 2;

    private Door door;

    public static void Create(Door door, Vector3 worldCenter, float radius)
    {
        GameObject zone = new("EnemyDoorZone")
        {
            layer = IgnoreRaycastLayer
        };

        zone.transform.SetParent(door.transform, worldPositionStays: false);
        zone.transform.position = worldCenter;

        // Trigger-Ereignisse brauchen einen Rigidbody auf einer der beiden Seiten.
        Rigidbody body = zone.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        SphereCollider sphere = zone.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = radius / GetLargestScale(zone.transform);

        zone.AddComponent<DoorProximityTrigger>().door = door;
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyMovement enemy = other.GetComponentInParent<EnemyMovement>();

        if (enemy != null)
        {
            enemy.SetDoorNearby(door, isNearby: true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyMovement enemy = other.GetComponentInParent<EnemyMovement>();

        if (enemy != null)
        {
            enemy.SetDoorNearby(door, isNearby: false);
        }
    }

    private static float GetLargestScale(Transform target)
    {
        Vector3 scale = target.lossyScale;

        float largest = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

        return largest > 0.0001f ? largest : 1f;
    }
}
