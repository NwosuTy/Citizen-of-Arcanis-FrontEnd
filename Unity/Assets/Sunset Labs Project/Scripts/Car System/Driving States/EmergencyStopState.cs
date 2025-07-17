using UnityEngine;

[CreateAssetMenu(menuName = "DrivingStates/EmergencyStopState")]
public class EmergencyStopState : DrivingStates
{
    private float timer;
    private float stopDuration = 1.0f;

    public override DrivingStates HandleAction(VehicleManager vm)
    {
        vm.horizontalInput = 0f;
        vm.verticalInput = -1f;
        vm.Movement.Move(vm.horizontalInput, vm.verticalInput);

        timer += Time.deltaTime;
        if (timer > stopDuration)
            return SwitchState(vm.AIController.normal, vm);
        return this;
    }

    protected override void ResetStateParameters(VehicleManager vm)
    {
        timer = 0f;
    }
}
