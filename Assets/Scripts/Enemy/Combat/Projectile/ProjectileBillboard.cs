using UnityEngine;

public class ProjectileBillboard : MonoBehaviour
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
        BillboardUtility.FaceCamera(
            transform,
            targetCamera,
            keepUpright: false
        );
    }
}
