using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody), typeof(AIVehicleDirector), typeof(VehiclePhysicsController))]
public class VehicleManager : MonoBehaviour
{
    public Rigidbody RB {  get; private set; }
    public UnityEvent<WayPointNode, Vector3> Assignment;

    public WheelManager Wheels {  get; private set; }
    public VehicleMovement Movement { get; private set; }
    public AIVehicleDirector AIController { get; private set; }
    public VehiclePhysicsController PhysicsController { get; private set; }

    [Header("Status")]
    public bool isGrounded;
    public bool playerControlled;
    public CarDrive_Type drive_Type = CarDrive_Type.AllWheels;
    public DrivingBehavior drivingBehavior = DrivingBehavior.Traffic;
    [SerializeField] private WheelCount wheelCountStatus = WheelCount.Four;

    [Header("Navigation Parameters")]
    public float steerAngle;
    public float accelValue;
    public float brakeValue;

    [Header("Input Parameters")]
    public bool debug;
    public float verticalInput;
    public float horizontalInput;
    public int WheelCountInt => (int)wheelCountStatus;

    private void Awake()
    {
        RB = GetComponent<Rigidbody>();
        Assignment?.RemoveAllListeners();
        Movement = GetComponent<VehicleMovement>();

        AIController = GetComponent<AIVehicleDirector>();
        Wheels = GetComponentInChildren<WheelManager>();
        PhysicsController = GetComponent<VehiclePhysicsController>();
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        //If Player Controls Ignore AI Input
        AIController.enabled = (playerControlled != true);

        Wheels.WheelManager_Update();
        AIController.VehicleDirector_Update(delta);
    }

    private void FixedUpdate()
    {
        Movement.HandleMovement();
        isGrounded = Wheels.IsGrounded();
    }
}
