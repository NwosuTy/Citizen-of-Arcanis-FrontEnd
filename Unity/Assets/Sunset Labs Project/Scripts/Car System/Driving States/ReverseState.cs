using UnityEngine;

[CreateAssetMenu(menuName = "DrivingStates/ReverseState")]
public class ReverseState : DrivingStates
{
    private float timer;

    [SerializeField] private float minTime = 1.0f;
    [SerializeField] private float duration = 2.2f;
    [SerializeField] private float reverseSpeed = -2.5f;
    [SerializeField] private float lookBehindDistance = 4.0f;

    public override DrivingStates HandleAction(VehicleManager vm)
    {
        if (vm == null) return this;

        timer += Time.deltaTime;
        GetDirectorAndMovement(vm, out var dir, out var move);
        if (dir == null || move == null) return this;

        // compute a look-behind point so the car swerves naturally while reversing
        Vector3 dirToTarget = (vm.transform.position - dir.Target.position);
        dirToTarget.y = 0f;
        if (dirToTarget.sqrMagnitude < 0.01f)
            dirToTarget = -vm.transform.forward;

        Vector3 lookPoint = vm.transform.position + dirToTarget.normalized * lookBehindDistance;

        // Convert to director-local space and steer — reverse=true inverts steering
        Vector3 local = dir.transform.InverseTransformPoint(lookPoint);
        SteerToTarget(vm, local, reverse: true);

        // set reverse throttle intent (writes brakeInput for reverse)
        ReverseThrottle(vm, dir, move, reverseSpeed);

        // Exit when done
        if (timer > duration || (timer > minTime && Mathf.Abs(move.CurrentSpeed) < 0.5f))
        {
            return SwitchState(dir.normal, vm);
        }
        return this;
    }

    private void ReverseThrottle(VehicleManager vm, AIVehicleDirector dir, VehicleMovement move, float reverseSpeed)
    {
        if (dir == null || move == null) return;
        // how urgently to back up: if current forward speed exists reduce reverse magnitude some
        float needed = Mathf.Clamp01((Mathf.Abs(reverseSpeed) - move.CurrentSpeed) * dir.AccelSensitivity * 0.8f);
        vm.verticalInput = 0f;
        vm.brakeInput = 2 * needed; // brakeInput used for reverse magnitude in physics layer
    }

    protected override void ResetStateParameters(VehicleManager vm)
    {
        timer = 0f;
    }
}
