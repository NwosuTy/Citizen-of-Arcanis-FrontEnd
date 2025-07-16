using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using DevionGames.InventorySystem;

public class CombatManager : MonoBehaviour
{
    private WaitForSeconds waitForSeconds;
    public Transform CameraObject { get; protected set; }

    public static CombatManager Instance { get; private set; }
    [field: SerializeField] public CharacterManager PlayerCombatPrefab { get; private set; }
    [field: SerializeField] public CharacterManager OppositionCombatPrefab { get; private set; }

    [Header("Parameters")]
    public bool hasMercenary;
    [SerializeField] private float transitionDelay;
    [SerializeField] private Animator sceneTransitionAnimator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("A duplicate CombatManager was found and destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if(CameraObject == null)
        {
            CameraObject = Camera.main.transform;
        }
    }

    private void Start()
    {
        waitForSeconds = new WaitForSeconds(transitionDelay);
    }

    public void AssignPlayer(CharacterManager player)
    {
        PlayerCombatPrefab = player;
    }

    public void StartDuel(CharacterManager npc)
    {
        OppositionCombatPrefab = npc;
        StartCoroutine(LoadCombatScene());
    }

    public void HandleLoot(WeaponManager weapon, DuelState duelState)
    {
        if (duelState != DuelState.Lost && hasMercenary == true)
        {
            MercenarySpawner spawner = FindAnyObjectByType<MercenarySpawner>();
            if (spawner != null)
            {
                spawner.RewardPlayer(weapon.pickableObject);
            }
        }
    }

    private IEnumerator LoadCombatScene()
    {
        sceneTransitionAnimator.SetTrigger("Fade Out");

        yield return waitForSeconds;
        SceneManager.LoadScene("Combat Scene");

        yield return waitForSeconds;
        sceneTransitionAnimator.CrossFade("RectangleGridOut", 0.0f);
    }
}
