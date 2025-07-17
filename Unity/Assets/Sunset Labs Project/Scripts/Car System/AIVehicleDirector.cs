// AIVehicleDirector.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(VehicleMovement))]
public class AIVehicleDirector : MonoBehaviour
{
    private WayPointNode currentNode;
    private DrivingStates activeState;
    private VehicleManager vehicleManager;
    private ReservationGridClass reservationGrid;
    public Transform Target { get; private set; }
    public RoutePoint ProgressPoint { get; private set; }

    private float speed;
    private int progressNum;
    private Vector3 lastPosition;
    private float progressDistance;

    // conflict & state
    private float avoidTime;
    private float avoidOffset;
    private float avoidOtherCarTime;
    private float avoidPathOffset = 0f;
    private float avoidOtherCarSlowdown = 1f;

    [Header("Parameters")]
    public float RandomPerlin = 0.5f;
    public float DesiredSpeed = 200f;
    public float AccelWanderSpeed = 0.1f;
    public float AccelWanderAmount = 0.1f;
    public float SteerSensitivity = 0.05f;
    public float AccelSensitivity = 0.04f;
    [SerializeField] private float steeringSmoothTime = 0.25f;

    [Header("Path Progression")]
    [SerializeField] private WayPointPath assignedPath;
    [SerializeField] private WayPointController circuit;
    [SerializeField] private float pointToPointThreshold = 4f;
    [SerializeField] private float lookAheadForTargetOffset = 5f;
    [SerializeField] private float lookAheadForTargetFactor = 0.1f;
    [SerializeField] private ProgressStyle progressStyle = ProgressStyle.SmoothAlongRoute;

    [Header("Avoidance Parameters")]
    [SerializeField] private float avoidanceRadius = 2.5f;
    [SerializeField] private float avoidanceDuration = 2f;
    [SerializeField] private float lateralWanderSpeed = 0.2f;
    [SerializeField] private float lateralWanderDistance = 1f;
    [SerializeField] private float avoidanceSlowdownFactor = 0.5f;

    [Header("State Machines")]
    public ParkState park;
    public NormalState normal;
    public ReverseState reverse;
    public OvertakeState overtake;
    public EmergencyStopState emergencyStop;

    public enum ProgressStyle
    {
        PointToPoint,
        FreeFlowRoutine,
        SmoothAlongRoute
    }

    public void Awake()
    {
        vehicleManager = GetComponent<VehicleManager>();
        if (circuit == null) circuit = FindObjectOfType<WayPointController>();
    }

    public void Start()
    {
        // instantiate states
        park = Instantiate(park);
        normal = Instantiate(normal);
        reverse = Instantiate(reverse);
        overtake = Instantiate(overtake);
        emergencyStop = Instantiate(emergencyStop);

        if(circuit != null) reservationGrid = circuit.ReservationGrid;
        Reset();
    }

    private void Reset()
    {
        if (circuit == null)
        {
            return;
        }
        progressNum = 0;
        progressDistance = 0f;

        Target = new GameObject($"{name}_Target").transform;
        Target.SetParent(circuit.DirectorsSpawnPoint, false);

        if (progressStyle != ProgressStyle.SmoothAlongRoute && circuit != null)
        {
            currentNode = circuit.GetClosestNodeToObject(transform, 180f);
            Target.SetPositionAndRotation(currentNode.transform.position, currentNode.transform.rotation);
        }
        activeState = overtake.SwitchState(normal, vehicleManager);
    }

    /// <summary>
    /// Called each frame by VehicleManager.Update(delta)
    /// </summary>
    public void VehicleDirector_Update(float delta)
    {
        if(circuit == null || reservationGrid == null)
        {
            return;
        }
        reservationGrid.ClearReservations(vehicleManager.GetInstanceID());
        if (vehicleManager.playerControlled)
        {
            return;
        }
        AdvancePath(delta);
        HandleStateChange();

        //Register new reservation at look-ahead position
        var pos = transform.position + transform.forward * lookAheadForTargetOffset;
        var res = new PathReservation(2.5f, pos, vehicleManager.GetInstanceID());
        reservationGrid.RegisterReservation(res);
    }

    private void AdvancePath(float delta)
    {
        switch (progressStyle)
        {
            case ProgressStyle.PointToPoint:
                PointRoutine();
                break;
            case ProgressStyle.FreeFlowRoutine:
                FreeFlowRoutine(delta);
                break;
            case ProgressStyle.SmoothAlongRoute:
                SmoothAlongRoutine(delta);
                break;
        }
        Debug.DrawLine(transform.position, Target.position, Color.green);
    }

    private void HandleStateChange()
    {
        if (activeState == null)
        {
            return;
        }
        var next = activeState.HandleAction(vehicleManager);
        if (next != null) activeState = next;
    }

    #region Path Progression

    private void PointRoutine()
    {
        if (assignedPath == null || assignedPath.PathNodes.Count == 0)
        {
            progressStyle = ProgressStyle.FreeFlowRoutine;
            return;
        }

        float distance = Vector3.Distance(Target.position, transform.position);
        if (distance < pointToPointThreshold)
        {
            progressNum = (progressNum + 1) % assignedPath.PathNodes.Count;
        }
        var node = assignedPath.PathNodes[progressNum];
        UpdateRouteProgress(node.position, node.rotation);
    }

