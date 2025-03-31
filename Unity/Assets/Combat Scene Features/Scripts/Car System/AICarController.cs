using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class AICarController : MonoBehaviour
{
    private Rigidbody rb;
    private bool isWaiting = false;
    private bool movingForward = true;

    private float angleToWaypoint;
    private float distanceToWaypoint;
    private int currentWaypointIndex = 0;

    [Header("Movement Settings")]
    [Tooltip("Speed at which the car moves forward.")]
    [SerializeField] private float speed = 10f;

    [Tooltip("Speed at which the car turns towards waypoints.")]
    [SerializeField] private float turnSpeed = 5f;

    [Tooltip("Color of waypoints in the scene view.")]
    [SerializeField] private Color wayPointColor = Color.red;

    [Header("Sensor Settings")]
    [Tooltip("Distance at which the car detects obstacles.")]
    [SerializeField] private float sensorRange = 10f;

    [Tooltip("Range within which the car avoids obstacles.")]
    [SerializeField] private float avoidanceRange = 5f;

    [SerializeField] private LayerMask vehicleLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Waypoint Settings")]
    [Tooltip("List of waypoints the car will follow.")]
    [SerializeField] private Transform[] waypoints;

    [Tooltip("Minimum wait time at the first and last waypoint.")]
    [SerializeField] private float minWaitTime = 10f;

    [Tooltip("Maximum wait time at the first and last waypoint.")]
    [SerializeField] private float maxWaitTime = 15f;

    [Tooltip("Should the waypoints loop after reaching the last one?")]
    [SerializeField] private bool loopWaypoints = true;

    [Tooltip("Distance threshold to consider waypoint reached.")]
    [SerializeField] private float stoppingDistance = 1f;

    [Header("Wheel Settings")]
    [Tooltip("Speed of the wheel rotation (visual effect).")]
    [SerializeField] private float wheelRotationSpeed = 200f;

    [Tooltip("Array of wheels (front left, front right, rear left, rear right).")]
    [SerializeField] private Transform[] wheels;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if(waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        if (isWaiting || currentWaypointIndex >= waypoints.Length)
        {
            return;
        }

        MoveToWaypoint();
        RotateWheels();
        MaintainUprightPosition();
    }

    private void MoveToWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0 || waypoints[currentWaypointIndex] == null)
        {
            return;
        }

        Vector3 targetPosition = waypoints[currentWaypointIndex].position;
        Vector3 direction = (targetPosition - transform.position).normalized;

        // Rotate the vehicle to face the waypoint
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        angleToWaypoint = Vector3.Angle(transform.forward, direction);

        float effectiveTurnSpeed = movingForward ? turnSpeed : turnSpeed * 2f;
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, effectiveTurnSpeed * Time.fixedDeltaTime));

        rb.velocity = direction * speed;
        distanceToWaypoint = Vector3.Distance(transform.position, targetPosition);
        if (distanceToWaypoint < stoppingDistance)
        {
            if(movingForward)
            {
                if (currentWaypointIndex == waypoints.Length - 1)
                {
                    StartCoroutine(WaitAndReverse());
                    return;
                }
                currentWaypointIndex++;
            }
            
            else
            {
                if (currentWaypointIndex == 0)
                {
                    StartCoroutine(WaitAndReverse());
                    return;
                }
                currentWaypointIndex--;
            }
        }
    }

    private IEnumerator WaitAndReverse()
    {
        isWaiting = true;
        rb.velocity = Vector3.zero;

        // Ensure the car remains upright
        Quaternion fixedRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        rb.MoveRotation(fixedRotation);

        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(waitTime);

        // Reverse direction
        movingForward = !movingForward;
        isWaiting = false;
    }

    private void RotateWheels()
    {
        float rotationAmount = speed * wheelRotationSpeed * Time.fixedDeltaTime;

        foreach (Transform wheel in wheels)
        {
            if (wheel != null)
            {
                wheel.Rotate(Vector3.right, rotationAmount);
            }
        }
    }

    private void MaintainUprightPosition()
    {
        rb.rotation = Quaternion.Euler(0, rb.rotation.eulerAngles.y, 0);
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = wayPointColor;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawSphere(waypoints[i].position, 2f);
            }
        }

        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }

        if (loopWaypoints && waypoints.Length > 1 && waypoints[0] != null && waypoints[^1] != null)
        {
            Gizmos.DrawLine(waypoints[^1].position, waypoints[0].position);
        }

        if (currentWaypointIndex < waypoints.Length && waypoints[currentWaypointIndex] != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, waypoints[currentWaypointIndex].position);
        }
    }
}