using UnityEngine;
using System.Collections.Generic;

public class TrafficManager : MonoBehaviour
{
    private Camera mainCamera;
    private Transform cameraTransform;

    private float cullDistanceSQ;
    private Vector3 lastCameraPosition;
    private List<TrafficVehicle> activeVehicles = new();

    [Header("Parameters")]
    [SerializeField] private float cullDistance = 100f;
    [SerializeField] private float distanceThreshold = 20.0f;

    [field: Header("Path Controllers")]
    [field: SerializeField] public TrafficPathController LeftController { get; private set; }
    [field: SerializeField] public TrafficPathController RightController { get; private set; }

    private void Awake()
    {
        mainCamera = Camera.main;
        cameraTransform = mainCamera.transform;
        cullDistanceSQ = MathPhysics_Helper.Square(cullDistance);
        activeVehicles = new(GetComponentsInChildren<TrafficVehicle>());
    }

    private void Start()
    {
        activeVehicles.ForEach(x => PrepareVehicles(x));
    }

    private void Update()
    {
        if (Vector3.Distance(lastCameraPosition, cameraTransform.position) > distanceThreshold)
        {
            CullDistantVehicles();
        }
        foreach (var vehicle in activeVehicles)
        {
            vehicle.TrafficVehicle_Update();
        }
    }

    private void PrepareVehicles(TrafficVehicle vehicle)
    {
        vehicle.SetPathIstructions();     
        vehicle.canUpdate = true;
    }

    private void CullDistantVehicles()
    {
        for (int i = activeVehicles.Count - 1; i >= 0; i--)
        {
            TrafficVehicle vehicle = activeVehicles[i];
            Transform vehicleTransform = vehicle.transform;
            if (vehicle == null)
            {
                activeVehicles.RemoveAt(i);
                continue;
            }

            float sqrDistance = (cameraTransform.position - vehicleTransform.position).sqrMagnitude;
            if (sqrDistance > cullDistanceSQ)
            {
                activeVehicles[i].gameObject.SetActive(false);
            }
        }
    }

    public void ClearWaypoint()
    {
        LeftController.ClearPath();
        RightController.ClearPath();
    }
}