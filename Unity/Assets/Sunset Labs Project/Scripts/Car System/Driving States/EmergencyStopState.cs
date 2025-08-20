using UnityEngine;

[CreateAssetMenu(menuName = "DrivingStates/EmergencyStopState")]
public class EmergencyStopState : DrivingStates
{
    private float timer;
    [SerializeField] private float stopDuration = 1.0f;

    public override DrivingStates HandleAction(VehicleManager vm)
    {
        // zero steering, full brake
        vm.horizontalInput = 0f;
        vm.verticalInput = 0f;
        vm.brakeInput = 1f;

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
