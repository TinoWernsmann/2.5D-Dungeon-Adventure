using UnityEngine;

public class ItemBillboard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    private void Start()
    {
        FindCameraIfMissing();
    }

    private void LateUpdate()
    {
        BillboardUtility.FaceCamera(
            transform,
            targetCamera
        );
    }

    private void FindCameraIfMissing()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }
}
