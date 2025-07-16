using UnityEngine;

[CreateAssetMenu(menuName = "DrivingStates/ParkState")]
public class ParkState : DrivingStates
{
    public override DrivingStates HandleAction(VehicleManager vm)
    {
        vm.horizontalInput = 0f;
        vm.verticalInput = -1f; // full brake
        vm.Movement.Move(vm.horizontalInput, vm.verticalInput);
        return this;
    }
}
