using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using Unity.VisualScripting;

public class CharacterCameraController : MonoBehaviour
{
    private CharacterManager characterManager;

    // cinemachine
    private float cinemachineTargetYaw;
    private float cinemachineTargetPitch;
    private const float _threshold = 0.01f;

    [Header("Sensitivity")]
    [SerializeField] private float normalSensivity = 1.0f;
    [SerializeField] private float lockedInSensitivity = 0.5f;

    [field: Header("Cinemachine Cameras and Others")]
    [field: SerializeField] public Image CrossHairImg { get; private set; }
    [field: SerializeField] public CinemachineVirtualCamera MainVirtualCamera { get; private set; }
    [field: SerializeField] public CinemachineVirtualCamera ShooterVirtualCamera { get; private set; }

    [Header("Cinemachine Properties")]
    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;
    [Tooltip("How far in degrees can you move the camera up")]
    [SerializeField] private float TopClamp = 30.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    [SerializeField] private float BottomClamp = -10.0f;
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    [SerializeField] private Transform CinemachineCameraTarget;

    private void Awake()
    {
        characterManager = GetComponent<CharacterManager>();

        CrossHairImg = GameObjectFinder.GetComponentByName<Image>("Crosshair");
        MainVirtualCamera = GameObjectFinder.GetComponentByName<CinemachineVirtualCamera>("Main Virtual Camera");
        ShooterVirtualCamera = GameObjectFinder.GetComponentByName<CinemachineVirtualCamera>("Gun Virtual Camera");
    }

    public void SetCameraTarget(Transform target)
    {
        CinemachineCameraTarget = target;
    }

    public void CameraRotation()
    {
        InputManager input = characterManager.PlayerInput;
        if (input.cameraInput.sqrMagnitude >= _threshold)
        {
            //Don't multiply mouse input by Time.deltaTime;
            float deltaTimeMultiplier = input.IsCurrentDeviceMouse() ? 0.35f : Time.deltaTime;

            float sensitivity = Sensitivity();
            cinemachineTargetYaw += input.cameraInput.x * deltaTimeMultiplier * sensitivity;
            cinemachineTargetPitch += input.cameraInput.y * deltaTimeMultiplier * sensitivity;
        }

        // clamp our rotations so our values are limited 360 degrees
        cinemachineTargetYaw = ClampAngle(cinemachineTargetYaw, float.MinValue, float.MaxValue);
        cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.rotation = Quaternion.Euler(cinemachineTargetPitch + CameraAngleOverride, cinemachineTargetYaw, 0.0f);
    }

    public void EnableShooterGraphics(bool status)
    {
        if(CrossHairImg != null) CrossHairImg.gameObject.SetActive(status);
        if (MainVirtualCamera != null) MainVirtualCamera.gameObject.SetActive(!status);
        if (ShooterVirtualCamera != null) ShooterVirtualCamera.gameObject.SetActive(status);
    }

    private float Sensitivity()
    {
        if(characterManager.isLockedIn)
        {
            if(characterManager.CombatManager.HasGun())
            {
                return 0.65f;
            }
            return lockedInSensitivity;
        }
        return normalSensivity;
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}
