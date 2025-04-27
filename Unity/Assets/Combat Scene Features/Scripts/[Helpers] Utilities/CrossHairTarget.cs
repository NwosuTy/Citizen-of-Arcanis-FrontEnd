using UnityEngine;

public class CrossHairTarget : MonoBehaviour
{
    Ray ray;
    Camera mainCamera;
    RaycastHit hitInfo;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        ray.origin = mainCamera.transform.position;
        ray.direction = mainCamera.transform.forward;

        if (Physics.Raycast(ray, out hitInfo))
        {
            transform.position = hitInfo.point;
            return;
        }
        transform.position = ray.origin + ray.direction * 1000f;
    }
}
