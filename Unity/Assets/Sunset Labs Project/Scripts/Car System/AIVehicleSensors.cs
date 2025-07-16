using UnityEngine;

public class AIVehicleSensors : MonoBehaviour
{
    [Header("Transforms")]
    [SerializeField] private Transform leftTransform;
    [SerializeField] private Transform rightTransform;
    [SerializeField] private Transform forwardTransform;

    [Header("Parameters")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField][Range(0f, 1f)] private float rayLength;
}
