using UnityEngine;
using Cinemachine;
using System.Collections;
using UnityEngine.SceneManagement;

public class DuelManager : MonoBehaviour
{
    public static DuelManager Instance;
    public DuelState DuelState { get; private set; }

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
        combatManager = CombatManager.Instance;

        SetObject(playerSpawnPoint, combatManager.PlayerCombatPrefab);
        SetObject(enemySpawnPoint, combatManager.OppositionCombatPrefab);

        SetCameraTarget();
        uiManager.PrepareTimer();
        OnDuelStateChanged += HandleDuelStateChanged;
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

            combatManager.AssignRewardBox(rewardSystem.rewardBox);
            yield return new WaitUntil(() => combatManager.hasRewardBox);
        }
        StartCoroutine(LevelLoader.LoadSceneAsync("DemoPrincipalScene"));
    }

    public void SetRewardSystem(RewardSystem rewardSystem)
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
        CharacterCombat combat = newCharacter.GetComponent<CharacterCombat>();
        CharacterStatistic stats = newCharacter.GetComponent<CharacterStatistic>();

        newCharacter.combatMode = true;
        combat.AssignWeapon(weapon);

        if (newCharacter.characterType == CharacterType.AI)
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