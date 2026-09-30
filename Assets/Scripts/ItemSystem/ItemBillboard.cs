using UnityEngine;

public class ItemBillboard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        FaceCamera();
    }

    private void FaceCamera()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 directionToCamera =
            targetCamera.transform.position - transform.position;

        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude <= 0.001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(
            directionToCamera
        );
    }
}
