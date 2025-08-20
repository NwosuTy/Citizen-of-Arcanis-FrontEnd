using UnityEngine;

[RequireComponent(typeof(VehicleMovement))]
public class AIVehicleDirector : MonoBehaviour
{
    private WayPointNode currentNode;
    private VehicleManager vehicleManager;
    private ReservationGrid reservationGrid;
    public Transform Target { get; private set; }
    public RoutePoint ProgressPoint { get; private set; }

    private float speed;
    private bool hasPath;
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
    public float AccelWanderSpeed = 0.1f;
    public float AccelWanderAmount = 0.1f;
    public float SteerSensitivity = 0.05f;
    public float AccelSensitivity = 0.04f;
    [SerializeField] private float steeringSmoothTime = 0.25f;

    [Header("Assigned Path")]
    [SerializeField] private WayPointPath assignedPath;

    [Header("Path Progression")]
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

    [Header("Parking / Restart (config)")]
    [Tooltip("Minimum seconds to stay in park before starting a new journey")]
    [SerializeField] private float minParkTime = 3f;
    [Tooltip("Maximum seconds to stay in park before starting a new journey")]
    [SerializeField] private float maxParkTime = 7.5f;
    [ReadOnlyInspector] public float distanceToFinalPos;
    [ReadOnlyInspector] public float distancebtwFinalNode;

    [Header("State Machines")]
    public ParkState park;
    public NormalState normal;
    public ReverseState reverse;
    public OvertakeState overtake;
    public EmergencyStopState emergencyStop;
    [ReadOnlyInspector] public DrivingStates activeState;

    public float MinParkTime => minParkTime;
    public float MaxParkTime => maxParkTime;
    public WayPointController Circuit => circuit;

    public enum ProgressStyle
    {
        PointToPoint,
        SmoothAlongRoute
    }

    public void Awake()
    {
        vehicleManager = GetComponent<VehicleManager>();
        if (circuit == null) circuit = FindObjectOfType<WayPointController>();
    }

    public void Start()
    {
        // instantiate states (timers local)
        if (park != null) park = Instantiate(park);
        if (normal != null) normal = Instantiate(normal);
        if (reverse != null) reverse = Instantiate(reverse);
        if (overtake != null) overtake = Instantiate(overtake);
        if (emergencyStop != null) emergencyStop = Instantiate(emergencyStop);

        if (circuit != null) reservationGrid = circuit.ReservationGrid;
        Reset();
        hasPath = assignedPath != null && assignedPath.HasPath;
    }

    private void Reset()
    {
        if (circuit == null)
            return;

        progressNum = 0;
        progressDistance = 0f;

        Target = new GameObject($"{name}_Target").transform;
        Target.SetParent(circuit.DirectorsSpawnPoint, false);

        if (progressStyle != ProgressStyle.SmoothAlongRoute && circuit != null)
        {
            currentNode = circuit.GetClosestNodeInFrontOfObject(transform, 180f);
            if (currentNode != null)
                Target.SetPositionAndRotation(currentNode.transform.position, currentNode.transform.rotation);
        }

        if (park != null)
            activeState = normal.SwitchState(park, vehicleManager);
    }

    /// <summary>
    /// Called each frame by VehicleManager.Update(delta)
    /// </summary>
    public void VehicleDirector_Update()
    {
        if (circuit == null || reservationGrid == null)
        {
            return;
        }
        reservationGrid.ClearReservations(vehicleManager.GetInstanceID());
        if (vehicleManager.playerControlled)
        {
            return;
        }
        AdvancePath();
    }

    private void AdvancePath()
    {
        // If no path -> park and do not advance
        if (assignedPath == null || assignedPath.PathNodes == null || assignedPath.PathNodes.Count == 0)
        {
            if (activeState != park && park != null)
            {
                activeState = activeState.SwitchState(park, vehicleManager);
            }
            return;
        }

        distancebtwFinalNode = vehicleManager.Movement.StopDistance;
        distanceToFinalPos = assignedPath.DistanceToFinalDestination(transform.position);
        bool completedPath = distanceToFinalPos < vehicleManager.Movement.StopDistance;
        if (completedPath)
        {
            if (activeState != park && park != null)
            {
                activeState = activeState.SwitchState(park, vehicleManager);
            }
            return;
        }

        // Only progress when hasPath == true (so we don't try to advance while generating)
        if (hasPath)
        {
            switch (progressStyle)
            {
                case ProgressStyle.PointToPoint:
                    PointRoutine();
                    break;
                case ProgressStyle.SmoothAlongRoute:
                    SmoothAlongRoutine();
                    break;
            }
        }
    }

