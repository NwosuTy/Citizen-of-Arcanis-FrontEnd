using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Lightweight aggregator of vehicle subsystems and status.
/// Responsible for wiring up references and the main update loop.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class VehicleManager : MonoBehaviour
{
    public Rigidbody RB { get; private set; }
    public UnityEvent<WayPointNode, Vector3> Assignment;

    public WheelManager Wheels { get; private set; }
    public VehicleMovement Movement { get; private set; }
    public AIVehicleDirector AIController { get; private set; }
    public VehiclePhysicsController PhysicsController { get; private set; }

    [Header("Status")]
    public bool isGrounded;
    public bool playerControlled;
    public CarDrive_Type drive_Type = CarDrive_Type.AllWheels;
    public DrivingBehavior drivingBehavior = DrivingBehavior.Traffic;
    public DriverExperience driverExperience = DriverExperience.Mid_Snr;
    [SerializeField] private WheelCount wheelCountStatus = WheelCount.Four;

    [Header("Navigation Parameters (read-only)")]
    [ReadOnlyInspector] public float steerAngle;
    [ReadOnlyInspector] public float accelValue;
    [ReadOnlyInspector] public float brakeValue;

    [Header("Input Parameters")]
    public bool debug;
    [HideInInspector] public float brakeInput;
    [HideInInspector] public float verticalInput;
    [HideInInspector] public float horizontalInput;
    public int WheelCountInt => (int)wheelCountStatus;

    private void Awake()
    {
        RB = GetComponent<Rigidbody>();
        Assignment?.RemoveAllListeners();

        Movement = GetComponent<VehicleMovement>();
        Wheels = GetComponentInChildren<WheelManager>();
        AIController = GetComponent<AIVehicleDirector>();
        PhysicsController = GetComponent<VehiclePhysicsController>();
    }

    private void Update()
    {
        Wheels.WheelManager_Update();
        if (AIController != null)
        {
            AIController.enabled = !playerControlled;
            AIController.VehicleDirector_Update();
        }
    }

    private void FixedUpdate()
    {
        Movement.HandleMovement();
        isGrounded = Wheels != null && Wheels.IsGrounded();
    }
}
