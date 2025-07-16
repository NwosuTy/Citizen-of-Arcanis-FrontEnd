using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;

public class DuelManager : MonoBehaviour
{
    public static DuelManager Instance;
    private List<int> indexList = new();

    private WeaponManager weapon;
    private Transform cameraObject;
    public DuelState DuelState { get; private set; }

    private CharacterManager enemy;
    private CharacterManager player;
    private RewardSystem rewardSystem;
    private CombatManager combatManager;

    [Header("Character Objects")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Camera Objects")]
    [SerializeField] private Transform cameraAimObject;
    [SerializeField] private CinemachineCamera gunCamera;
    [SerializeField] private CinemachineCamera thirdPersonCamera;

    [Header("Weapon Objects")]
    [SerializeField] private WeaponManager[] weaponManagers;

    public event DuelStateChanged OnDuelStateChanged;
    public delegate void DuelStateChanged(DuelState newState);

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        DuelState = DuelState.OnGoing;

        cameraObject = Camera.main.transform;
        combatManager = CombatManager.Instance;

        SetObject(enemySpawnPoint, combatManager.OppositionCombatPrefab, CharacterType.AI);
        SetObject(playerSpawnPoint, combatManager.PlayerCombatPrefab, CharacterType.Player);

        uiManager.PrepareTimer();
        OnDuelStateChanged += HandleDuelStateChanged;
        SetCameraProperty(gunCamera, player.CameraTarget);
        SetCameraProperty(thirdPersonCamera, player.CameraTarget);
    }

    private void Update()
    {
        DuelState newState = SwitchDuelState();
        if (newState != DuelState && DuelState == DuelState.OnGoing)
        {
            DuelState = newState;
            OnDuelStateChanged?.Invoke(DuelState);
        }

        if (DuelState == DuelState.OnGoing)
        {
            uiManager.HandleCountdown(Time.deltaTime);
        }
    }

    private void HandleDuelStateChanged(DuelState state)
    {
        if (state != DuelState.OnGoing)
        {
            StartCoroutine(HandleReward());
        }
    }

    private DuelState SwitchDuelState()
    {
        if (player.isDead) return DuelState.Lost;
        if (enemy.isDead) return DuelState.Win;
        if (uiManager.timeUp) return DuelState.Draw;
        return DuelState.OnGoing;
    }

    private IEnumerator HandleReward()
    {
        if (DuelState != DuelState.Lost)
        {
            yield return new WaitForSeconds(2.5f);
            yield return StartCoroutine(rewardSystem.HandleFillUpRewardBox(DuelState));

            rewardSystem.rewardBox.HandleRewarding();
            yield return new WaitUntil(() => rewardSystem.rewardBox.FinishedCleaning);
        }
        LevelLoader.HandleLoadLevel("DemoPrincipalScene", () => combatManager.HandleLoot(weapon, DuelState), this);
    }

    public void SetRewardSystem(RewardSystem rewardSystem)
    {
        this.rewardSystem = rewardSystem;
    }

    private void SetCameraProperty(CinemachineVirtualCameraBase camera, Transform playerTransform)
    {
        camera.Follow = playerTransform;
        camera.LookAt = playerTransform;
    }

    private void PrepareWeapon(CharacterManager characterManager)
    {
        weapon = GetRandomWeapon(null);
        if (weapon == null)
        {
            Debug.LogError("No weapon found");
            return;
        }

        Transform holder = characterManager.CombatManager.WeaponHolder(weapon);
        WeaponManager spawnedItem = Instantiate(weapon, holder);
        spawnedItem.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        spawnedItem.pickableObject.SetPhysicsSystem(false);
        spawnedItem.Initialize(cameraObject, characterManager);
        characterManager.CombatManager.AssignWeapon(spawnedItem);
    }

    private WeaponManager GetRandomWeapon(WeaponManager exclude)
    {
        indexList.Clear();
        for (int i = 0; i < weaponManagers.Length; i++)
        {
            WeaponManager weapon = weaponManagers[i];
            if (weapon == null || weapon == exclude)
            {
                continue;
            }
            indexList.Add(i);
        }

        int randomIndex = Random.Range(0, indexList.Count);
        int selectedIndex = indexList[randomIndex];
        return weaponManagers[selectedIndex];
    }

    private void SetObject(Transform parent, CharacterManager character, CharacterType characterType)
    {
        CharacterManager newCharacter = Instantiate(character, parent);

        newCharacter.combatMode = true;
        newCharacter.SetCharacterType(characterType);

        if (characterType == CharacterType.AI)
        {
            enemy = newCharacter;
            PrepareWeapon(enemy);
            enemy.currentTeam = Team.Red;
            newCharacter.findTarget = true;
        }
        else
        {
            player = newCharacter;
            player.currentTeam = Team.Blue;
            player.CombatManager.SetCrossHair(cameraAimObject);
        }
        uiManager.PrepareDuelingCharacter(newCharacter);
    }
}