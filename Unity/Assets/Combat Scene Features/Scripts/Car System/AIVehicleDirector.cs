using System;
using System.Collections;
using UnityEngine;

[RequireComponent (typeof(VehicleMovement))]
public class AIVehicleDirector : MonoBehaviour
{
    VehicleManager vehicleManager;

    private float avoidPathOffset;
    private float avoidOtherCarTime;
    private float avoidOtherCarSlowdown;
    public Transform Target { get; private set; }

    [Header("Navigation Parameter")]
    [SerializeField] private WayPointNode currentNode;
    [SerializeField] private WayPointPath assignedPath;
    [SerializeField] private WayPointController circuit;
    [SerializeField] private float steeringSmoothTime = 0.25f;
    
    [Header("Status")]
    [SerializeField] private bool stopWhenTargetReached;
    [Tooltip("Whether to update the position smoothly along the route (good for curved paths) or just when we reach each waypoint.")]
    [SerializeField] private ProgressStyle progressStyle = ProgressStyle.SmoothAlongRoute;

    [Header("Thresholds & Sensitivity")]
    [Tooltip("How sensitively the AI uses the brake to reach the current desired speed")]
    [SerializeField] private float brakeSensitivity = 1f;
    [Tooltip("How sensitively the AI uses the accelerator to reach the current desired speed")]
    [SerializeField] private float accelSensitivity = 0.04f;
    [Tooltip("how sensitively the AI uses steering input to turn to the desired direction")]
    [SerializeField] private float steerSensitivity = 0.05f;
    [Tooltip("proximity to target to consider we 'reached' it, and stop driving.")]
    [SerializeField] private float reachTargetThreshold = 2f;
    [Tooltip("Proximity to waypoint which must be reached to switch target to next waypoint : only used in PointToPoint mode.")]
    [SerializeField] private float pointToPointThreshold = 4f;

    [Header("Look Ahead Parameters")]
    [Tooltip("The offset ahead along the route that the we will aim for")]
    [SerializeField] private float lookAheadForTargetOffset = 5;
    [Tooltip("A multiplier adding distance ahead along the route to aim for, based on current speed")]
    [SerializeField] private float lookAheadForTargetFactor = .1f;
    [Tooltip("The offset ahead only the route for speed adjustments (applied as the rotation of the waypoint target transform)")]
    [SerializeField] private float lookAheadForSpeedOffset = 10;
    [Tooltip("A multiplier adding distance ahead along the route for speed adjustments")]
    [SerializeField] private float lookAheadForSpeedFactor = .2f;

    [field: Header("Wandering Parameters")]
    [SerializeField] private float randomPerlin;
    [Tooltip("how fast the lateral wandering will fluctuate")]
    [SerializeField] private float lateralWanderSpeed = 0.1f;
    [Tooltip("how far the car will wander laterally towards its target")]
    [SerializeField] private float lateralWanderDistance = 3f;
    [Tooltip("how fast the cars acceleration wandering will fluctuate")]
    [SerializeField][Range(0, 1)] private float accelWanderSpeed = 0.1f;
    [Tooltip("how much the cars acceleration will wander")]
    [SerializeField][Range(0, 1)] private float accelWanderAmount = 0.1f;

    public enum ProgressStyle
    {
        PointToPoint,
        FreeFlowRoutine,
        SmoothAlongRoute
    }

    // these are public, readable by other objects - i.e. for an AI to know where to head!
    public RoutePoint TargetPoint { get; private set; }
    public RoutePoint SpeedPoint { get; private set; }
    public RoutePoint ProgressPoint { get; private set; }

    private float speed; // current speed of this object
    private Vector3 lastPosition; // Used to calculate current speed.
    private int progressNum; // the current waypoint number, used in point-to-point mode.
    private float progressDistance; // The progress round the route, used in smooth mode.

    private void Awake()
    {
        vehicleManager = GetComponent<VehicleManager>();
    }

    private void Reset()
    {
        progressNum = 0;
        progressDistance = 0;
        
        if (progressStyle != ProgressStyle.SmoothAlongRoute)
        {
            currentNode = circuit.GetClosestNodeToObject(vehicleManager.transform, 180f);
            Transform t = currentNode.transform;
            Target.SetPositionAndRotation(t.position, t.rotation);
        }
    }

    private void Start()
    {
        if (Target == null)
        {
            Target = new GameObject(name + " Waypoint Target").transform;
        }
        Reset();
    }

    public void ControlInput(float desiredSpeed, VehicleMovement move)
    {
        Vector3 offsetTargetPos = Target.position;
        Evade(desiredSpeed, offsetTargetPos, Target);

        float accelBrakeSensitivity = (desiredSpeed < move.CurrentSpeed) ? brakeSensitivity : accelSensitivity;
        HandleVerticalMovement(desiredSpeed, accelBrakeSensitivity, move);

        Vector3 localTarget = transform.InverseTransformPoint(offsetTargetPos);
        HandleHorizontalMovement(localTarget, move);

        move.Move(vehicleManager.horizontalInput, vehicleManager.verticalInput);
        if (stopWhenTargetReached && localTarget.magnitude < reachTargetThreshold)
        {
            move.driving = false;
        }
    }

