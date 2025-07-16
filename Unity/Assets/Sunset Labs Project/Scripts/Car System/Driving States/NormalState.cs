// NormalState.cs
using UnityEngine;

[CreateAssetMenu(menuName = "DrivingStates/NormalState")]
public class NormalState : DrivingStates
{
    private float stuckTimer;

    public override DrivingStates HandleAction(VehicleManager vm)
    {
        GetDirectorAndMovement(vm, out var dir, out var move);

        // 1) Yield (traffic only)
        if (vm.drivingBehavior == DrivingBehavior.Traffic && dir.CheckYieldCondition())
            return SwitchState(dir.park, vm);

        // 2) Stuck → Reverse
        if (Mathf.Abs(move.CurrentSpeed) < stuckThreshold)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > stuckTimeLimit)
                return SwitchState(dir.reverse, vm);
        }
        else
        {
            stuckTimer = 0f;
        }

        // 3) Cautious speed reduction (unchanged)
        float baseSpeed = dir.DesiredSpeed * dir.BehaviorSpeedFactor(vm.drivingBehavior);
        float desiredSpeed = baseSpeed;
        Vector3 fwd = move.CurrentSpeed > baseSpeed * 0.1f
                      ? vm.RB.velocity
                      : vm.transform.forward;

        switch (move.BrakeConditionType)
        {
            case BrakeCondition.TargetDirectionDifference:
                {
                    float turnAngle = Vector3.Angle(dir.Target.forward, fwd);
                    float spin = vm.RB.angularVelocity.magnitude * move.CautiousAngularVelocityFactor;
                    float caution = Mathf.InverseLerp(0f, move.CautiousMaxAngle, Mathf.Max(spin, turnAngle));
                    desiredSpeed = Mathf.Lerp(baseSpeed, baseSpeed * move.CautiousSpeedFactor, caution);
                    break;
                }
            case BrakeCondition.TargetDistance:
                {
                    float dist = Vector3.Distance(dir.Target.position, vm.transform.position);
                    float distCaution = Mathf.InverseLerp(move.CautiousMaxDistance, 0f, dist);
                    float spinCaution = Mathf.InverseLerp(0f, move.CautiousMaxAngle,
                                            vm.RB.angularVelocity.magnitude * move.CautiousAngularVelocityFactor);
                    float caution = Mathf.Max(distCaution, spinCaution);
                    desiredSpeed = Mathf.Lerp(baseSpeed, baseSpeed * move.CautiousSpeedFactor, caution);
                    break;
                }
            case BrakeCondition.NeverBrake:
                break;
        }

        // 4) Conflict → Overtake or block (unchanged)
        if (dir.CheckForConflict(out _))
        {
            if (vm.drivingBehavior == DrivingBehavior.Racing)
            {
                dir.BlockOvertake();
                ThrottleToSpeed(vm, baseSpeed * 0.8f, dir.AccelSensitivity);
                vm.Movement.Move(vm.horizontalInput, vm.verticalInput);
                return this;
            }
            dir.StartOvertake();
            return SwitchState(dir.overtake, vm);
        }

        // 5) Normal steering & throttle, with avoidance
        Vector3 rawTarget = dir.ComputeNormalTarget(ref desiredSpeed);

        SteerToTarget(vm, dir.transform.InverseTransformPoint(rawTarget));
        ThrottleToSpeed(vm, desiredSpeed, dir.AccelSensitivity);

        vm.Movement.Move(vm.horizontalInput, vm.verticalInput);
        return this;
    }

    protected override void ResetStateParameters(VehicleManager vm)
    {
        stuckTimer = 0f;
    }
}
