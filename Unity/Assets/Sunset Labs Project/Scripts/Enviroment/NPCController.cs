using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

public class NPCController : MonoBehaviour
{
    public static NPCController Instance;
    private enum SpawnMethod { Random, RoundRobin }

    private CharacterManager mercenary;
    public List<CharacterManager> npcList = new();

    private int spawnedNPCCount;
    private Collider[] npcColliders;
    private const int cnst_tileSize = 10;
    private readonly HashSet<Vector3> spawnedTiles = new();

    private Bounds lastNavMeshBuildBounds;
    private bool hasLastNavMeshBounds = false;

    private readonly Dictionary<CharacterManager, ObjectPool<CharacterManager>> npcPools = new();
    private readonly Dictionary<CharacterManager, ObjectPool<CharacterManager>> mercenaryPools = new();

    public CharacterManager player;

    [Space]
    [Header("Debug Parameters")]
    [SerializeField] private bool drawLastNavMeshBounds = true;
    [SerializeField] private Color lastNavMeshBoundsColor = Color.yellow;

    [Header("Parameters")]
    [SerializeField] private bool showRadius;
    [Range(15, 40)]
    [SerializeField] private int maxNPCCount = 20;
    [SerializeField] private float spawnRadius = 2f;
    [SerializeField] private LayerMask characterLayer;

    [Header("Spawn Tools")]
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private float spawnDensityPerTile = 0.5f;
    [SerializeField] private SpawnMethod spawnMethod = SpawnMethod.RoundRobin;

    [Header("Objects To Spawn")]
    [SerializeField] private CharacterManager[] Npcs;
    [SerializeField] private CharacterManager[] mercenaries;
    [SerializeField] private Vector3Int navMeshSize = new(40, 10, 40);

    // internal helpers
    private int roundRobinCounter = 0;
    private const int maxSampleAttemptsPerSpawn = 5;
    public Transform PlayerTransform => player.transform;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        npcColliders = new Collider[maxNPCCount];

        CreateNewPool(30, 35, Npcs, npcPools);
        CreateNewPool(3, 5, mercenaries, mercenaryPools);

