using UnityEngine;

[CreateAssetMenu(menuName = "DrivingStates/NormalState")]
public class NormalState : DrivingStates
{
    private float stuckTimer;

    public override DrivingStates HandleAction(VehicleManager vm)
    {
        GetDirectorAndMovement(vm, out var dir, out var move);
        if (dir == null || move == null)
        {
            return this;
        }

        if (vm.drivingBehavior == DrivingBehavior.Traffic && dir.CheckYieldCondition())
        {
            return SwitchState(dir.park, vm);
        }

        if (move.CurrentSpeed < stuckThreshold)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > stuckTimeLimit)
            {
                return SwitchState(dir.reverse, vm);
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        // base speed and cautious adjustments
        float desiredSpeed = 0.0f;
        float baseSpeed = vm.Movement.topSpeed * dir.BehaviorSpeedFactor(vm.drivingBehavior);
        if (vm.drivingBehavior == DrivingBehavior.Traffic)
        {
            desiredSpeed = dir.GetAdaptiveDesiredSpeed(baseSpeed);
        }
        Vector3 fwd = move.CurrentSpeed > baseSpeed * 0.1f ? vm.RB.velocity : vm.transform.forward;

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
            default:
                break;
        }

        // conflict handling
        if (dir.CheckForConflict(out _))
        {
            if (vm.drivingBehavior == DrivingBehavior.Racing)
            {
                dir.BlockOvertake();
                ThrottleToSpeed(vm, baseSpeed * 0.8f, dir.AccelSensitivity);
                return this;
            }
            dir.StartOvertake();
            return SwitchState(dir.overtake, vm);
        }

        // steering & throttle with avoidance
        Vector3 rawTarget = dir.ComputeNormalTarget(ref desiredSpeed);

        SteerToTarget(vm, dir.transform.InverseTransformPoint(rawTarget));
        ThrottleToSpeed(vm, desiredSpeed, dir.AccelSensitivity);

        return this;
    }

    protected override void ResetStateParameters(VehicleManager vm)
    {
        stuckTimer = 0f;
    }
}
