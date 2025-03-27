using UnityEngine;
using Cinemachine;
using System.Collections;
using UnityEngine.SceneManagement;

public class DuelManager : MonoBehaviour
{
    public DuelState duelState;
    public static DuelManager Instance;
    private CombatManager combatManager;

    private CharacterManager enemy;
    private CharacterManager player;
    private RewardSystem rewardSystem;

    [Header("Character Objects")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("General Property")]
    [SerializeField] private CharacterDamageCollider weapon;
    [SerializeField] private CinemachineFreeLook freeLookCamera;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(Instance);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        combatManager = CombatManager.Instance;

        duelState = DuelState.OnGoing;
        SetObject(playerSpawnPoint, combatManager.PlayerCombatPrefab);
        SetObject(enemySpawnPoint, combatManager.OppositionCombatPrefab);

        SetCameraTarget();
        uiManager.PrepareTimer();
    }

    private void Update()
    {
        duelState = SwitchDuelState();

        if(duelState != DuelState.OnGoing)
        {
            StartHandingReward();
            return;
        }
        uiManager.HandleCountdown(Time.deltaTime);
    }

    public void StartHandingReward()
    {
        StartCoroutine(HandleReward());
    }

    private DuelState SwitchDuelState()
    {
        if (player.isDead)
        {
            return DuelState.Lost;
        }
        if (enemy.isDead)
        {
            return DuelState.Win;
        }
        if (uiManager.timeUp)
        {
            return DuelState.Draw;
        }
        return DuelState.OnGoing;
    }

    private IEnumerator HandleReward()
    {
        if(duelState != DuelState.Lost)
        {
            StartCoroutine(rewardSystem.HandleFillUpRewardBox(duelState));
            yield return new WaitUntil(() => rewardSystem.hasFinished);

            combatManager.AssignRewardBox(rewardSystem);
            yield return new WaitUntil(() => combatManager.hasRewardBox);
        }
        SceneManager.LoadScene("DemoPrincipalScene");
    }

    public void AddRewardSystem(RewardSystem rewardSystem)
    {
        this.rewardSystem = rewardSystem;
    }

    private void SetCameraTarget()
    {
        freeLookCamera.Follow = player.transform;
        freeLookCamera.LookAt = player.transform;
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
}