    private void SmoothAlongRoutine(float delta)
    {
        if (assignedPath.PathNodes.Count == 0)
        {
            progressStyle = ProgressStyle.FreeFlowRoutine;
            return;
        }
        CalculateSpeed(delta);

        RoutePoint rp = assignedPath.GetRoutePoint(progressDistance + lookAheadForTargetOffset + lookAheadForTargetFactor * speed);
        UpdateRouteProgress(rp.position, Quaternion.LookRotation(rp.direction));
    }

    private void FreeFlowRoutine(float delta)
    {
        CalculateSpeed(delta);
        float distance = Vector3.Distance(Target.position, transform.position);
        if (distance < pointToPointThreshold)
        {
            WayPointNode nextNode = currentNode.GetNextNodeRandomly(null);
            currentNode = (nextNode != null) ? nextNode : circuit.GetClosestNodeToObject(transform, 180);
            StartCoroutine(SmoothLookAheadToTarget(delta, currentNode));
        }
        lastPosition = transform.position;
    }

    private IEnumerator SmoothLookAheadToTarget(float delta, WayPointNode node)
    {
        float elapsed = 0f;

        Target.GetPositionAndRotation(out Vector3 startPos, out Quaternion startRot);
        node.transform.GetPositionAndRotation(out Vector3 endPos, out Quaternion endRot);

        while (elapsed < steeringSmoothTime)
        {
            float t = elapsed / steeringSmoothTime;
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            Quaternion rot = Quaternion.Slerp(startRot, endRot, t);
            Target.SetPositionAndRotation(pos, rot);

            elapsed += delta;
            yield return null;
        }
    }

    private void CalculateSpeed(float delta)
    {
        if (delta > 0)
        {
            float sqrMag = (transform.position - lastPosition).sqrMagnitude;
            speed = Mathf.Sqrt(sqrMag) / delta;
        }
    }

    private void UpdateRouteProgress(Vector3 pos, Quaternion rot)
    {
        Target.SetPositionAndRotation(pos, rot);

        ProgressPoint = assignedPath.GetRoutePoint(progressDistance);
        Vector3 progressDelta = ProgressPoint.position - transform.position;

        if (Vector3.Dot(progressDelta, ProgressPoint.direction) < 0)
        {
            progressDistance += progressDelta.magnitude * 0.5f;
        }
        lastPosition = transform.position;
    }

    #endregion

    #region Conflict & Overtake Helpers

    public bool CheckForConflict(out PathReservation other)
    {
        other = default;
        if (reservationGrid == null) return false;
        var r = new PathReservation(2.5f, transform.position + transform.forward * 5f, vehicleManager.GetInstanceID());
        return reservationGrid.CheckConflict(r, out other);
    }

    public bool CheckYieldCondition()
    {
        // TODO
        return false;
    }

    public void StartOvertake()
    {
        avoidTime = Time.time + 2f;
        avoidOffset = (Random.value > .5f) ? 2.5f : -2.5f;
    }

    public void UpdateOvertakeTarget()
    {
        if (Time.time < avoidTime)
            Target.position += avoidOffset * Time.deltaTime * transform.right;
    }

    public void BlockOvertake()
    {
        Target.position += transform.right * ((Random.value > .5f) ? 1f : -1f);
    }

    public Vector3 ComputeNormalTarget(ref float desiredSpeed)
    {
        return ComputeTargetWithAvoidance(ref desiredSpeed, Target.position);
    }

    public float BehaviorSpeedFactor(DrivingBehavior b) => b switch
    {
        DrivingBehavior.Traffic => 1f,
        DrivingBehavior.Pursuit => 1.2f,
        DrivingBehavior.Racing => 1.5f,
        _ => 1f
    };

    public float BehaviorAggression(DrivingBehavior b) => b switch
    {
        DrivingBehavior.Traffic => 0.8f,
        DrivingBehavior.Pursuit => 1f,
        DrivingBehavior.Racing => 1.3f,
        _ => 1f
    };

    /// <summary>
    /// Your avoidance helper.
    /// </summary>
    /// <summary>
    /// Compute a steering target that avoids nearby vehicles.
    /// </summary>
    private Vector3 ComputeTargetWithAvoidance(ref float desiredSpeed, Vector3 originalTarget)
    {
        Vector3 adjustedTarget = originalTarget;

        // Build a look‑ahead reservation
        var reservation = new PathReservation
        {
            Vehicle_ID = vehicleManager.GetInstanceID(),
            position = transform.position + transform.forward * lookAheadForTargetOffset,
            Radius = avoidanceRadius
        };
        bool conflictDetected = reservationGrid.CheckConflict(reservation, out _);

        if (conflictDetected)
        {
            // start avoidance window
            avoidOtherCarTime = Time.time + avoidanceDuration;
            avoidOtherCarSlowdown = avoidanceSlowdownFactor;
            avoidPathOffset = Random.value > 0.5f ? avoidanceRadius : -avoidanceRadius;

            // immediate swerve
            adjustedTarget += transform.right * avoidPathOffset;
            Debug.DrawLine(transform.position, adjustedTarget, Color.red);
        }
        else if (Time.time < avoidOtherCarTime)
        {
            // still in avoidance window
            adjustedTarget += transform.right * avoidPathOffset;
            desiredSpeed *= avoidOtherCarSlowdown;
        }
        else
        {
            // subtle Perlin wander when clear
            float wander = (Mathf.PerlinNoise(Time.time * lateralWanderSpeed, RandomPerlin) * 2 - 1);
            adjustedTarget += wander * lateralWanderDistance * transform.right;
        }
        return adjustedTarget;
    }
    #endregion
}
