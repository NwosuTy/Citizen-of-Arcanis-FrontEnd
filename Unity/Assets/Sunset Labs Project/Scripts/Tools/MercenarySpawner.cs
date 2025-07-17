using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class MercenarySpawner : MonoBehaviour
{
    private CharacterManager target;
    private MercenarySpawner Instance;

    private bool activeMercenaryInScene;
    private List<int> indexList = new();

    private TextAsset selectedMessage;
    private CharacterManager selectedManager;

    public bool testSpawner;

    [Header("Spawn Parameters")]
    [SerializeField] private float sphereRadius;
    [SerializeField] private LootBox[] lootBoxes;
    [SerializeField] private Transform spawnPoint;

    [Header("Parameters")]
    [SerializeField] private TextAsset[] messages;
    [SerializeField] private CharacterManager[] mercenaries;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if(testSpawner && activeMercenaryInScene != true)
        {
            CreateMercenary();
        }
    }

    public void SetTarget(CharacterManager target)
    {
        this.target = target;
    }

    private Vector3 SpawnPosition(float radius)
    {
        Vector3 randomPoint = Random.insideUnitSphere * radius + spawnPoint.position;
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, sphereRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return randomPoint;
    }

    private void CreateMercenary()
    {
        //Create Mercenary
        activeMercenaryInScene = true;
        selectedManager = GetRandomItem();
        Vector3 spawnPosition = SpawnPosition(sphereRadius);
     
        CharacterManager mercenary = Instantiate(selectedManager, spawnPosition, Quaternion.identity);
        mercenary.transform.SetParent(spawnPoint);

        //Assign Target
        mercenary.SetTarget(target);
        mercenary.Assignment.AddListener(() => SetAssignment(mercenary));
        testSpawner = false;
    }

    private void SetAssignment(CharacterManager c)
    {
        c.hasReached = true;
        selectedMessage = GetRandomText();
        
        CombatManager.Instance.hasMercenary = true;
        DialogueManager.Instance.HandleDialogue(selectedMessage, c);
    }

    public void RewardPlayer(PickableObject obj)
    {
        Vector3 pos = SpawnPosition(1.5f);
        int random = Random.Range(0, lootBoxes.Length);
        LootBox lootBox = Instantiate(lootBoxes[random], pos, Quaternion.identity);
        lootBox.AddItem(obj);
    }

    private TextAsset GetRandomText()
    {
        indexList.Clear();
        for (int i = 0; i < messages.Length; i++)
        {
            if (messages[i] == selectedMessage || indexList.Contains(i))
            {
                continue;
            }
            indexList.Add(i);
        }
        int randomIndex = Random.Range(0, indexList.Count);
        return messages[indexList[randomIndex]];
    }

    private CharacterManager GetRandomItem()
    {
        indexList.Clear();
        for(int i = 0; i < mercenaries.Length; i++)
        {
            if(mercenaries[i] == selectedManager || indexList.Contains(i))
            {
                continue;
            }
            indexList.Add(i);
        }
        int randomIndex = Random.Range(0, indexList.Count);
        return mercenaries[indexList[randomIndex]];
    }
}
