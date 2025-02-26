using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;

public class DuelManager : MonoBehaviour
{
    private static DuelManager instance;

    private CharacterManager enemy;
    private CharacterManager player;

    [Header("Enemy Objects")]
    [SerializeField] private UIBar enemyHealthBar;
    [SerializeField] private Transform enemySpawnPoint;

    [Header("Player Objects")]
    [SerializeField] private UIBar playerHealthBar;
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
    }

    private void Update()
    {
        if(player.isDead || enemy.isDead)
        {
            SceneManager.LoadScene("DemoPrincipalScene");
        }
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
            characterStatistic.SetHealthBar(enemyHealthBar);
            return;
        }
        player = newCharacter;
        newCharacter.currentTeam = Team.Blue;
        characterStatistic.SetHealthBar(playerHealthBar);
    }

    private void SetCameraTarget()
    {
        freeLookCamera.Follow = player.transform;
        freeLookCamera.LookAt = player.transform;
    }
}
