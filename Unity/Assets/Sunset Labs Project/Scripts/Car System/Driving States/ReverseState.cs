using UnityEngine;

[CreateAssetMenu(menuName = "DrivingStates/ReverseState")]
public class ReverseState : DrivingStates
{
    private float timer;

    [SerializeField] private float minTime = 1.5f;
    [SerializeField] private float duration = 2.0f;
    [SerializeField] private float reverseSpeed = -2.5f;
    [SerializeField] private float turnSpeedDegrees = 90f;

    public override DrivingStates HandleAction(VehicleManager vm)
    {
        var rigidBody = vm.RB;
        float delta = Time.deltaTime;
        Transform transform = vm.transform;

        timer += delta;
        GetDirectorAndMovement(vm, out var dir, out var move);

        // Calculate vector pointing to the target
        Vector3 toTarget = dir.Target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude > 0.001f)
        {
            // Desired rotation toward the target
            Quaternion desiredRotation = Quaternion.LookRotation(toTarget, Vector3.up);

            // Smoothly rotate the Rigidbody toward the target
            Quaternion rot = Quaternion.RotateTowards(rigidBody.rotation,desiredRotation,turnSpeedDegrees * delta);
            rigidBody.MoveRotation(rot);
        }

        // Drive in reverse
        ReverseThrottle(vm, reverseSpeed);
        move.Move(vm.horizontalInput, vm.verticalInput);

        // Optionally enforce a fixed reverse velocity
        rigidBody.velocity = transform.forward * reverseSpeed;
        if (timer > duration || (timer > minTime && Mathf.Abs(move.CurrentSpeed) < 0.5f))
        {
            return SwitchState(dir.normal, vm);
        }
        return this;
    }

    protected override void ResetStateParameters(VehicleManager vm)
    {
        timer = 0f;
    }
}
