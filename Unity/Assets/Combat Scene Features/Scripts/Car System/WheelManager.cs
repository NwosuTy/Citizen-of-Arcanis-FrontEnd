using UnityEngine;
using Unity.VisualScripting;
using System.Collections.Generic;

public class WheelManager : MonoBehaviour
{
    private VehicleManager carManager;

    private float forwardFriction;
    private float sidewardFriction;

    private List<Transform> wheelTransforms = new();
    public List<WheelCollider> WheelColliders { get; private set; } = new();

    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider FL_Wheel;
    [SerializeField] private WheelCollider FR_Wheel;
    [SerializeField] private WheelCollider BL_Wheel;
    [SerializeField] private WheelCollider BR_Wheel;

    [Header("Wheel Transforms")]
    [SerializeField] private Transform FL_Wheel_Transform;
    [SerializeField] private Transform FR_Wheel_Transform;
    [SerializeField] private Transform BL_Wheel_Transform;
    [SerializeField] private Transform BR_Wheel_Transform;

    [Header("Wheel Parameters")]
    [SerializeField] private float wheelRadius = 0.45f;
    [SerializeField] private WheelCurveContainer wheelCurveContainer;
    [SerializeField] private Vector3 wheelPosition = new(0, 0.1f, 0f);

    [Header("Parameters")]
    [SerializeField] private float frictionMultiplier;
    [EvenOrOdd(2, 8)][SerializeField] private int wheelCount;
    [Range(0.8f, 1.7f)][SerializeField] private float friction = 0.8f;
    [Range(0.35f, 1.0f)][SerializeField] private float slipLimit = 0.4f;
    
    private void Awake()
    {
        if (FL_Wheel == null) { GetWheelColliders(); }
        carManager = GetComponentInParent<VehicleManager>();
    }

    private void Start()
    {
        sidewardFriction = forwardFriction = friction;
        WheelColliders = new() { FL_Wheel, FR_Wheel, BL_Wheel, BR_Wheel };
        wheelTransforms = new() { FL_Wheel_Transform, FR_Wheel_Transform, BL_Wheel_Transform, BR_Wheel_Transform };

        SetUpWheels();
    }

    public void WheelManager_Update()
    {
        sidewardFriction = forwardFriction = friction;
        carManager.PhysicsController.SetStiffness(forwardFriction, sidewardFriction);
    }

    public bool IsGrounded()
    {
        return (FL_Wheel.isGrounded && FR_Wheel.isGrounded && BL_Wheel.isGrounded && BR_Wheel.isGrounded);
    }

    public void GetWheelColliders()
    {
        if(FL_Wheel != null)
        {
            DestroyImmediate(FL_Wheel.transform.parent.gameObject);
        }

        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb == null)
        {
            carManager = GetComponentInParent<VehicleManager>();
            carManager.AddComponent<Rigidbody>();
        }
        GameObject o = new();
        GameObject newObj = Instantiate(o, transform);

        DestroyImmediate(o);
        newObj.name = "Wheel Colliders";
        FL_Wheel = CreateWheel(FL_Wheel_Transform, newObj.transform, "Front Left Wheel");
        FR_Wheel = CreateWheel(FR_Wheel_Transform, newObj.transform, "Front Right Wheel");

