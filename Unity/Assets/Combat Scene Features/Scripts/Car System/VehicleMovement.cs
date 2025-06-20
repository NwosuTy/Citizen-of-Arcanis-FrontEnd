using UnityEngine;

public class VehicleMovement : MonoBehaviour
{
    private Rigidbody rb;
    private VehicleManager vehicleManager;

    [field: Header("Default Stats")]
    [field: SerializeField] public float DefaultSpeed { get; private set; } = 200f;

    [Header("Parameters")]
    public float topSpeed;
    [SerializeField] private float maxSteerAngle = 25f;
    [field: SerializeField] public float brakeTorque { get; private set; } = 20000f;
    [field: SerializeField] public float reverseTorque { get; private set; } = 150f;
    public float CurrentSpeed { get { return rb.velocity.magnitude * 2.23693629f; } }

    [Header("Destination And Cautious Parameters")]
    [Tooltip("distance at which distance-based cautiousness begins")]
    [SerializeField] private float cautiousMaxDistance = 100f;
    [Tooltip("how cautious the AI should be when considering its own current angular velocity (i.e. easing off acceleration if spinning!)")]
    [SerializeField] private float cautiousAngularVelocityFactor = 30f;
    [Tooltip("angle of approaching corner to treat as warranting maximum caution")]
    [SerializeField][Range(0, 180)] private float cautiousMaxAngle = 50f;
    [Tooltip("percentage of max speed to use when being maximally cautious")]
    [SerializeField][Range(0, 1)] private float cautiousSpeedFactor = 0.05f;

    [Header("Status")]
    public bool driving;
    [SerializeField] private float seconds;
    [SerializeField] private BrakeCondition brakeCondition = BrakeCondition.TargetDistance;

    private void Awake()
    {
        vehicleManager = GetComponent<VehicleManager>();
    }

    private void Start()
    {
        rb = vehicleManager.RB;
        topSpeed = DefaultSpeed;
    }

    public void HandleMovement(Transform target)
    {
        if(vehicleManager.playerControlled)
        {
            PlayerControlled_Movement();
            return;
        }
        AIControlled_Movement(target);
    }

    public void Move(float horizontal, float vertical)
    {
        MoveVehicle(horizontal, vertical, 0f, 0f);
    }

    private void PlayerControlled_Movement()
    {

    }

    private void AIControlled_Movement(Transform target)
    {
        if (!driving)
        {
            MoveVehicle(0, 0, -1f, 1f);
            return;
        }
        VehiclePhysicsController physics = vehicleManager.PhysicsController;

        Vector3 fwd = transform.forward;
        float maxSpeed = physics.MaxSpeed;
        float desiredSpeed = maxSpeed;

        if (rb.velocity.magnitude > maxSpeed * 0.1f)
        {
            fwd = rb.velocity;
        }

        switch (brakeCondition)
        {
            case BrakeCondition.TargetDirectionDifference:
                {
                    float approachingCornerAngle = Vector3.Angle(target.forward, fwd);
                    float spinningAngle = rb.angularVelocity.magnitude * cautiousAngularVelocityFactor;
                    float cautiousnessRequired = Mathf.InverseLerp(0, cautiousMaxAngle, Mathf.Max(spinningAngle, approachingCornerAngle));

                    desiredSpeed = Mathf.Lerp(maxSpeed, maxSpeed * cautiousSpeedFactor, cautiousnessRequired);
                    break;
                }

            case BrakeCondition.TargetDistance:
                {
                    float distance = Vector3.Distance(target.position, transform.position);
                    float distanceCautiousFactor = Mathf.InverseLerp(cautiousMaxDistance, 0, distance);

                    float spinningAngle = rb.angularVelocity.magnitude * cautiousAngularVelocityFactor;

                    float cautiousnessRequired = Mathf.Max(
                        Mathf.InverseLerp(0, cautiousMaxAngle, spinningAngle), distanceCautiousFactor);
                    desiredSpeed = Mathf.Lerp(maxSpeed, maxSpeed * cautiousSpeedFactor,
                                                cautiousnessRequired);
                    break;
                }
            case BrakeCondition.NeverBrake:
                break;
        }
        vehicleManager.AIController.ControlInput(desiredSpeed, this);
    }

    private void MoveVehicle(float steering, float accelerate, float footBrake, float handbrake)
    {
        WheelManager wheel = vehicleManager.Wheels;
        VehiclePhysicsController physics = vehicleManager.PhysicsController;

        ClampInputValues(steering, accelerate, footBrake, handbrake);
        HorizontalMovement(steering, wheel);

        physics.EngineDrive(accelerate, footBrake);
        HandleBrake(handbrake, wheel, physics);

        physics.EngineUpdater(wheel, rb, this);
        wheel.VisualizeWheelMovement();
    }

    private void HandleBrake(float handBrake, WheelManager wheel, VehiclePhysicsController physics)
    {
        if (handBrake > 0f)
        {
            var hbTorque = handBrake * physics.MaxHandbrakeTorque;
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

    private void ClampInputValues(float acceleration, float footBrake, float handBrake, float steering)
    {
        steering = Mathf.Clamp(steering, -1.0f, 5.0f);
        vehicleManager.accelValue = Mathf.Clamp(acceleration, 0.0f, 5.0f);

        handBrake = Mathf.Clamp(handBrake, 0.0f, 5.0f);
        vehicleManager.brakeValue = -1.0f * Mathf.Clamp(footBrake, -5.0f, 0.0f);
    }
}
