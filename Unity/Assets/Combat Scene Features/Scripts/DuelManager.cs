using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;

public class DuelManager : MonoBehaviour
{
    private static DuelManager instance;

    private CharacterManager enemy;
    private CharacterManager player;

    [Header("Character Objects")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("General Property")]
    [SerializeField] private CharacterDamageCollider weapon;
    [SerializeField] private CinemachineFreeLook freeLookCamera;

    private void Awake()
    {
        if(instance != null)
        {
            Destroy(instance);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        CombatManager combatManager = CombatManager.Instance;

        SetObject(playerSpawnPoint, combatManager.PlayerCombatPrefab);
        SetObject(enemySpawnPoint, combatManager.OppositionCombatPrefab);

        SetCameraTarget();
        uiManager.PrepareTimer();
    }

    private void Update()
    {
        if(player.isDead || enemy.isDead || uiManager.timeUp)
        {
            SceneManager.LoadScene("DemoPrincipalScene");
        }
        uiManager.HandleCountdown(Time.deltaTime);
    }

    private void SetObject(Transform parent, CharacterManager character)
    {
        CharacterManager newCharacter = Instantiate(character, parent);
        CharacterCombat combatManager = newCharacter.GetComponent<CharacterCombat>();
        CharacterStatistic characterStatistic = newCharacter.GetComponent<CharacterStatistic>();

        newCharacter.combatMode = true;
        combatManager.AssignWeapon(weapon);
        if(newCharacter.characterType == CharacterType.AI)
        {
            enemy = newCharacter;
            newCharacter.currentTeam = Team.Red;
        }
        else
        {
            player = newCharacter;
            newCharacter.currentTeam = Team.Blue;
        }
        uiManager.PrepareDuelingCharacter(newCharacter);
    }

    private void SetCameraTarget()
    {
        freeLookCamera.Follow = player.transform;
        freeLookCamera.LookAt = player.transform;
    }
}
