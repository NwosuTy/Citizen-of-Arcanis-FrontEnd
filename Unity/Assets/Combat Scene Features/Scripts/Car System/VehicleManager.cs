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

    [Header("Navigation Parameters")]
    public float steerAngle;
    public float accelValue;
    public float brakeValue;

    [Header("Input Parameters")]
    public float verticalInput;
    public float horizontalInput;

    private void Awake()
    {
        Assignment.RemoveAllListeners();
        RB = GetComponent<Rigidbody>();
        Movement = GetComponent<VehicleMovement>();

        AIController = GetComponent<AIVehicleDirector>();
        Wheels = GetComponentInChildren<WheelManager>();
        PhysicsController = GetComponent<VehiclePhysicsController>();
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        isGrounded = Wheels.IsGrounded();

        //If Player Controls Ignore AI Input
        AIController.enabled = (playerControlled != true);

        Wheels.WheelManager_Update();
        AIController.VehicleController_Update(delta);
    }

    private void FixedUpdate()
    {
        Movement.HandleMovement(AIController.Target);
    }
}
