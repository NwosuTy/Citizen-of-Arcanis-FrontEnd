using System.Collections;
using UnityEngine;

/// <summary>
/// Central movement/physics applier. Reads inputs written by states or player and applies them once per FixedUpdate.
/// </summary>
[RequireComponent(typeof(VehicleManager))]
public class VehicleMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Coroutine kickStartRoutine;
    private VehicleManager vehicleManager;

    private float stopDistance;
    public float StopDistance => stopDistance;
    public float GetLinearSpeed => rb != null ? rb.velocity.magnitude : 0f;
    // CurrentSpeed used in states expects units consistent with your PhysicsController (original code used mph conversion)
    public float CurrentSpeed => rb != null ? rb.velocity.magnitude * 2.23693629f : 0f; // mph

    [field: Header("Default Stats")]
    [field: SerializeField] public float DefaultSpeed { get; private set; } = 200f;

    [Header("Parameters")]
    public float topSpeed;
    [SerializeField] private float maxSteerAngle = 25f;
    [Tooltip("Starting Torque Value When Drive Just Commences")]
    [SerializeField] private float kickStartTargetTorque = 50f;

    [Header("Traffic approach tuning")]
    [SerializeField] private float slowStartDistance = 30f;
    [SerializeField][Range(0f, 1f)] private float minApproachSpeedFactor = 0.25f;

    [field: Header("Brake and Reverse Parameters")]
    [field: SerializeField] public float BrakeTorque { get; private set; } = 20000f;
    [field: SerializeField] public float ReverseTorque { get; private set; } = 150f;

    [field: Header("Destination And Cautious Parameters")]
    [field: SerializeField] public float CautiousMaxDistance { get; private set; } = 100f;
    [field: SerializeField] public float CautiousAngularVelocityFactor { get; private set; } = 30f;
    [field: SerializeField][field: Range(0f, 180f)] public float CautiousMaxAngle { get; private set; } = 50f;
    [field: SerializeField][field: Range(0f, 1f)] public float CautiousSpeedFactor { get; private set; } = 0.05f;

    [Header("Input smoothing")]
    [SerializeField][Range(0.0f, 0.3f)] private float inputSmoothing = 0.06f; // smaller -> snappier
    private float appliedSteer;
    private float steerVel;

    [Header("Status")]
    public bool canDrive;
    [ReadOnlyInspector] public bool driving;
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
                StopCoroutine(kickStartRoutine);
            kickStartRoutine = StartCoroutine(KickStartVehicleRoutine());
        }
        driving = status;
    }

    public void SetStopDistance(float stopDistance)
    {
        this.stopDistance = stopDistance;
    }

    private IEnumerator KickStartVehicleRoutine()
    {
        if (rb != null)
        {
            rb.velocity = transform.forward * 1.5f;
        }

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
            wheels.ForEach(wc => wc.motorTorque = currentTorque);
            elapsed += Time.deltaTime;
            yield return null;
        }
        wheels.ForEach(wc => wc.motorTorque = kickStartTargetTorque);
    }

    /// <summary>
    /// Called from VehicleManager.FixedUpdate: ensure state has a chance to write inputs before applying them.
    /// </summary>
    public void HandleMovement()
    {
        SetDriving(canDrive);

        // If this vehicle is player controlled, use player input path
        if (vehicleManager.playerControlled)
        {
            PlayerControlled_Movement();
            return;
        }

        // If AI controlled: let the director/state machine run first so it sets inputs,
        // then apply the movement using those inputs.
        if (vehicleManager.AIController != null)
        {
            vehicleManager.AIController.StateChangeAndCheckReservations();
        }
        AIControlled_Movement();
    }

    public void Move(float horizontal, float vertical)
    {
        MoveVehicle(horizontal, vertical, 0f, 0f);
    }

    private void PlayerControlled_Movement()
    {
        // Hook this to your player input system.
        // Example: MoveVehicle(playerSteer, playerThrottle, playerBrake, playerHandbrake);
    }

    private void AIControlled_Movement()
    {
        if (!driving)
        {
            // when not allowed to drive, hold brakes / handbrake
            MoveVehicle(0f, 0f, 1f, 1f);
            return;
        }

        // Read inputs written by states
        float steer = vehicleManager.horizontalInput;
        float throttle = vehicleManager.verticalInput; // forward throttle 0..1
        float footBrake = vehicleManager.brakeInput;   // brake/reverse 0..1
        float handbrake = 0f;

        MoveVehicle(steer, throttle, footBrake, handbrake);
    }

    private void MoveVehicle(float steering, float accelerate, float footBrake, float handbrake)
    {
        var wheels = vehicleManager.Wheels;
        var physics = vehicleManager.PhysicsController;

        ClampInputValues(ref steering, ref accelerate, ref footBrake, ref handbrake);
        // steering smoothing applied once here
        appliedSteer = Mathf.SmoothDamp(appliedSteer, steering, ref steerVel, inputSmoothing);

        HorizontalMovement(appliedSteer, wheels);

        // feed to physics controller:
        physics.EngineDrive(accelerate, footBrake);
        HandleBrake(handbrake, wheels, physics);

        physics.EngineUpdater(wheels, rb, this);
        wheels.VisualizeWheelMovement();
    }

    private void HandleBrake(float handBrake, WheelManager wheel, VehiclePhysicsController physics)
    {
        if (handBrake > 0f && wheel != null && physics != null)
        {
            float hbTorque = handBrake * physics.MaxHandbrakeTorque;
            // rear wheels typical
            if (wheel.WheelColliders.Count >= 4)
            {
                wheel.WheelColliders[2].brakeTorque = hbTorque;
                wheel.WheelColliders[3].brakeTorque = hbTorque;
            }
        }
    }

    private void HorizontalMovement(float horizontal, WheelManager wheels)
    {
        vehicleManager.steerAngle = horizontal * maxSteerAngle;
        if (wheels == null) return;
        var colliders = wheels.WheelColliders;
        if (colliders.Count >= 2)
        {
            colliders[0].steerAngle = vehicleManager.steerAngle;
            colliders[1].steerAngle = vehicleManager.steerAngle;
        }
    }

    /// <summary>
    /// Standardized input clamps:
    /// steering: -1..1
    /// accelerate: 0..1 (forward throttle)
    /// footBrake: 0..1 (brake/reverse magnitude)
    /// handBrake: 0..1
    /// Also updates VehicleManager.accelValue/brakeValue for wheel friction logic.
    /// </summary>
    private void ClampInputValues(ref float steering, ref float accelerate, ref float footBrake, ref float handBrake)
    {
        steering = Mathf.Clamp(steering, -1f, 1f);
        accelerate = Mathf.Clamp01(accelerate);   // forward throttle (0..1)
        handBrake = Mathf.Clamp01(handBrake);
        footBrake = Mathf.Clamp01(footBrake);     // brake/reverse (0..1)

        // these two are used by wheel friction logic elsewhere
        vehicleManager.accelValue = accelerate;
        vehicleManager.brakeValue = footBrake;
    }

    /// <summary>
    /// Return a desired speed adjusted for remaining path distance for Traffic behavior.
    /// </summary>
    public float GetAdaptiveDesiredSpeed(float baseSpeed, float progressDistance, WayPointPath assignedPath)
    {
        float remaining;
        if (assignedPath != null && assignedPath.HasPath)
        {
            remaining = Mathf.Max(0f, assignedPath.TotalLength - progressDistance);
            if (remaining <= 0f)
                remaining = assignedPath.DistanceToFinalDestination(transform.position);
        }
        else
        {
            remaining = assignedPath != null ? assignedPath.DistanceToFinalDestination(transform.position) : 0f;
        }
        // Delegate interpolation + stop logic to the Movement layer
        return AdjustSpeedForRemainingDistance(baseSpeed, remaining);
    }

    private float AdjustSpeedForRemainingDistance(float baseSpeed, float remainingDistance)
    {
        if (baseSpeed <= 0f) return 0f;

        stopDistance = Mathf.Max(0f, stopDistance);
        slowStartDistance = Mathf.Max(0.0001f, slowStartDistance);
        minApproachSpeedFactor = Mathf.Clamp01(minApproachSpeedFactor);

        if (remainingDistance <= stopDistance) return 0f;
        if (remainingDistance >= slowStartDistance) return baseSpeed;

        float minSpeed = baseSpeed * minApproachSpeedFactor;
        float t = remainingDistance / slowStartDistance;
        t = Mathf.Clamp01(t);
        return Mathf.Lerp(minSpeed, baseSpeed, t);
    }
}
