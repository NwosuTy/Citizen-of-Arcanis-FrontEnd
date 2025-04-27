using UnityEngine;

/// <summary>
/// Manages drone behavior, allowing it to follow a target character with smooth motion, rotation,
/// and floating effects. The drone adjusts its position based on the camera's perspective.
/// </summary>
public class DroneFollower : MonoBehaviour
{
    /// <summary>
    /// The character that the drone will follow.
    /// </summary>
    private CharacterManager target;
    private Transform targetTransform;

    [Tooltip("The main camera's transform used for calculating relative positioning.")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Distance from the target character to the drone.")]
    [SerializeField] private float followDistance = 2.3f;
    [Tooltip("Base height for the drone above the target.")]
    [SerializeField] private float followHeight = 2.0f;

    [Tooltip("Smoothing speed for the drone's movement.")]
    [SerializeField] private float smoothSpeed = 8.0f;
    [Tooltip("Amount of vertical oscillation for floating effect.")]
    [SerializeField] private float floatAmount = 0.5f;

    [Tooltip("Speed of the floating oscillation.")]
    [SerializeField] private float floatSpeed = 1.5f;
    [Tooltip("Smoothing speed for position transitions when idle.")]
    [SerializeField] private float transitionSpeed = 5.0f;
    [Tooltip("Speed of rotation smoothing for the drone.")]
    [SerializeField] private float rotationSmoothSpeed = 3.0f;

    /// <summary>
    /// Internal velocity tracker for smooth movement.
    /// </summary>
    private Vector3 velocity = Vector3.zero;

    /// <summary>
    /// Initial offset for the drone's relative position.
    /// </summary>
    private Vector3 offset;

    /// <summary>
    /// Timer for managing floating oscillation.
    /// </summary>
    private float floatTimer = 0f;

    /// <summary>
    /// Flag to check if the target is moving.
    /// </summary>
    private bool isMoving = false;

    /// <summary>
    /// Target rotation for smooth orientation adjustments.
    /// </summary>
    private Quaternion targetRotation;
    void Start()
    {
        offset = new Vector3(1.0f, followHeight, -followDistance); // Offset to position the drone
        transform.position = Vector3.SmoothDamp(transform.position, offset, ref velocity, 1f / smoothSpeed);
        targetRotation = transform.rotation;
    }

    void Update()
    {
        if (target != null && cameraTransform != null)
        {
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Vector3 cameraRight = cameraTransform.right;
            Vector3 desiredPosition = targetTransform.position
                                      + (-cameraForward * followDistance)
                                      + (cameraRight * offset.x)
                                      + (Vector3.up * followHeight);

            floatTimer += Time.deltaTime * floatSpeed;
            float floatOffset = Mathf.Sin(floatTimer) * floatAmount;
            desiredPosition.y += floatOffset;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f / transitionSpeed);

            Vector3 directionToTarget = targetTransform.position - transform.position;
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            targetRotation = Quaternion.Slerp(targetRotation, lookRotation, rotationSmoothSpeed * Time.deltaTime);

            transform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// Performs fixed-time updates for smoother drone motion when the target is moving.
    /// Calculates the desired position and applies floating effects.
    /// </summary>
    void FixedUpdate()
    {
        if (target != null && cameraTransform != null)
        {
            isMoving = target.Controller.velocity.magnitude > 0.1f;

            if (isMoving)
            {
                Vector3 cameraForward = cameraTransform.forward;
                cameraForward.y = 0;
                cameraForward.Normalize();

                Vector3 cameraRight = cameraTransform.right;
                Vector3 desiredPosition = targetTransform.position
                                          + (-cameraForward * followDistance)
                                          + (cameraRight * offset.x)
                                          + (Vector3.up * followHeight);

                floatTimer += Time.fixedDeltaTime * floatSpeed;
                float floatOffset = Mathf.Sin(floatTimer) * floatAmount;
                desiredPosition.y += floatOffset;

                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f / smoothSpeed);
            }
        }
    }

    public void SetDrone_Target(CharacterManager cm)
    {
        target = cm;
        targetTransform = target.transform;
    }

    public void SetCameraTransform(Transform cameraTransform)
    {
        this.cameraTransform = cameraTransform;
    }
}