        if (GameObjectTool.TryFindFirstObject<NavMeshBaker>(out var navMeshBuilder))
        {
            navMeshBuilder.OnNavMeshBuild += HandleSpawnUpdater;
        }
    }

    /// <summary>
    /// Called by NavMeshBaker when a partial navmesh build completes for a region (bounds).
    /// Updates the list of NPCs inside the bounds and releases distant NPCs; then spawns new NPCs around the player tile.
    /// </summary>
    public void HandleSpawnUpdater(Bounds bounds)
    {
        hasLastNavMeshBounds = true;
        lastNavMeshBuildBounds = bounds;
        int countFound = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, npcColliders, Quaternion.identity, characterLayer);

        var nearCharacters = new List<CharacterManager>(countFound);
        for (int i = 0; i < countFound; i++)
        {
            var col = npcColliders[i];
            if (col == null)
            {
                continue;
            }

            if(npcColliders[i].TryGetComponent<CharacterManager>(out var character) && character != player)
            {
                nearCharacters.Add(character);
            }
        }
        HashSet<CharacterManager> present = new(nearCharacters);
        var currentNPCsCopy = new List<CharacterManager>(npcList);

        foreach (var npc in currentNPCsCopy)
        {
            if(npc == null)
            {
                npcList.Remove(npc);
                continue;
            }

            if(present.Contains(npc) != true)
            {
                npcList.Remove(npc);
                npc.mySpawnPool?.Release(npc);
                spawnedNPCCount = Mathf.Max(0, spawnedNPCCount - 1);
            }
        }

        Transform playerTransform = player.transform;
        Vector3 currentTilePosition = new
        (
            Mathf.FloorToInt(playerTransform.position.x) / cnst_tileSize,
            Mathf.FloorToInt(playerTransform.position.y) / cnst_tileSize,
            Mathf.FloorToInt(playerTransform.position.z) / cnst_tileSize
        );

        if (!spawnedTiles.Contains(currentTilePosition))
        {
            spawnedTiles.Add(currentTilePosition);
        }
        HandleSpawn(currentTilePosition, true);
    }

    private void HandleSpawn(Vector3 currentTilePosition, bool isNPC)
    {
        CharacterManager[] characters;
        Dictionary<CharacterManager, ObjectPool<CharacterManager>> dictionary;

        if (isNPC)
        {
            characters = Npcs;
            dictionary = npcPools;
        }
        else
        {
            characters = mercenaries;
            dictionary = mercenaryPools;
        }
        if (characters == null || characters.Length == 0 || dictionary == null || dictionary.Count == 0)
            return;

        if (isNPC && spawnedNPCCount >= maxNPCCount)
            return;
        if (!isNPC && mercenary != null)
            return;
        SpawnMesh(isNPC, currentTilePosition, characters, dictionary);
    }

    private void SpawnMesh(bool isNPC, Vector3 currentTilePosition, CharacterManager[] characters,
        Dictionary<CharacterManager, ObjectPool<CharacterManager>> dictionary)
    {
        int navMeshCalc = (navMeshSize.x / cnst_tileSize) / 2;
        for (int x = -navMeshCalc; x < navMeshCalc; x++)
        {
            for (int z = -navMeshCalc; z < navMeshCalc; z++)
            {
                if (isNPC && spawnedNPCCount >= maxNPCCount)
                    return;

                Vector3 tilePosition = new(currentTilePosition.x + x, currentTilePosition.y, currentTilePosition.z + z);

                if (spawnedTiles.Contains(tilePosition))
                    continue;

                int enemiesSpawnedPerTile = 0;
                spawnedTiles.Add(tilePosition);

                int rndCount = Random.Range(7, maxNPCCount);
                while((!isNPC || spawnedNPCCount < rndCount))
                {
                    int spawnIndex;
                    int length = characters.Length;
                    if (spawnMethod == SpawnMethod.RoundRobin)
                    {
                        spawnIndex = roundRobinCounter % length;
                        roundRobinCounter++;
                    }
                    else
                    {
                        spawnIndex = Random.Range(0, length);
                    }

                    bool spawnSucceeded = false;
                    CharacterManager prefab = characters[spawnIndex];
                    for (int attempt = 0; attempt < maxSampleAttemptsPerSpawn && (!isNPC || spawnedNPCCount < maxNPCCount); attempt++)
                    {
                        if (SpawnObjectOnNavmesh(tilePosition, prefab, dictionary, isNPC))
                        {
                            enemiesSpawnedPerTile++;
                            spawnedNPCCount++;
                            spawnSucceeded = true;
                            break;
                        }
                    }

                    if (!spawnSucceeded)
                    {
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Try to sample a walkable position inside the tile and spawn an NPC from the pool.
    /// Returns true if spawn succeeded (pooled object is positioned, activated and added).
    /// </summary>
    private bool SpawnObjectOnNavmesh(Vector3 tilePos, CharacterManager prefab,
        Dictionary<CharacterManager, ObjectPool<CharacterManager>> dictionary, bool isNPC = true)
    {
        if (prefab == null || dictionary == null || !dictionary.ContainsKey(prefab))
        {
            return false;
        }
        int walkableArea = 1 << NavMesh.GetAreaFromName("Walkable");
        Vector3 randomPos = new(Random.Range(-27.5f, 27.5f), 0.0f, Random.Range(-27.5f, 27.5f));
        Vector3 samplePos = tilePos * cnst_tileSize + randomPos;

        if (NavMesh.SamplePosition(samplePos, out NavMeshHit hit, spawnRadius, walkableArea))
        {
            spawnPosition = hit.position;
            CharacterManager spawnedNPC = dictionary[prefab].Get();
            if (spawnedNPC == null)
            {
                return false;
            }
            spawnedNPC.mySpawnPool = dictionary[prefab];

            if (isNPC)
            {
                if (!npcList.Contains(spawnedNPC))
                {
                    npcList.Add(spawnedNPC);
                }
            }
            else
            {
                mercenary = spawnedNPC;
            }
            return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        if (!drawLastNavMeshBounds || !hasLastNavMeshBounds) return;

        Gizmos.color = lastNavMeshBoundsColor;
        // DrawWireCube expects the full size (not extents)
        Gizmos.DrawWireCube(lastNavMeshBuildBounds.center, lastNavMeshBuildBounds.size);
    }

    #region Object Pool

    private CharacterManager CreateObject(CharacterManager objToSpawn)
    {
        CharacterManager spawn = Instantiate(objToSpawn, spawnParent);
        // ensure the pooled object starts inactive and in a neutral state
        spawn.gameObject.SetActive(false);
        spawn.canUpdate = false;
        return spawn;
    }

    private void GetObject(CharacterManager spawnedObject)
    {
        StartCoroutine(PrepareGetObject(spawnedObject));
    }

    private IEnumerator PrepareGetObject(CharacterManager spawnedObject)
    {
        Transform spawnTransform = spawnedObject.transform;

        spawnedObject.canUpdate = false;
        spawnTransform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        yield return new WaitUntil(() => spawnTransform.position == spawnPosition);

        if(GameObjectTool.TryGetComponentInChildren(spawnTransform, out RandomSkinSelector rss))
        {
            rss.RandomSkin();
        }

        yield return null;
        spawnedObject.gameObject.SetActive(true);
        spawnedObject.canUpdate = true;
    }

    private ObjectPool<CharacterManager> CharacterPool(int min, int max, CharacterManager objToSpawn)
    {
        ObjectPool<CharacterManager> objectPool = new(
            () => CreateObject(objToSpawn),              // create
            spawn => { GetObject(spawn); }, // onGet
            spawn => { spawn.gameObject.SetActive(false); spawn.canUpdate = false; }, // onRelease
            spawn => { if (spawn != null) GameObject.Destroy(spawn.gameObject); },   // onDestroy
            false, min, max
        );
        return objectPool;
    }

    private void CreateNewPool(int min, int max, CharacterManager[] characters, Dictionary<CharacterManager, ObjectPool<CharacterManager>> dictionary)
    {
        if (characters == null || characters.Length == 0) return;

        foreach (var character in characters)
        {
            if (character == null) continue;
            if (dictionary.ContainsKey(character))
            {
                continue;
            }
            dictionary[character] = CharacterPool(min, max, character);
        }
    }
    #endregion
}
