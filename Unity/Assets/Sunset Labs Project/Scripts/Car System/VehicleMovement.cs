using UnityEngine;
using System.Collections;

public class VehicleMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Coroutine kickStartRoutine;
    private VehicleManager vehicleManager;

    [field: Header("Default Stats")]
    [field: SerializeField] public float DefaultSpeed { get; private set; } = 200f;

    [Header("Parameters")]
    public float topSpeed;
    [SerializeField] private float maxSteerAngle = 25f;
    [Tooltip("Starting Torque Value When Drive Just Commences")]
    [SerializeField] private float kickStartTargetTorque = 50f;
    public float CurrentSpeed => rb.velocity.magnitude * 2.23693629f; // mph
    [field: SerializeField] public float BrakeTorque { get; private set; } = 20000f;
    [field: SerializeField] public float ReverseTorque { get; private set; } = 150f;

    [field: Header("Destination And Cautious Parameters")]
    [Tooltip("Distance at which distance-based cautiousness begins")]
    [field: SerializeField] public float CautiousMaxDistance { get; private set; } = 100f;
    [Tooltip("How cautious the AI should be when considering its own angular velocity")]
    [field: SerializeField] public float CautiousAngularVelocityFactor { get; private set; } = 30f;
    [Tooltip("Angle of approaching corner to treat as warranting maximum caution")]
    [field: SerializeField][field: Range(0f, 180f)] public float CautiousMaxAngle { get; private set; } = 50f;
    [Tooltip("Percentage of max speed to use when being maximally cautious")]
    [field: SerializeField][field: Range(0f, 1f)] public float CautiousSpeedFactor { get; private set; } = 0.05f;

    [Header("Status")]
    public bool canDrive;
    private bool driving;
    [SerializeField] private float seconds;
    [field: SerializeField] public BrakeCondition BrakeConditionType { get; private set; } = BrakeCondition.TargetDistance;

    private void Awake()
    {
        vehicleManager = GetComponent<VehicleManager>();
    }

    private void Start()
    {
        rb = vehicleManager.RB;
        topSpeed = DefaultSpeed;
    }

    public void SetDriving(bool status)
    {
        if (!driving && status)
        {
            if (kickStartRoutine != null)
            {
                StopCoroutine(kickStartRoutine);
            }
            kickStartRoutine = StartCoroutine(KickStartVehicleRoutine());
        }
        driving = status;
    }

    private IEnumerator KickStartVehicleRoutine()
    {
        rb.velocity = transform.forward * 2f;
        var wheels = vehicleManager.Wheels.WheelColliders;

        foreach (var wc in wheels)
        {
            wc.motorTorque = 0f;
            wc.brakeTorque = 0f;
        }

        float elapsed = 0f;
        const float duration = 1.5f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float currentTorque = Mathf.Lerp(0f, kickStartTargetTorque, t);
            foreach (var wc in wheels)
                wc.motorTorque = currentTorque;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // finalize at full kick-start torque
        foreach (var wc in wheels)
            wc.motorTorque = kickStartTargetTorque;
    }

    public void HandleMovement()
    {
        SetDriving(canDrive);

        if (vehicleManager.playerControlled)
        {
            PlayerControlled_Movement();
        }
        else
        {
            AIControlled_Movement();
        }
    }

    public void Move(float horizontal, float vertical)
    {
        MoveVehicle(horizontal, vertical, 0f, 0f);
    }

    private void PlayerControlled_Movement()
    {
        // TODO: Implement player input handling
    }

    private void AIControlled_Movement()
    {
        if(!driving)
        {
            // reverse + handbrake until 'driving' flips true
            MoveVehicle(0f, 0f, -1f, 1f);
            return;
        }
        // drive using state-machine inputs
        MoveVehicle(vehicleManager.horizontalInput, vehicleManager.verticalInput, 0f, 0f);
    }

    private void MoveVehicle(float steering, float accelerate, float footBrake, float handbrake)
    {
        var wheels = vehicleManager.Wheels;
        var physics = vehicleManager.PhysicsController;

        ClampInputValues(ref steering, ref accelerate, ref footBrake, ref handbrake);
        HorizontalMovement(steering, wheels);

        physics.EngineDrive(accelerate, footBrake);
        HandleBrake(handbrake, wheels, physics);

        physics.EngineUpdater(wheels, rb, this);
        wheels.VisualizeWheelMovement();
    }

    private void HandleBrake(float handBrake, WheelManager wheel, VehiclePhysicsController physics)
    {
        if (handBrake > 0f)
        {
            float hbTorque = handBrake * physics.MaxHandbrakeTorque;
            wheel.WheelColliders[2].brakeTorque = hbTorque;
            wheel.WheelColliders[3].brakeTorque = hbTorque;
        }
    }

    private void HorizontalMovement(float horizontal, WheelManager wheels)
    {
        vehicleManager.steerAngle = horizontal * maxSteerAngle;
        wheels.WheelColliders[0].steerAngle = vehicleManager.steerAngle;
        wheels.WheelColliders[1].steerAngle = vehicleManager.steerAngle;
    }

    private void ClampInputValues(ref float steering, ref float accelerate, ref float footBrake, ref float handBrake)
    {
        steering = Mathf.Clamp(steering, -1f, 1f);
        accelerate = Mathf.Clamp(accelerate, 0f, 5f);
        handBrake = Mathf.Clamp(handBrake, 0f, 5f);
        footBrake = Mathf.Clamp(-footBrake, 0f, 5f);

        vehicleManager.accelValue = accelerate;
        vehicleManager.brakeValue = footBrake;
    }
}
