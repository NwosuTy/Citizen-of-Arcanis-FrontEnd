using UnityEngine;

/// <summary>
/// Base state for driving behaviours. States only set intent (horizontalInput, verticalInput, brakeInput).
/// </summary>
public abstract class DrivingStates : ScriptableObject
{
    // tuning for steering feedforward/derivative smoothing (per-state, can be tweaked in inspector if needed)
    [Header("Input Smoothing (state-level)")]
    [SerializeField] protected float steerFeedforwardWeight = 0.6f;
    [SerializeField] protected float steerDerivativeScale = 0.0025f;
    [SerializeField] protected float steerSmoothingTime = 0.07f; // small -> snappy
    [SerializeField] protected float largeAngleThresholdDeg = 20f;

    // small throttle feedforward tuning
    [SerializeField] protected float throttleFeedforwardWeight = 0.4f;
    [SerializeField] protected float accelWanderAmount = 0.06f;

    // internal state for smoothing per-scriptable-instance (each director instantiates these)
    private float steerVel;
    private float lastDesiredSteer = 0f;

    protected float stuckThreshold = 0.5f;
    protected float stuckTimeLimit = 2.0f;

    public virtual void OnEnter(VehicleManager vm) { }
    public virtual void OnExit(VehicleManager vm) { }

    protected void GetDirectorAndMovement(VehicleManager vm, out AIVehicleDirector dir, out VehicleMovement move)
    {
        dir = vm.AIController;
        move = vm.Movement;
    }

    /// <summary>
    /// Compute a desired steering (-1..1) and write vm.horizontalInput using feedforward + derivative + light smoothing.
    /// localTarget is in director-local space (use dir.transform.InverseTransformPoint(...)).
    /// Set reverse=true when reversing (inverts steering behavior so look-behind steering works naturally).
    /// </summary>
    protected void SteerToTarget(VehicleManager vm, Vector3 localTarget, bool reverse = false)
    {
        GetDirectorAndMovement(vm, out var dir, out var move);
        if (dir == null) return;

        float angle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        float baseSteer = Mathf.Clamp(angle * dir.SteerSensitivity, -1f, 1f);

        // feedforward component (immediate)
        float feedforward = baseSteer * steerFeedforwardWeight;

        // derivative term based on change in desired steer
        float deriv = (baseSteer - lastDesiredSteer) / Mathf.Max(0.0001f, Time.deltaTime);
        float derivTerm = Mathf.Clamp(deriv * steerDerivativeScale * dir.SteerSensitivity, -0.3f, 0.3f);

        // candidate target steer
        float targetSteer = feedforward + derivTerm;

        // if reversing invert steering direction (realistic reversed control)
        if (reverse) targetSteer *= -1f;

        // large-angle quick reaction: temporarily reduce smoothing and bias towards baseSteer
        float smoothing = Mathf.Clamp(steerSmoothingTime, 0.01f, 0.5f);
        if (Mathf.Abs(angle) > largeAngleThresholdDeg)
        {
            smoothing = Mathf.Max(0.01f, smoothing * 0.25f);
            targetSteer = Mathf.Lerp(targetSteer, baseSteer, 0.7f);
        }

        // SmoothDamp toward target from current vm.horizontalInput
        float smooth = Mathf.SmoothDamp(vm.horizontalInput, targetSteer, ref steerVel, smoothing);
        // mix immediate + smooth to keep snappy but damp jitter
        vm.horizontalInput = Mathf.Lerp(targetSteer, smooth, 0.25f);

        lastDesiredSteer = baseSteer;
    }

    /// <summary>
    /// Set throttle intent. vm.verticalInput = forward throttle [0..1], vm.brakeInput = brake/reverse [0..1].
    /// Desired is in the same units as VehicleMovement.topSpeed (e.g. mph/kph depending on your setup).
    /// sens is director.AccelSensitivity.
    /// </summary>
    protected void ThrottleToSpeed(VehicleManager vm, float desired, float sens)
    {
        GetDirectorAndMovement(vm, out var dir, out var move);
        if (dir == null || move == null) return;

        // proportional term. positive -> need forward throttle; negative -> need braking/reverse
        float p = Mathf.Clamp((desired - move.CurrentSpeed) * sens, -1f, 1f);

        // small feedforward helps responsiveness for acceleration
        float ff = 0f;
        if (move.CurrentSpeed >= 0.01f)
            ff = Mathf.Clamp((desired / Mathf.Max(1f, move.CurrentSpeed)) - 1f, -0.5f, 0.5f);

        float noise = Mathf.PerlinNoise(Time.time * dir.AccelWanderSpeed + dir.RandomPerlin, 0f);
        // bias noise around zero (-0.5..0.5)
        float wander = (noise * 2f - 1f) * accelWanderAmount;

        if (p >= 0f)
        {
            vm.verticalInput = Mathf.Clamp01(p + ff * throttleFeedforwardWeight + wander * (1f - dir.AccelWanderAmount));
            vm.brakeInput = 0f;
        }
        else
        {
            // braking / reverse intent stored in brakeInput (positive)
            vm.verticalInput = 0f;
            vm.brakeInput = Mathf.Clamp01(-p); // -p is positive
        }
    }

    // convenience to clear inputs when switching states
    protected void ClearInputs(VehicleManager vm)
    {
        if (vm == null) return;
        vm.horizontalInput = 0f;
        vm.verticalInput = 0f;
        vm.brakeInput = 0f;
    }

    public abstract DrivingStates HandleAction(VehicleManager vm);

    protected virtual void ResetStateParameters(VehicleManager vm) { }

    public DrivingStates SwitchState(DrivingStates next, VehicleManager vm)
    {
        OnExit(vm);
        next.ResetStateParameters(vm);
        next.OnEnter(vm);
        return next;
    }
}