    public void VehicleController_Update(float delta)
    {
        if(vehicleManager.playerControlled)
        {
            return;
        }

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

    private void PointRoutine()
    {
        if (assignedPath.PathNodes.Count == 0)
        {
            progressStyle = ProgressStyle.FreeFlowRoutine;
            return;
        }
        // point to point mode. Just increase the waypoint if we're close enough:
        Vector3 targetDelta = Target.position - transform.position;
        if (targetDelta.magnitude < pointToPointThreshold)
        {
            progressNum = (progressNum + 1) % assignedPath.PathNodes.Count;
        }
        Transform pathNode = assignedPath.PathNodes[progressNum];
        Target.SetPositionAndRotation(pathNode.position, pathNode.rotation);

        // get our current progress along the route
        ProgressPoint = assignedPath.GetRoutePoint(progressDistance);
        Vector3 progressDelta = ProgressPoint.position - transform.position;
        if (Vector3.Dot(progressDelta, ProgressPoint.direction) < 0)
        {
            progressDistance += progressDelta.magnitude;
        }
        lastPosition = transform.position;
    }

    private void FreeFlowRoutine(float delta)
    {
        if(delta > 0)
        {
            float currentSpeed = (transform.position - lastPosition).magnitude / delta;
            speed = Mathf.Lerp(speed, currentSpeed, delta);
        }

        Vector3 directionToTarget = Target.position - transform.position;
        if(directionToTarget.magnitude < pointToPointThreshold)
        {
            WayPointNode nextNode = currentNode.GetNextNodeRandomly(currentNode);
            print(nextNode);
            currentNode = (nextNode != null) ? nextNode : circuit.GetClosestNodeToObject(transform, 180);

            StartCoroutine(SmoothLookAheadToTarget(delta, currentNode));
        }
        lastPosition = transform.position;
    }

    private void SmoothAlongRoutine(float delta)
    {
        if(assignedPath.PathNodes.Count == 0)
        {
            progressStyle = ProgressStyle.FreeFlowRoutine;
            return;
        }
        // determine the position we should currently be aiming for
        // (this is different to the current progress position, it is a a certain amount ahead along the route)
        // we use lerp as a simple way of smoothing out the speed over time.
        if (delta > 0)
        {
            speed = Mathf.Lerp(speed, (lastPosition - transform.position).magnitude / delta, delta);
        }
        RoutePoint routePoint = assignedPath.GetRoutePoint(progressDistance + lookAheadForTargetOffset + lookAheadForTargetFactor * speed);
        Target.SetPositionAndRotation(routePoint.position, Quaternion.LookRotation(routePoint.direction));

        // get our current progress along the route
        ProgressPoint = assignedPath.GetRoutePoint(progressDistance);
        Vector3 progressDelta = ProgressPoint.position - transform.position;
        if (Vector3.Dot(progressDelta, ProgressPoint.direction) < 0)
        {
            progressDistance += progressDelta.magnitude * 0.5f;
        }
        lastPosition = transform.position;
    }

    private IEnumerator SmoothLookAheadToTarget(float delta, WayPointNode node)
    {
        float elapsed = 0.0f;
        Target.GetPositionAndRotation(out Vector3 start, out Quaternion startRot);
        node.transform.GetPositionAndRotation(out Vector3 end, out Quaternion endRot);

        while (elapsed < steeringSmoothTime)
        {
            float t = elapsed / steeringSmoothTime;
            Vector3 pos = Vector3.Lerp(start, end, t);
            Quaternion rot = Quaternion.Slerp(startRot, endRot, t);
            Target.SetPositionAndRotation(pos, rot);
            elapsed += delta;
            yield return null;
        }
    }

    private void Evade(float desiredSpeed, Vector3 offsetTargetPos, Transform target)
    {
        if (Time.time < avoidOtherCarTime)
        {
            desiredSpeed *= avoidOtherCarSlowdown;
            offsetTargetPos += target.right * avoidPathOffset;
            return;
        }
        offsetTargetPos += (Mathf.PerlinNoise(Time.time * lateralWanderSpeed, randomPerlin) * 2 - 1) * lateralWanderDistance * target.right;
    }

    private void HandleHorizontalMovement(Vector3 localTarget, VehicleMovement move)
    {
        float targetAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        float steer = Mathf.Clamp(targetAngle * steerSensitivity, -1, 1) * Mathf.Sign(move.CurrentSpeed);
        vehicleManager.horizontalInput = steer;
    }

    private void HandleVerticalMovement(float desiredSpeed, float accelBrakeSensitivity, VehicleMovement move)
    {
        float wanderAmount = accelWanderAmount;
        float accel = Mathf.Clamp((desiredSpeed - move.CurrentSpeed) * accelBrakeSensitivity, -1, 1);
        accel *= (1 - wanderAmount) + (Mathf.PerlinNoise(Time.time * accelWanderSpeed, randomPerlin) * wanderAmount);

        vehicleManager.verticalInput = accel;
    }
}