        BL_Wheel = CreateWheel(BL_Wheel_Transform, newObj.transform, "Back Left Wheel");
        BR_Wheel = CreateWheel(BR_Wheel_Transform, newObj.transform, "Back Right Wheel");
        WheelColliders = new() { FL_Wheel, FR_Wheel, BL_Wheel, BR_Wheel };
    }

    public void VisualizeWheelMovement()
    {
        for(int i = 0; i < wheelCount; i++)
        {
            HandleWheelVisualMovement(WheelColliders[i], wheelTransforms[i]);
        }
    }

    public void SetWheelTransforms(Transform FL, Transform FR, Transform RR, Transform RL)
    {
        FL_Wheel_Transform = FL;
        FR_Wheel_Transform = FR;
        BL_Wheel_Transform = RL;
        BR_Wheel_Transform = RR;
    }

    public void TractionControl(VehiclePhysicsController engine)
    {
        switch (carManager.drive_Type)
        {
            //Loop through all wheels
            case CarDrive_Type.AllWheels:
                for (int i = 0; i < 4; i++)
                {
                    AdjustWheelTorque(WheelColliders[i], engine);
                }
            break;

            case CarDrive_Type.RearWheels:
                AdjustWheelTorque(WheelColliders[2], engine);
                AdjustWheelTorque(WheelColliders[3], engine);
            break;

            case CarDrive_Type.FrontWheels:
                AdjustWheelTorque(WheelColliders[0], engine);
                AdjustWheelTorque(WheelColliders[1], engine);
            break;
        }
    }

    private void AdjustWheelTorque(WheelCollider wc, VehiclePhysicsController engine)
    {
        wc.GetGroundHit(out WheelHit wheelHit);
        AdjustTorque(wheelHit.forwardSlip, engine);
    }

    private void AdjustTorque(float forwardSlip, VehiclePhysicsController engine)
    {
        if (forwardSlip >= slipLimit && engine.currentTorque >= 0)
        {
            engine.currentTorque -= 10 * engine.TractionControl;
        }
        else
        {
            engine.currentTorque += 10 * engine.TractionControl;
            if (engine.currentTorque > engine.FullTorqueOverAllWheels)
            {
                engine.currentTorque = engine.FullTorqueOverAllWheels;
            }
        }
    }

    private void SetUpWheels()
    {
        SetUpWheel(FL_Wheel);
        SetUpWheel(FR_Wheel);
        SetUpWheel(BR_Wheel);
        SetUpWheel(BL_Wheel);
    }

    private void SetUpWheel(WheelCollider wc)
    {
        //Forward
        WheelFrictionCurve curve = wc.forwardFriction;

        curve.extremumSlip = wheelCurveContainer.fwd_extremumSlip;
        curve.asymptoteSlip = wheelCurveContainer.fwd_asymptoteSlip;
        curve.asymptoteValue = wheelCurveContainer.fwd_asymptoteValue;
        CheckVerticalMovement(forwardFriction, curve);
        wc.forwardFriction = curve;

        //Sideways
        curve.extremumSlip = wheelCurveContainer.sid_extremumSlip;
        curve.asymptoteSlip = wheelCurveContainer.sid_asymptoteSlip;
        curve.asymptoteValue = wheelCurveContainer.sid_asymptoteValue;
        CheckVerticalMovement(sidewardFriction, curve);
        wc.sidewaysFriction = curve;
    }

    private WheelCollider CreateWheel(Transform wheel, Transform parent, string objName)
    {
        GameObject newObj = new()
        {
            name = objName
        };
        WheelCollider wheelCollider = newObj.AddComponent<WheelCollider>();
        newObj.transform.SetPositionAndRotation(wheel.position, wheel.rotation);
        newObj.transform.SetParent(parent);

        wheelCollider.radius = wheelRadius;
        wheelCollider.center = wheelPosition;
        return wheelCollider;
    }

    private void CheckVerticalMovement(float frictionDirection, WheelFrictionCurve curve)
    {
        curve.stiffness = (carManager.accelValue <= 0.01f) ? frictionDirection : frictionDirection * frictionMultiplier;
    }

    //Simulate Wheel Movement and Rotation
    private void HandleWheelVisualMovement(WheelCollider wc, Transform t)
    {
        wc.GetWorldPose(out Vector3 pos, out Quaternion rot);
        t.SetPositionAndRotation(pos, rot);
    }

    [System.Serializable]
    public class WheelCurveContainer
    {
        [Header("Forward Curve")]
        [Range(0.75f, 1.3f)] public float fwd_asymptoteSlip = 0.8f;
        [Range(0.8f, 1.5f)] public float fwd_asymptoteValue = 1.0f;
        [Range(0.05f, 0.2f)] public float fwd_extremumSlip = 0.065f;

        [Header("Sideways Curve")]
        [Range(0.75f, 1.3f)] public float sid_asymptoteSlip = 0.8f;
        [Range(0.8f, 1.5f)] public float sid_asymptoteValue = 1.0f;
        [Range(0.05f, 0.2f)] public float sid_extremumSlip = 0.065f;
    }
}
