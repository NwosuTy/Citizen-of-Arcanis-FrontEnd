using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    private RewardBox rewardBox = new();
    private WaitForSeconds waitForSeconds;
    public bool hasRewardBox {  get; private set; }

    public static CombatManager Instance { get; private set; }
    [field: SerializeField] public CharacterManager PlayerCombatPrefab { get; private set; }
    [field: SerializeField] public CharacterManager OppositionCombatPrefab { get; private set; }

    [Header("Parameters")]
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

    private void Start()
    {
        rewardBox = new();
        waitForSeconds = new WaitForSeconds(transitionDelay);
    }

    public void AssignPlayer(CharacterManager player)
    {
        PlayerCombatPrefab = player;
    }

    public void StartDuel(CharacterManager npc)
    {
        OppositionCombatPrefab = npc;
        InventoryManagerPanel_UI.Instance.InventoryManager.ParseToCombatmanager();
        StartCoroutine(LoadCombatScene());
    }

    private IEnumerator LoadCombatScene()
    {
        sceneTransitionAnimator.SetTrigger("Fade Out");

        yield return waitForSeconds;
        SceneManager.LoadScene("Combat Scene");

        yield return waitForSeconds;
        sceneTransitionAnimator.CrossFade("RectangleGridOut", 0.0f);
    }

    public void AssignRewardBox(RewardBox rewardBox)
    {
        hasRewardBox = true;
        for(int i = 0; i < rewardBox.itemsList.Count; i++)
        {
            ItemClass reward = rewardBox.itemsList[i];
            if(this.rewardBox.itemsList.Contains(reward))
            {
                continue;
            }
            this.rewardBox.itemsList.Add(rewardBox.itemsList[i]);
        }
    }

    public void AddRewardsToInventory(CharacterInventoryManager inventory)
    {
        for (int i = 0; i < rewardBox.itemsList.Count; i++)
        {
            ItemClass itemClass = rewardBox.itemsList[i];
            inventory.AddUnExistingItem(itemClass);
        }
        hasRewardBox = false;
        rewardBox.EmptyBox();
    }
}
