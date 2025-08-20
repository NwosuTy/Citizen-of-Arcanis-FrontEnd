using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages wheel colliders and visual transforms. Keeps runtime behavior allocation-free.
/// Editor-only helper should populate colliders if missing.
/// </summary>
public class WheelManager : MonoBehaviour
{
    private VehicleManager carManager;

    private float forwardFriction;
    private float sidewardFriction;

    private List<Transform> wheelTransforms = new();
    public List<WheelCollider> WheelColliders { get; private set; } = new();

    [field: Header("Wheel Colliders (assign in editor)")]
    [field: SerializeField] public WheelCollider FL_Wheel { get; private set; }
    [field: SerializeField] public WheelCollider FR_Wheel { get; private set; }
    [field: SerializeField] public WheelCollider BL_Wheel { get; private set; }
    [field: SerializeField] public WheelCollider BR_Wheel { get; private set; }

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
    [Range(0.8f, 1.7f)][SerializeField] private float friction = 0.8f;
    [Range(0.35f, 1.0f)][SerializeField] private float slipLimit = 0.4f;

    private void Awake()
    {
        carManager = GetComponentInParent<VehicleManager>();
    }

    private void Start()
    {
        // ensure colliders list references are stable
        WheelColliders = new() { FL_Wheel, FR_Wheel, BL_Wheel, BR_Wheel };
        wheelTransforms = new() { FL_Wheel_Transform, FR_Wheel_Transform, BL_Wheel_Transform, BR_Wheel_Transform };

        sidewardFriction = forwardFriction = friction;
        SetUpWheels();
    }

    public void WheelManager_Update()
    {
        sidewardFriction = forwardFriction = friction;
        carManager.PhysicsController?.SetStiffness(forwardFriction, sidewardFriction);
    }

    public bool IsGrounded()
    {
        if (WheelColliders == null || WheelColliders.Count < 4) return false;
        return (WheelColliders[0].isGrounded && WheelColliders[1].isGrounded &&
                WheelColliders[2].isGrounded && WheelColliders[3].isGrounded);
    }

    public void VisualizeWheelMovement()
    {
        int wheelCount = carManager.WheelCountInt;
        for (int i = 0; i < Mathf.Min(wheelCount, WheelColliders.Count); i++)
        {
            HandleWheelVisualMovement(WheelColliders[i], wheelTransforms.Count > i ? wheelTransforms[i] : null);
        }
    }

    /// <summary>
    /// Editor helper to create WheelCollider objects under a 'Wheel Colliders' gameobject.
    /// This is safe to call in the editor. If colliders already exist, their parent will be destroyed and recreated.
    /// </summary>
    public void GetWheelColliders()
    {
        #if UNITY_EDITOR
        if (FL_Wheel != null)
        {
            var parent = FL_Wheel.transform.parent;
            if (parent != null && parent.name == "Wheel Colliders")
                DestroyImmediate(parent.gameObject);
        }
        #endif

        GameObject container = new("Wheel Colliders");
        container.transform.SetParent(this.transform, false);

        WheelCollider CreateCollider(Transform wheelTransform, string name)
        {
            GameObject go = new(name);
            go.transform.SetParent(container.transform, false);
            if (wheelTransform != null)
            {
                go.transform.SetPositionAndRotation(wheelTransform.position, wheelTransform.rotation);
            }
            var wc = go.AddComponent<WheelCollider>();
            wc.radius = wheelRadius; // uses the existing field wheelRadius from this class
            wc.center = wheelPosition;
            return wc;
        }

        FL_Wheel = CreateCollider(FL_Wheel_Transform, "Front Left Wheel");
        FR_Wheel = CreateCollider(FR_Wheel_Transform, "Front Right Wheel");
        BL_Wheel = CreateCollider(BL_Wheel_Transform, "Back Left Wheel");
        BR_Wheel = CreateCollider(BR_Wheel_Transform, "Back Right Wheel");
        WheelColliders = new List<WheelCollider> { FL_Wheel, FR_Wheel, BL_Wheel, BR_Wheel };
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
            case CarDrive_Type.AllWheels:
                for (int i = 0; i < WheelColliders.Count; i++)
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
        if (wc == null || engine == null) return;
        wc.GetGroundHit(out WheelHit wheelHit);
        AdjustTorque(wheelHit.forwardSlip, engine);
    }

    private void AdjustTorque(float forwardSlip, VehiclePhysicsController engine)
    {
        if (forwardSlip >= slipLimit)
        {
            float slipFactor = Mathf.Clamp01(forwardSlip / slipLimit);
            engine.currentTorque = engine.FullTorqueOverAllWheels * (1f - engine.TractionControl * slipFactor);
        }
        else
        {
            engine.currentTorque = engine.FullTorqueOverAllWheels;
        }
    }

    private void SetUpWheels()
    {
        foreach (var wc in WheelColliders)
            if (wc != null) SetUpWheel(wc);
    }

    private void SetUpWheel(WheelCollider wc)
    {
        var curve = wc.forwardFriction;
        curve.extremumSlip = wheelCurveContainer.fwd_extremumSlip;
        curve.asymptoteSlip = wheelCurveContainer.fwd_asymptoteSlip;
        curve.asymptoteValue = wheelCurveContainer.fwd_asymptoteValue;
        CheckVerticalMovement(forwardFriction, curve);
        wc.forwardFriction = curve;

        curve = wc.sidewaysFriction;
        curve.extremumSlip = wheelCurveContainer.sid_extremumSlip;
        curve.asymptoteSlip = wheelCurveContainer.sid_asymptoteSlip;
        curve.asymptoteValue = wheelCurveContainer.sid_asymptoteValue;
        CheckVerticalMovement(sidewardFriction, curve);
        wc.sidewaysFriction = curve;

        wc.radius = wheelRadius;
        wc.center = wheelPosition;
    }

    private void CheckVerticalMovement(float frictionDirection, WheelFrictionCurve curve)
    {
        curve.stiffness = (carManager.accelValue <= 0.01f) ? frictionDirection : frictionDirection * frictionMultiplier;
    }

    private void HandleWheelVisualMovement(WheelCollider wc, Transform t)
    {
        if (wc == null || t == null) return;
        wc.GetWorldPose(out Vector3 pos, out Quaternion rot);
        t.SetPositionAndRotation(pos, rot);
    }

    [System.Serializable]
    public class WheelCurveContainer
    {
        [Header("Forward Curve")]
        [Range(0.25f, 1f)] public float fwd_asymptoteSlip = 0.8f;
        [Range(0.25f, 1.25f)] public float fwd_asymptoteValue = 1.0f;
        [Range(0.05f, 1f)] public float fwd_extremumSlip = 0.065f;

        [Header("Sideways Curve")]
        [Range(0.25f, 1f)] public float sid_asymptoteSlip = 0.8f;
        [Range(0.25f, 1.25f)] public float sid_asymptoteValue = 1.0f;
        [Range(0.05f, 1f)] public float sid_extremumSlip = 0.065f;
    }
}
