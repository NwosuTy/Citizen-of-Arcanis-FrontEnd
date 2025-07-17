using UnityEngine;

public abstract class DrivingStates : ScriptableObject
{
    protected float stuckThreshold = 0.5f;
    protected float stuckTimeLimit = 2.0f;

    protected void GetDirectorAndMovement(VehicleManager vm, out AIVehicleDirector dir, out VehicleMovement move)
    {
        dir = vm.AIController;
        move = vm.Movement;
    }

    protected void SteerToTarget(VehicleManager vm, Vector3 localTarget)
    {
        GetDirectorAndMovement(vm, out var dir, out _);
        float angle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        vm.horizontalInput = Mathf.Clamp(angle * dir.SteerSensitivity, -1f, 1f);
    }

    protected void ThrottleToSpeed(VehicleManager vm, float desired, float sens)
    {
        GetDirectorAndMovement(vm, out var dir, out var move);
        float accel = Mathf.Clamp((desired - move.CurrentSpeed) * sens, -1f, 1f);
        float noise = Mathf.PerlinNoise(Time.time * dir.AccelWanderSpeed, dir.RandomPerlin);
        vm.verticalInput = accel * (1 - dir.AccelWanderAmount) + noise * dir.AccelWanderAmount;
    }

    protected void ReverseThrottle(VehicleManager vm, float reverseSpd)
    {
        GetDirectorAndMovement(vm, out var dir, out var move);
        vm.verticalInput = Mathf.Clamp((reverseSpd - move.CurrentSpeed) * dir.AccelSensitivity, -1f, -0.35f);
    }

    public abstract DrivingStates HandleAction(VehicleManager vm);

    protected virtual void ResetStateParameters(VehicleManager vm)
    {

    }

    public DrivingStates SwitchState(DrivingStates next, VehicleManager vm)
    {
        ResetStateParameters(vm);
        return next;
    }
}
