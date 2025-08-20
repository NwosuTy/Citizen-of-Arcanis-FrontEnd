using UnityEngine;

[CreateAssetMenu(menuName = "DrivingStates/OvertakeState")]
public class OvertakeState : DrivingStates
{
    private float timer;
    [SerializeField] private float duration = 1.5f;

    public override DrivingStates HandleAction(VehicleManager vm)
    {
        GetDirectorAndMovement(vm, out var dir, out var move);
        if (dir == null || move == null) return this;

        timer += Time.deltaTime;

        dir.UpdateOvertakeTarget();

        float aggression = dir.BehaviorAggression(vm.drivingBehavior);
        ThrottleToSpeed(vm, vm.Movement.topSpeed * aggression, dir.AccelSensitivity);
        SteerToTarget(vm, dir.transform.InverseTransformPoint(dir.Target.position));

        // movement is applied centrally by VehicleMovement

        if (timer > duration)
            return SwitchState(dir.normal, vm);
        return this;
    }

    protected override void ResetStateParameters(VehicleManager vm)
    {
        timer = 0f;
    }
}
