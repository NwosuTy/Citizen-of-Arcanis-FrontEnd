using TMPro;
using UnityEngine;
using Cinemachine;

/// <summary>
/// Handles the loading and instantiation of character prefabs based on player selection.
/// This script is used in game scenes where the selected character needs to be spawned.
/// It reads the player's character selection from PlayerPrefs and instantiates the corresponding prefab.
/// </summary>
public class LoadCharacter : MonoBehaviour
{
    private int selectedIndex;
    private CharacterManager spawnedCharacter;

    [Header("Parameters")]  
    [Tooltip("Transform reference for the position where the character will be spawned")]
    public Transform spawnPoint;
    [SerializeField] private Transform cameraAimObject;

    [Header("UI Elements")]
    [SerializeField] private PlayerUI playerUI;

    [Header("Camera Objects")]
    [SerializeField] private CinemachineVirtualCamera gunCamera;
    [SerializeField] private CinemachineVirtualCamera thirdPersonCamera;

    [Header("Instantiated Objects")]
    [Tooltip(" Array of drone prefabs that can be instantiated.")]
    public GameObject[] dronePrefab;
    [Tooltip("Array of character prefabs that can be instantiated.")]
    public CharacterManager[] characterManagerPrefabs;

    void Start()
    {
        CreateCharacter();
        CreateDroneObject();
    }

    private void CreateCharacter()
    {
        selectedIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
        if (selectedIndex < 0 || selectedIndex >= characterManagerPrefabs.Length)
        {
            Debug.LogError("�ndice de personaje seleccionado est� fuera de rango. Verifica los prefabs asignados en el inspector.");
            return;
        }
        CharacterManager prefab = characterManagerPrefabs[selectedIndex];
        spawnedCharacter = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        spawnedCharacter.SetCharacterType(CharacterType.Player);

        SetCharacterParameters();
        SetMiniMapAndCameraProperties(spawnedCharacter.CameraTarget);
    }

    private void CreateDroneObject()
    {
        int droneIndex = selectedIndex % dronePrefab.Length;
        GameObject drone = Instantiate(dronePrefab[droneIndex], spawnPoint.position + new Vector3(0, 2, -1), Quaternion.identity);

        // Set up the drone to follow the character
        DroneFollower droneFollower = drone.AddComponent<DroneFollower>();
        droneFollower.SetDrone_Target(spawnedCharacter);

        // Assign the main camera to the drone 
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            droneFollower.SetCameraTransform(mainCamera.transform);
            return;
        }
        Debug.LogWarning("No se encontró la cámara principal en la escena.");
    }

    private void SetMiniMapAndCameraProperties(Transform playerTransform)
    {
        MinimapController minimapController = FindObjectOfType<MinimapController>();

        if (minimapController != null)
        {
            minimapController.player = playerTransform;
            SetCameraProperty(gunCamera, playerTransform);
            SetCameraProperty(thirdPersonCamera, playerTransform);
        }
    }

    private void SetCharacterParameters()
    {
        if(spawnedCharacter == null)
        {
            return;
        }

        playerUI.SetParameters(spawnedCharacter);
        spawnedCharacter.currentTeam = Team.Blue;
        spawnedCharacter.CombatManager.SetCrossHair(cameraAimObject);
    }

    private void SetCameraProperty(CinemachineVirtualCameraBase camera, Transform playerTransform)
    {
        camera.Follow = playerTransform;
        camera.LookAt = playerTransform;
    }
}
