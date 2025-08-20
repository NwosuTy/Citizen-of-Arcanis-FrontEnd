using UnityEngine;

/// <summary>
/// Handles engine forces, gear logic and speed capping.
/// </summary>
[RequireComponent(typeof(VehicleManager))]
public class VehiclePhysicsController : MonoBehaviour
{
    private VehicleManager carManager;

    private float gearFactor;
    private float oldRotation;
    private static int noOfGears = 5;
    [HideInInspector] public float currentTorque;

    public float Revs { get; private set; }
    public float Wheel_ForwardStiffness { get; private set; }
    public float Wheel_SidewardStiffness { get; private set; }
    public float MaxSpeed => carManager.Movement.topSpeed;

    [Header("Engine Status")]
    [SerializeField] private SpeedType speedType = SpeedType.MPH;
    [SerializeField] private float revRangeBoundary = 1f;

    [Header("Engine Parameters")]
    [SerializeField] private int gearNum;
    [SerializeField] private float driveMultiplier = 1.5f;
    [Range(0, 1)][SerializeField] private float steerHelper = 0.775f;

    [Header("Engine Physics")]
    [SerializeField] private float downForce = 100f;
    [field: SerializeField] public float MaxHandbrakeTorque { get; private set; } = 1e+08f;
    [field: SerializeField] public float FullTorqueOverAllWheels { get; private set; } = 2000f;
    [field: Range(0, 1)][field: SerializeField] public float TractionControl { get; private set; } = 0.3f;

    [Header("Gear Ratios")]
    [SerializeField] private float finalDrive = 3.8f;
    [SerializeField] private float wheelRadius = 0.34f;
    [SerializeField] private float[] gearRatios = { 3.5f, 2.2f, 1.5f, 1.0f, 0.8f };

    private void Awake()
    {
        carManager = GetComponent<VehicleManager>();
    }

    private void Start()
    {
        MaxHandbrakeTorque = float.MaxValue;
        currentTorque = FullTorqueOverAllWheels;
    }

    public void SetStiffness(float fwd, float sid)
    {
        Wheel_ForwardStiffness = fwd;
        Wheel_SidewardStiffness = sid;
    }

    public void EngineDrive(float accelerate, float footBrake)
    {
        Rigidbody rb = carManager.RB;
        WheelManager wm = carManager.Wheels;
        VehicleMovement move = carManager.Movement;

        SteerHelper(rb, wm);
        ApplyDrive(accelerate, footBrake, move, wm);
        CapSpeed(move, rb);
    }

    public void EngineUpdater(WheelManager wheel, Rigidbody rb, VehicleMovement move)
    {
        CalculateRevs(move);
        GearChanging(move);
        AddDownForce(rb);
        wheel.TractionControl(this);
    }

    private void GearChanging(VehicleMovement move)
    {
        float inverseGear = 1f / noOfGears;
        float engineRPM = Mathf.Abs(move.CurrentSpeed / MaxSpeed);

        float lowerLimit = inverseGear * gearNum;
        float upperLimit = inverseGear * (gearNum + 1);

        if (gearNum > 0 && engineRPM < lowerLimit)
            gearNum--;
        if (engineRPM > upperLimit && gearNum < (noOfGears - 1))
            gearNum++;
    }

    private void CalculateRevs(VehicleMovement move)
    {
        CalculateGearFactor(move);
        float gearNumFactor = gearNum / (float)noOfGears;

        float revsMax = MathPhysics_Helper.ULerp(revRangeBoundary, 1f, gearNumFactor);
        float revsMin = MathPhysics_Helper.ULerpCurve(0f, revRangeBoundary, gearNumFactor);

        Revs = MathPhysics_Helper.ULerp(revsMin, revsMax, gearFactor);
    }

    private void CalculateGearFactor(VehicleMovement move)
    {
        float engineRPM = 1f / noOfGears;
        float targetFactor = Mathf.InverseLerp(
            Multiplier(engineRPM, gearNum),
            Multiplier(engineRPM, gearNum + 1),
            Mathf.Abs(move.CurrentSpeed / MaxSpeed));

        gearFactor = Mathf.Lerp(gearFactor, targetFactor, Time.deltaTime * 5f);
    }

    private float Multiplier(float a, float b) => a * b;

    private void AddDownForce(Rigidbody rb)
    {
        if (rb == null) return;
        rb.AddForce(downForce * rb.velocity.magnitude * -transform.up);
    }

    private void CapSpeed(VehicleMovement move, Rigidbody rb)
    {
        if (rb == null || move == null) return;

        float speed = rb.velocity.magnitude;
        switch (speedType)
        {
            case SpeedType.KPH:
                speed *= 3.6f;
                if (speed > move.topSpeed)
                    rb.velocity = (move.topSpeed / 3.6f) * rb.velocity.normalized;
                break;
            case SpeedType.MPH:
                speed *= 2.23693629f;
                if (speed > move.topSpeed)
                    rb.velocity = (move.topSpeed / 2.23693629f) * rb.velocity.normalized;
                break;
        }
    }

    private void SteerHelper(Rigidbody rigidBody, WheelManager wheelManager)
    {
        if (rigidBody == null || wheelManager == null) return;

        for (int i = 0; i < Mathf.Min(4, wheelManager.WheelColliders.Count); i++)
        {
            wheelManager.WheelColliders[i].GetGroundHit(out WheelHit wheelHit);
            if (wheelHit.normal == Vector3.zero) continue;
        }

        if (Mathf.Abs(oldRotation - transform.eulerAngles.y) < 10f)
        {
            float turnAdjust = (transform.eulerAngles.y - oldRotation) * steerHelper;
            Quaternion velRot = Quaternion.AngleAxis(turnAdjust, Vector3.up);
            rigidBody.velocity = velRot * rigidBody.velocity;
        }
        oldRotation = transform.eulerAngles.y;
    }

    private void ApplyDrive(float accelerate, float footBrake, VehicleMovement move, WheelManager wheel)
    {
        int wheelCount = carManager.WheelCountInt;
        float gearRatio = gearRatios[Mathf.Clamp(gearNum, 0, gearRatios.Length - 1)];
        float totalTorque = currentTorque * gearRatio * finalDrive;

        int driveWheels = carManager.drive_Type == CarDrive_Type.AllWheels ? wheelCount : 2;
        float torquePerWheel = Mathf.Max((totalTorque / driveWheels) * accelerate, 50f);

        var colliders = wheel.WheelColliders;

        // Apply torque
        for (int i = 0; i < wheelCount; i++)
        {
            bool shouldDrive = (carManager.drive_Type == CarDrive_Type.AllWheels) ||
                               (carManager.drive_Type == CarDrive_Type.FrontWheels && i < 2) ||
                               (carManager.drive_Type == CarDrive_Type.RearWheels && i >= 2);

            if (shouldDrive)
                colliders[i].motorTorque = torquePerWheel;
        }

        // Braking / reverse logic
        for (int i = 0; i < wheelCount; i++)
        {
            if (move.CurrentSpeed > 5 && Vector3.Angle(transform.forward, carManager.RB.velocity) < 50f)
            {
                colliders[i].brakeTorque = move.BrakeTorque * footBrake;
            }
            else if (footBrake > 0)
            {
                colliders[i].brakeTorque = 0f;
                colliders[i].motorTorque = -move.ReverseTorque * footBrake;
            }
        }
    }
}
