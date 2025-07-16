using UnityEngine;

[CreateAssetMenu(menuName = "DrivingStates/OvertakeState")]
public class OvertakeState : DrivingStates
{
    private float timer;
    private float duration = 1.5f;

    public override DrivingStates HandleAction(VehicleManager vm)
    {
        GetDirectorAndMovement(vm, out var dir, out var move);
        timer += Time.deltaTime;

        dir.UpdateOvertakeTarget();

        float aggression = dir.BehaviorAggression(vm.drivingBehavior);
        ThrottleToSpeed(vm, dir.DesiredSpeed * aggression, dir.AccelSensitivity);
        SteerToTarget(vm, dir.transform.InverseTransformPoint(dir.Target.position));

        move.Move(vm.horizontalInput, vm.verticalInput);

        if (timer > duration)
            return SwitchState(dir.normal, vm);
        return this;
    }

    protected override void ResetStateParameters(VehicleManager vm)
    {
        timer = 0f;
    }
}
