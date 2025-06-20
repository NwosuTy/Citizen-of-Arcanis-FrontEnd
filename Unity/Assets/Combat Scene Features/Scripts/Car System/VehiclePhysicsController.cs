using UnityEngine;

public class VehiclePhysicsController : MonoBehaviour
{
    private VehicleManager carManager;

    //Parameters
    private float gearFactor;
    private float oldRotation;
    [HideInInspector] public float currentTorque;

    //Properties
    public float Revs { get; private set; }
    public float Wheel_ForwardStiffness {  get; private set; }
    public float Wheel_SidewardStiffness { get; private set; }
    public float MaxSpeed { get { return carManager.Movement.topSpeed; } }

    [Header("Engine Status")]
    [SerializeField] private SpeedType speedType;
    [SerializeField] private static int noOfGears = 5;
    [SerializeField] private float revRangeBoundary = 1f;

    [Header("Engine Parameters")]
    [SerializeField] private int gearNum;
    [SerializeField] private float radius = 4f;
    [Range(0, 1)][SerializeField] private float steerHelper = 0.775f;

    [Header("Engine Physics")]
    [SerializeField] private float downForce = 100f;
    [field: SerializeField] public float MaxHandbrakeTorque { get; private set; } = 1e+08f;
    [field: SerializeField] public float FullTorqueOverAllWheels { get; private set; } = 2000f;
    [field: Range(0, 1)][field: SerializeField] public float TractionControl { get; private set; } = 1f;

    private void Awake()
    {
        carManager = GetComponent<VehicleManager>();
    }

    private void Start()
    {
        MaxHandbrakeTorque = float.MaxValue;
        currentTorque = FullTorqueOverAllWheels - (TractionControl * FullTorqueOverAllWheels);
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
        CapSpeed(carManager.Movement, rb);
    }

    public void EngineUpdater(WheelManager wheel, Rigidbody rigidbody, VehicleMovement move)
    {
        CalculateRevs(move);
        GearChanging(move);

        AddDownForce(rigidbody);
        wheel.TractionControl(this);
        //carManager.carFXManager.CheckForWheelSpin(carManager.carLocomotionManager, carManager.wheelManager.slipLimit);
    }

    private void GearChanging(VehicleMovement move)
    {
        float inverseGear = (1 / (float)noOfGears);
        float engineRPM = Mathf.Abs(move.CurrentSpeed / MaxSpeed);

        float lowerGearLimit = inverseGear * gearNum;
        float upperGearLimit = inverseGear * (gearNum + 1);

        if (gearNum > 0 && engineRPM < lowerGearLimit)
        {
            gearNum--;
        }

        if (engineRPM > upperGearLimit && (gearNum < (noOfGears - 1)))
        {
            gearNum++;
        }
    }

    private void CalculateRevs(VehicleMovement move)
    {
        CalculateGearFactor(move);
        var gearNumFactor = gearNum / (float)noOfGears;
        var revsRangeMax = MathPhysics_Helper.ULerp(revRangeBoundary, 1f, gearNumFactor);
        var revsRangeMin = MathPhysics_Helper.ULerpCurve(0f, revRangeBoundary, gearNumFactor);
        
        Revs = MathPhysics_Helper.ULerp(revsRangeMin, revsRangeMax, gearFactor);
    }

    private void CalculateGearFactor(VehicleMovement move)
    {
        float engineRPM = (1 / (float)noOfGears);

        var targetGearFactor = Mathf.InverseLerp(Multiplier(engineRPM, gearNum),
            Multiplier(engineRPM, (gearNum + 1)), Mathf.Abs(move.CurrentSpeed / MaxSpeed));
        gearFactor = Mathf.Lerp(gearFactor, targetGearFactor, Time.deltaTime * 5f);
    }

    private float Multiplier(float a, float b)
    {
        return a * b;
    }

    private void AddDownForce(Rigidbody rigidbody)
    {
        rigidbody.AddForce(downForce * rigidbody.velocity.magnitude * -transform.up);
    }

    private void CapSpeed(VehicleMovement move, Rigidbody rb)
    {
        float speed = rb.velocity.magnitude;
        switch (speedType)
        {
            case SpeedType.MPH:
                speed *= 2.23693629f;
                if (speed > move.topSpeed)
                {
                    rb.velocity = (move.topSpeed / 2.23693629f) * rb.velocity.normalized;
                }
            break;

            case SpeedType.KPH:
                speed *= 3.6f;
                if (speed > move.topSpeed)
                {
                    rb.velocity = (move.topSpeed / 3.6f) * rb.velocity.normalized;
                }
            break;
        }
    }

    private void SteerHelper(Rigidbody rigidBody, WheelManager wheelManager)
    {
        for (int i = 0; i < 4; i++)
        {
            wheelManager.WheelColliders[i].GetGroundHit(out WheelHit wheelhit);
            if (wheelhit.normal == Vector3.zero)
                continue;
        }

        if (Mathf.Abs(oldRotation - transform.eulerAngles.y) < 10f)
        {
            var turnadjust = (transform.eulerAngles.y - oldRotation) * steerHelper;
            Quaternion velRotation = Quaternion.AngleAxis(turnadjust, Vector3.up);
            rigidBody.velocity = velRotation * rigidBody.velocity;
        }
        oldRotation = transform.eulerAngles.y;
    }

    private void ApplyDrive(float accelerate, float footBrake, VehicleMovement move, WheelManager wheel)
    {
        switch (carManager.drive_Type)
        {
            case CarDrive_Type.AllWheels:
                float thrustTorque = accelerate * (currentTorque / 4f);
                for (int i = 0; i < 4; i++)
                {
                    wheel.WheelColliders[i].motorTorque = thrustTorque;
                }
                break;

            case CarDrive_Type.FrontWheels:
                ApplyWheelDrive(0, 1, accelerate, wheel);
                break;

            case CarDrive_Type.RearWheels:
                ApplyWheelDrive(0, 2, accelerate, wheel);
                break;

        }

        for (int i = 0; i < 4; i++)
        {
            if (move.CurrentSpeed > 5 && Vector3.Angle(transform.forward, carManager.RB.velocity) < 50f)
            {
                wheel.WheelColliders[i].brakeTorque = carManager.Movement.brakeTorque * footBrake;
            }
            else if (footBrake > 0)
            {
                wheel.WheelColliders[i].brakeTorque = 0f;
                wheel.WheelColliders[i].motorTorque = -carManager.Movement.reverseTorque * footBrake;
            }
        }
    }

    private void ApplyWheelDrive(int i, int j, float accelerate, WheelManager wheel)
    {
        float thrustTorque = accelerate * (currentTorque / 2f);
        wheel.WheelColliders[i].motorTorque = wheel.WheelColliders[j].motorTorque = thrustTorque;
    }
}
