using UnityEngine;

[CreateAssetMenu(menuName = "DrivingStates/ParkState")]
public class ParkState : DrivingStates
{
    private float timer;
    private int entriesCount;
    private float parkDuration;

    public override void OnEnter(VehicleManager vm)
    {
        timer = 0f;
        base.OnEnter(vm);
        entriesCount++;
        vm.Movement.canDrive = false;

        // don't forcibly zero velocity here — let the Movement / Brake handle graceful stop.
        // If you want a snap-stop as an exceptional UX choice, keep it but it's not recommended.
        // vm.RB.velocity = Vector3.zero; // removed to keep input-driven model

        var dir = vm.AIController;
        float minT = dir != null ? dir.MinParkTime : 1.5f;
        float maxT = dir != null ? dir.MaxParkTime : 3.0f;

        parkDuration = Random.Range(minT, maxT);
        dir?.ClearPath();
    }

    public override DrivingStates HandleAction(VehicleManager vm)
    {
        // park: brake applied and no steering
        vm.verticalInput = 0f;
        vm.brakeInput = 1f;
        vm.horizontalInput = 0f;

        timer += Time.deltaTime;
        if (timer >= parkDuration)
        {
            vm.AIController.ParkCompleted();
            vm.Movement.canDrive = true; // restore driving when leaving
            return SwitchState(vm.AIController.normal, vm);
        }
        return this;
    }

    protected override void ResetStateParameters(VehicleManager vm)
    {
        timer = 0f;
        parkDuration = 0f;
        vm.Movement.canDrive = true;
    }
}