    public void StateChangeAndCheckReservations()
    {
        if (vehicleManager.playerControlled)
        {
            return;
        }

        if (activeState != null)
        {
            var next = activeState.HandleAction(vehicleManager);
            if (next != null)
            {
                activeState = next;
            }
        }
        // dynamic look-ahead based on speed to reduce false positives
        float dynamicLookahead = lookAheadForTargetOffset + speed * lookAheadForTargetFactor;
        var pos = transform.position + transform.forward * dynamicLookahead;
        float dynamicRadius = Mathf.Max(avoidanceRadius, speed * 0.02f); // tiny scale by speed
        var res = new PathReservation(dynamicRadius, pos, vehicleManager.GetInstanceID());
        reservationGrid.RegisterReservation(res);
    }

    #region Path Progression

    private void PointRoutine()
    {
        float distance = Vector3.Distance(Target.position, transform.position);
        if (distance < pointToPointThreshold)
        {
            progressNum = (progressNum + 1) % assignedPath.PathNodes.Count;
        }
        var node = assignedPath.PathNodes[progressNum];
        UpdateRouteProgress(node.transform.position, node.transform.rotation);
    }

    private void SmoothAlongRoutine()
    {
        speed = vehicleManager.Movement.GetLinearSpeed;

        RoutePoint rp = assignedPath.GetRoutePoint(progressDistance + lookAheadForTargetOffset + lookAheadForTargetFactor * speed);
        UpdateRouteProgress(rp.position, Quaternion.LookRotation(rp.direction));
    }

    public float GetAdaptiveDesiredSpeed(float baseSpeed) => 
        vehicleManager.Movement.GetAdaptiveDesiredSpeed(baseSpeed, progressDistance, assignedPath);

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

    /// <summary>
    /// Called by ParkState when its parking timer finishes.
    /// Director keeps responsibility to create/refresh a new path.
    /// </summary>
    public void ParkCompleted()
    {
        ClearPath();
        WayPointNode start = null;
        WayPointNode finalDestinationNode = null;

        if (circuit != null)
        {
            if (assignedPath != null)
            {
                WayPointNode exclude = assignedPath.FinalDestinationNode;
                finalDestinationNode = circuit.GetNode(vehicleManager.transform, exclude, out start);
            }
        }

        if (assignedPath == null || circuit == null || finalDestinationNode == null)
        {
            return;
        }
        assignedPath.RefreshPath(vehicleManager.driverExperience, start, finalDestinationNode);
        //Reduce Speed When 65% of Route Has Been Completed
        vehicleManager.Movement.SetStopDistance(assignedPath.DistanceBetweenLastTwoNodes() * 0.35f);
        hasPath = assignedPath.HasPath;
    }

    public void ClearPath()
    {
        hasPath = false;
        assignedPath?.Clear();
    }

    #region Conflict & Overtake Helpers

    public bool CheckForConflict(out PathReservation other)
    {
        other = default;
        if (reservationGrid == null) return false;
        var r = new PathReservation(avoidanceRadius, transform.position + transform.forward * 5f, vehicleManager.GetInstanceID());
        return reservationGrid.CheckConflict(r, out other);
    }

    public bool CheckYieldCondition()
    {
        // TODO - implement traffic yield logic
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
    /// Compute a steering target that avoids nearby vehicles.
    /// </summary>
    private Vector3 ComputeTargetWithAvoidance(ref float desiredSpeed, Vector3 originalTarget)
    {
        Vector3 adjustedTarget = originalTarget;

        var reservation = new PathReservation
        {
            Vehicle_ID = vehicleManager.GetInstanceID(),
            position = transform.position + transform.forward * lookAheadForTargetOffset,
            Radius = avoidanceRadius
        };
        bool conflictDetected = reservationGrid.CheckConflict(reservation, out _);

        if (conflictDetected)
        {
            avoidOtherCarTime = Time.time + avoidanceDuration;
            avoidOtherCarSlowdown = avoidanceSlowdownFactor;
            avoidPathOffset = Random.value > 0.5f ? avoidanceRadius : -avoidanceRadius;

            adjustedTarget += transform.right * avoidPathOffset;
            Debug.DrawLine(transform.position, adjustedTarget, Color.red);
        }
        else if (Time.time < avoidOtherCarTime)
        {
            adjustedTarget += transform.right * avoidPathOffset;
            desiredSpeed *= avoidOtherCarSlowdown;
        }
        else
        {
            float wander = (Mathf.PerlinNoise(Time.time * lateralWanderSpeed, RandomPerlin) * 2 - 1);
            adjustedTarget += wander * lateralWanderDistance * transform.right;
        }
        return adjustedTarget;
    }

    #endregion
}
