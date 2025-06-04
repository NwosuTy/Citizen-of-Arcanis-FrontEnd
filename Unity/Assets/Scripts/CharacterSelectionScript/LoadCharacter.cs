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

    [Header("Parameters")]  
    [Tooltip("Transform reference for the position where the character will be spawned")]
    public Transform spawnPoint;
    [SerializeField] private Transform cameraAimObject;

    [Header("UI Elements")]
    [SerializeField] private PlayerUI playerUI;
    [SerializeField] private CharacterManager ccc;

    [Header("Camera Objects")]
    [SerializeField] private CinemachineVirtualCamera gunCamera;
    [SerializeField] private CinemachineVirtualCamera thirdPersonCamera;

    [Header("Instantiated Objects")]
    [Tooltip(" Array of drone prefabs that can be instantiated.")]
    public PlayerCompanion[] companions;
    [Tooltip("Array of character prefabs that can be instantiated.")]
    public CharacterManager[] characterManagerPrefabs;

    void Start()
    {
        CreateCharacter();
    }

    private void CreateCharacter()
    {
        selectedIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
        if (selectedIndex < 0 || selectedIndex >= characterManagerPrefabs.Length)
        {
            Debug.LogError("�ndice de personaje seleccionado est� fuera de rango. Verifica los prefabs asignados en el inspector.");
            return;
        }
        CharacterManager spawnedCharacter = Instantiate(characterManagerPrefabs[selectedIndex], spawnPoint);
        ccc = spawnedCharacter;
        spawnedCharacter.SetCharacterType(CharacterType.Player);

        CreateDroneObject(spawnedCharacter);
        SetCharacterParameters(spawnedCharacter);
        spawnedCharacter.CombatManager.SetDuellingCharacter();
        SetMiniMapAndCameraProperties(spawnedCharacter.CameraTarget);
    }

    private void CreateDroneObject(CharacterManager player)
    {
        int droneIndex = Random.Range(0, companions.Length);
        PlayerCompanion companion = Instantiate(companions[droneIndex], player.transform.position + new Vector3(3, 3, -3), Quaternion.identity);

        companion.SetFollowCharacter(player);
        companion.transform.SetParent(spawnPoint);
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

    private void SetCharacterParameters(CharacterManager player)
    {
        if(player == null)
        {
            return;
        }

        playerUI.SetParameters(player);
        player.currentTeam = Team.Blue;
        player.CombatManager.SetCrossHair(cameraAimObject);
    }

    private void SetCameraProperty(CinemachineVirtualCameraBase camera, Transform playerTransform)
    {
        camera.Follow = playerTransform;
        camera.LookAt = playerTransform;
    }
}
