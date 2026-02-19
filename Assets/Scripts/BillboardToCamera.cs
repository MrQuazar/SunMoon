using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        transform.rotation = Quaternion.LookRotation(
            targetCamera.transform.forward,
            targetCamera.transform.up
        );
    }
}
