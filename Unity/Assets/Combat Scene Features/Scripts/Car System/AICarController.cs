using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class AICarController : MonoBehaviour
{
    private Rigidbody rb;
    private bool isWaiting = false;
    private bool movingForward = true;
    private int currentWaypointIndex = 0;
    private bool firstCycleCompleted = false;
    private Collider[] detectedVehicles = new Collider[10]; // Buffer to reduce GC allocations

    [Header("Movement Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private Color wayPointColor = Color.red;

    [Header("Sensor Settings")]
    [SerializeField] private float sensorRange = 10f;
    [SerializeField] private float avoidanceRange = 5f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask vehicleLayer;

    [Header("Waypoint Settings")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float minWaitTime = 10f;
    [SerializeField] private float maxWaitTime = 15f;
    [SerializeField] private bool loopWaypoints = true;
    [SerializeField] private float stoppingDistance = 1f;

    [Header("Wheel Settings")]
    [SerializeField] private float wheelRotationSpeed = 200f;
    [SerializeField] private Transform frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("No waypoints assigned to AI Car! Please assign waypoints in the Inspector.");
            enabled = false;
            return;
        }
    }

    private void FixedUpdate()
    {
        if (isWaiting || currentWaypointIndex >= waypoints.Length) return;

        MoveToWaypoint();
        RotateWheels();
    }

    private void MoveToWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0 || waypoints[currentWaypointIndex] == null) return;

        Vector3 targetPosition = waypoints[currentWaypointIndex].position;
        Vector3 direction = (targetPosition - transform.position).normalized;

        // Rotate the vehicle to face the waypoint
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));

        // Move forward only when facing the waypoint
        if (Vector3.Angle(transform.forward, direction) < 10f)
        {
            rb.velocity = direction * speed;
        }

        // Check if we reached the last or first waypoint
        if (Vector3.Distance(transform.position, targetPosition) < stoppingDistance)
        {
            if (currentWaypointIndex == waypoints.Length - 1)
            {
                if (!firstCycleCompleted)
                {
                    firstCycleCompleted = true;
                }
                else
                {
                    StartCoroutine(WaitAndReverse());
                }
            }
            else if (currentWaypointIndex == 0 && firstCycleCompleted)
            {
                StartCoroutine(WaitAndReverse());
            }
            else
            {
                currentWaypointIndex = movingForward ? currentWaypointIndex + 1 : currentWaypointIndex - 1;
            }
        }
    }

    private IEnumerator WaitAndReverse()
    {
        isWaiting = true;
        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(waitTime);

        // Reverse direction
        movingForward = !movingForward;

        // Ensure smooth transition
        currentWaypointIndex = movingForward ? 0 : waypoints.Length - 1;
        isWaiting = false;
    }

    private void RotateWheels()
    {
        float rotationAmount = speed * wheelRotationSpeed * Time.fixedDeltaTime;

        if (rearLeftWheel != null) { rearLeftWheel.Rotate(Vector3.right, rotationAmount); }
        if (frontLeftWheel != null) { frontLeftWheel.Rotate(Vector3.right, rotationAmount); }
        if (rearRightWheel != null) { rearRightWheel.Rotate(Vector3.right, rotationAmount); }
        if (frontRightWheel != null) { frontRightWheel.Rotate(Vector3.right, rotationAmount); }
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
