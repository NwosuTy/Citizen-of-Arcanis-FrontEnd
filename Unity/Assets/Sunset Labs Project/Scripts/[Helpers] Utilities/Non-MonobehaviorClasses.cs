using UnityEngine;
using UnityEngine.Pool;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;

public interface IDamagabele
{
    public CharacterManager TakingDamage_Character();
    public void TakeDamage(int damageValue, AttackType attackType);
}

#region Enums

public enum CornerBoundary 
{ 
    NorthLeft,
    NorthRight,
    SouthLeft,
    SouthRight,
    None
}

public enum TileDirection
{
    Left,
    Right,
    North,
    South,
    Ignore,
}

public enum TileType
{
    Fence,
    Normal,
    CornerPiece
}

public enum DrivingBehavior 
{ 
    Traffic,
    Pursuit,
    Racing
}

public enum DriverExperience
{
    Novice,
    Mid_Snr,
    Expert
}

public enum ItemType
{
    Currency,
    Collectible
}

public enum CompanionState
{
    Friendly,
    High_Alert
}

public enum CarDrive_Type
{
    AllWheels,
    RearWheels,
    FrontWheels
}

public enum SpeedType
{
    MPH,
    KPH
}

public enum DuelState
{
    Win,
    Draw,
    Lost,
    OnGoing
}

public enum BrakeCondition
{
    NeverBrake,
    TargetDirectionDifference,
    TargetDistance
}

public enum ItemBoxTier //More For Design
{
    Wood, //2 Items[1-3] and Coin[5-15]
    Metal, //3 Items[1-5] and Coin[5-25]
    Gold, //3 Items[2-5], Coin[10-25] and Token[5-15]
    Diamond //5 Items[3-7] and Ruby[1-10]
}

public enum WeaponType
{
    Gun,
    Melee
}

public enum WheelCount
{
    Two = 2,
    Three = 3,
    Four = 4,
    Six = 6,
    Eight = 8
}

#endregion

#region Classes

[System.Serializable]
public class MintRequest
{
    public string userId;
    public int[] tokenIds;
    public int[] amounts;
    public string recipient;
}

[System.Serializable]
public class UseItemRequest
{
    public int nftId;
    public int quantity;

    public UseItemRequest(int nftId, int quantity)
    {
        this.nftId = nftId;
        this.quantity = quantity;
    }
}

[System.Serializable]
public class ItemClass
{
    public int itemCount;
    public PickableObject pickedObj;
    public InventorySlotUI SlotUI { get; private set; }

    public ItemClass(int count, PickableObject item)
    {
        pickedObj = item;
        itemCount = count;
    }

    public void SetSlotUI(InventorySlotUI slotUI)
    {
        SlotUI = slotUI;
    }

    public void UpdateItemCount(bool shouldAdd)
    {
        float alpha;
        if (shouldAdd)
        {
            alpha = 1.0f;
            itemCount++;
        }
        else
        {
            alpha = 0.04f;
            itemCount--;
        }

        if(itemCount <= 0) itemCount = 0;
        SlotUI.UpdateSlotUI(alpha);
    }
}

[System.Serializable]
public class RewardBox
{
    public string boxname;
    public List<ItemClass> itemsList = new();
    public bool FinishedCleaning { get; private set; }

    public void FillUpBox(ItemBox itemBox, ItemClass reward)
    {
        FinishedCleaning = false;
        boxname = itemBox.boxName;
        itemsList.Add(reward);
    }

    public void CleanBox()
    {
        itemsList.RemoveAll(x => x.pickedObj == null);
        FinishedCleaning = true;
    }

    public void EmptyBox()
    {
        boxname = "";
        itemsList.Clear();
        FinishedCleaning = true;
    }

    public void HandleRewarding()
    {
        for (int i = 0; i < itemsList.Count; i++)
        {
            ItemClass itemClass = itemsList[i];
            CharacterInventoryManager.Instance.AddUnExistingItem(itemClass);
        }
        EmptyBox();
    }
}

[System.Serializable]
public class WeaponRecoil
{
    private int index;
    private float time;
    private Transform cameraObject;

    [Header("Parameters")]
    [SerializeField] private float duration;
    [SerializeField] private Vector2[] recoilPattern;

    [Header("Components")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    public void Initialize(Transform cameraObj, CinemachineImpulseSource impulseSource)
    {
        cameraObject = cameraObj;
        this.impulseSource = impulseSource;
    }

    public void ResetIndex()
    {
        index = 0;
    }

    public void HandleRecoil(float delta)
    {
        if(time > 0.0f)
        {
            //freeLook.m_YAxis.Value -= ((verticalRecoil / 1000) * delta) / duration;
            //freeLook.m_XAxis.Value -= ((horizontalRecoil / 1000) * delta) / duration;
            time -= delta;
        }
    }

    public void GenerateRecoilPattern()
    {
        time = duration;
        impulseSource.GenerateImpulse(cameraObject.forward);

        //float verticalRecoil = recoilPattern[index].y;
        //float horizontalRecoil = recoilPattern[index].x;

        index = NextIndex();
    }

    private int NextIndex()
    {
        return (index + 1) % recoilPattern.Length;
    }
}

public class Bullet
{
    public float time;
    public Vector3 initialPosition;
    public Vector3 initialVelocity;

    public Bullet(Vector3 pos, Vector3 vel)
    {
        time = 0.0f;
        initialPosition = pos;
        initialVelocity = vel;
    }
}

public class BulletFX
{
    private ObjectPool<BulletFX> pool;
    private ObjectPool<GameObject> bulletDecalPool;

    private MonoBehaviour context;
    private ParticleSystem bulletImpactFX;

    private WaitForSeconds impactDelay = new(2f);
    private WaitForSeconds decalDelay = new(7.5f);

    public BulletFX(GameObject decalPrefab, ParticleSystem impactFXPrefab, MonoBehaviour context)
    {
        bulletImpactFX = GameObject.Instantiate(impactFXPrefab);
        bulletDecalPool = ObjectPooler.GameObjectPool(decalPrefab, 50, 100);
        this.context = context;
    }

    public void SetPool(ObjectPool<BulletFX> pool) => this.pool = pool;

    public void GetObject()
    {
        bulletImpactFX.gameObject.SetActive(true);
    }

    public void HandleBulletImpact(Vector3 pos, Quaternion rot)
    {
        bulletImpactFX.transform.SetPositionAndRotation(pos, rot);
        bulletImpactFX.Emit(1);

        var decal = bulletDecalPool.Get();
        decal.transform.SetPositionAndRotation(pos, rot);
        decal.transform.Rotate(Vector3.forward, Random.Range(0, 360));
        decal.SetActive(true);

        context.StartCoroutine(ReleaseDecalAfterDelay(decal));
        pool.Release(this);
    }

    public void Release()
    {
        context.StartCoroutine(DisableImpactFX());
    }

    private IEnumerator DisableImpactFX()
    {
        yield return impactDelay;
        bulletImpactFX.gameObject.SetActive(false);
    }

    private IEnumerator ReleaseDecalAfterDelay(GameObject decal)
    {
        yield return decalDelay;
        bulletDecalPool.Release(decal);
    }
}

public class EvenOrOddAttribute : PropertyAttribute
{
    public bool isEven;
    public BoundInt boundary;

    public EvenOrOddAttribute(int min, int max, bool isEven = true)
    {
        this.isEven = isEven;
        boundary = new(min, max);
    }
}

public class ReadOnlyInspectorAttribute : PropertyAttribute
{
}

/// <summary>
/// Efficient spatial reservation grid.
/// Tracks which grid cells each vehicle has reserved so ClearReservations only touches relevant cells.
/// </summary>
public class ReservationGrid
{
    private const float CellSize = 10f;

    // Recycled lists pool to avoid allocations
    private readonly Stack<List<PathReservation>> _listPool = new();
    private readonly Dictionary<Vector2Int, List<PathReservation>> _grid = new();

    // Map of vehicle id -> cells that contain reservations for that vehicle
    private readonly Dictionary<int, List<Vector2Int>> _vehicleCellIndex = new();

    /// <summary>
    /// Clears all reservations for the provided vehicle id. Fast: touches only cells that contained reservations for this vehicle.
    /// </summary>
    public void ClearReservations(int vehicleId)
    {
        if (!_vehicleCellIndex.TryGetValue(vehicleId, out var cells))
            return;

        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (!_grid.TryGetValue(cell, out var list))
                continue;

            // remove reservations belonging to this vehicle
            for (int j = list.Count - 1; j >= 0; j--)
            {
                if (list[j].Vehicle_ID == vehicleId)
                {
                    list.RemoveAt(j);
                }
            }

            // if cell empty, recycle
            if (list.Count == 0)
            {
                _grid.Remove(cell);
                _listPool.Push(list);
            }
        }

        // Remove the record for the vehicle
        _vehicleCellIndex.Remove(vehicleId);
    }

    /// <summary>
    /// Register a reservation into the grid. Also records which cell this vehicle touched.
    /// </summary>
    public void RegisterReservation(PathReservation res)
    {
        Vector2Int cell = GetCell(res.position);

        if (!_grid.TryGetValue(cell, out var list))
        {
            list = _listPool.Count > 0 ? _listPool.Pop() : new List<PathReservation>(4);
            _grid[cell] = list;
        }

        list.Add(res);

        if (!_vehicleCellIndex.TryGetValue(res.Vehicle_ID, out var vehicleCells))
        {
            vehicleCells = new List<Vector2Int>(4);
            _vehicleCellIndex[res.Vehicle_ID] = vehicleCells;
        }

        // Avoid duplicate entries
        if (vehicleCells.Count == 0 || vehicleCells[^1] != cell)
        {
            vehicleCells.Add(cell);
        }
    }

    /// <summary>
    /// Checks if the provided reservation conflicts with any other reservations in the neighborhood.
    /// </summary>
    public bool CheckConflict(PathReservation res, out PathReservation other)
    {
        Vector2Int cell = GetCell(res.position);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                var neighborCell = new Vector2Int(cell.x + dx, cell.y + dz);
                if (!_grid.TryGetValue(neighborCell, out var list))
                    continue;

                for (int i = 0; i < list.Count; i++)
                {
                    var existing = list[i];
                    if (existing.Vehicle_ID == res.Vehicle_ID)
                        continue;

                    float sqrDist = (res.position - existing.position).sqrMagnitude;
                    float combinedRadius = res.Radius + existing.Radius;

                    if (sqrDist < combinedRadius * combinedRadius)
                    {
                        other = existing;
                        return true;
                    }
                }
            }
        }
        other = default;
        return false;
    }

    private static Vector2Int GetCell(Vector3 pos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(pos.x / CellSize),
            Mathf.FloorToInt(pos.z / CellSize)
        );
    }
}


[System.Serializable]
public class WayPointPath
{
    private Vector3 _finalDestination;
    private float[] _distances = new float[0];
    private Vector3[] _nodePositions = new Vector3[0];

    public WayPointNode FinalDestinationNode { get; private set; }
    public bool HasPath => PathNodes != null && PathNodes.Count > 0;

    [Header("Non-expert randomization tuning")]
    [Tooltip("Higher value -> more randomness for novices (bigger range)")]
    [SerializeField] private int noviceMaxRange = 3;
    [Tooltip("Higher value -> more randomness for mid-senior; lower -> more deterministic")]
    [SerializeField] private int midSnrMaxRange = 4;
    [field: SerializeField] public List<WayPointNode> PathNodes { get; private set; } = new();

    /// <summary>
    /// Preferred API: resolve start node using an injected WayPointController.
    /// This avoids any 'FindObjectOfType' calls and is fast.
    /// </summary>
    public void RefreshPath(DriverExperience exp, Transform requester, WayPointNode finalNode, WayPointController controller)
    {
        if (controller == null)
        {
            RefreshPath(exp, (WayPointNode)null, finalNode);
            return;
        }
        WayPointNode startNode = controller.GetClosestNodeInFrontOfObject(requester, 180f);
        RefreshPath(exp, startNode, finalNode);
    }

    /// <summary>
    /// Legacy overload for backward compatibility. Logs a warning and falls back to FindObjectOfType.
    /// Prefer calling the overload that accepts a WayPointController.
    /// </summary>
    public void RefreshPath(DriverExperience exp, Transform requester, WayPointNode startNode, WayPointNode finalNode)
    {
        var controller = Object.FindObjectOfType<WayPointController>();
        if (controller != null && requester != null)
        {
            startNode = controller.GetClosestNodeInFrontOfObject(requester, 180f);
        }
        RefreshPath(exp, startNode, finalNode);
    }

    /// <summary>
    /// Core path refresh: startNode -> finalNode.
    /// Includes the startNode as the first element in PathNodes and prevents duplicate nodes.
    /// Safe: uses a visited HashSet and bounded by max steps (20).
    /// </summary>
    public void RefreshPath(DriverExperience exp, WayPointNode startNode, WayPointNode finalNode)
    {
        PathNodes.Clear();

        // If no meaningful start or final, clear caches and exit.
        if (startNode == null || finalNode == null)
        {
            //CachePositionsAndDistances();
            FinalDestinationNode = finalNode;
            _finalDestination = finalNode != null ? finalNode.transform.position : Vector3.zero;
            return;
        }

        // Add the start node as first element
        PathNodes.Add(startNode);
        var visited = new HashSet<WayPointNode>(capacity: 32)
        {
            startNode
        };

        WayPointNode current = startNode;

        int safety = 0;
        const int maxSteps = 20; // existing constraint

        while (current != null && current != finalNode && ++safety <= maxSteps)
        {
            WayPointNode next;
            if (exp == DriverExperience.Expert)
            {
                next = current.GetNextNodeDistanceBased(finalNode.transform.position, visited);
            }
            else
            {
                // Tunable randomness
                int maxRange = (exp == DriverExperience.Novice) ? noviceMaxRange : midSnrMaxRange;
                // Heuristic: prefer distance when random sample >= 2 (keeps prior behavior but tunable)
                bool preferDistance = Random.Range(0, Mathf.Max(1, maxRange)) >= 2;
                next = current.GetNextNode(preferDistance, finalNode.transform.position, visited);
            }

            if (next == null)
            {
                break;
            }

            // Add if not present (defensive, although visited prevents dupes)
            if (!PathNodes.Contains(next))
            {
                visited.Add(next);
                PathNodes.Add(next);
            }
            current = next;
            if (current == finalNode) break;
        }

        CachePositionsAndDistances();
        FinalDestinationNode = finalNode;
        _finalDestination = finalNode != null ? finalNode.transform.position : Vector3.zero;
    }


    public float DistanceToFinalDestination(Vector3 currentPosition)
    {
        if (FinalDestinationNode == null)
        {
            return float.PositiveInfinity;
        }
        return Vector3.Distance(currentPosition, _finalDestination);
    }

    /// <summary>
    /// Build cached arrays used by the spline sampler. Makes arrays safe for Catmull-Rom sampling
    /// by ensuring at least 4 points exist (pads with repeats when necessary).
    /// </summary>
    private void CachePositionsAndDistances()
    {
        int count = PathNodes.Count;
        if (count == 0)
        {
            _nodePositions = new Vector3[0];
            _distances = new float[0];
            return;
        }

        // If we have fewer than 4 nodes, pad/repeat nodes so Catmull-Rom sampler has >= 4 points.
        if (count < 4)
        {
            int padded = 4;
            _nodePositions = new Vector3[padded];
            _distances = new float[padded];

            // Fill positions by repeating the small node list cyclically.
            for (int i = 0; i < padded; i++)
            {
                _nodePositions[i] = PathNodes[i % count].transform.position;
            }

            // Compute cumulative distances across the padded array
            float acc = 0f;
            for (int i = 0; i < padded - 1; i++)
            {
                _distances[i] = acc;
                acc += Vector3.Distance(_nodePositions[i], _nodePositions[i + 1]);
            }
            // last entry = total length
            _distances[padded - 1] = acc;
            return;
        }

        // For count >= 4, we create arrays of size count + 1 (last entry holds total length)
        _nodePositions = new Vector3[count + 1];
        _distances = new float[count + 1];

        float accumulator = 0f;
        for (int i = 0; i < count; ++i)
        {
            Vector3 p1 = PathNodes[i].transform.position;
            Vector3 p2 = PathNodes[(i + 1) % count].transform.position;

            _nodePositions[i] = p1;
            _distances[i] = accumulator;
            accumulator += Vector3.Distance(p1, p2);
        }

        // last entry: repeat last node position (safe sentinel) and store total length
        _nodePositions[count] = PathNodes[count - 1].transform.position;
        _distances[count] = accumulator;
    }

    /// <summary>
    /// Safely get a RoutePoint for 'dist'. Guards against empty/invalid caches and clamps dist.
    /// </summary>
    public RoutePoint GetRoutePoint(float dist)
    {
        // Defensive guards: if caches are invalid, return a safe RoutePoint fallback
        if (_nodePositions == null || _nodePositions.Length == 0 || _distances == null || _distances.Length == 0)
        {
            RoutePoint fallback = new();
            if (PathNodes != null && PathNodes.Count > 0)
            {
                fallback.position = PathNodes[0].transform.position;
                if (PathNodes.Count > 1)
                    fallback.direction = (PathNodes[1].transform.position - PathNodes[0].transform.position).normalized;
                else
                    fallback.direction = Vector3.forward;
            }
            else
            {
                fallback.position = Vector3.zero;
                fallback.direction = Vector3.forward;
            }
            return fallback;
        }

        // Clamp requested distance into the valid range to avoid helper indexing beyond bounds.
        float maxDist = _distances[^1];
        dist = Mathf.Clamp(dist, 0f, maxDist);

        // It's now safe to call the lower-level sampler.
        return MathPhysics_Helper.GetRoutePoint(_nodePositions, _distances, dist);
    }

    public float DistanceBetweenTwoNodes(int indexA, int indexB)
    {
        int count = PathNodes?.Count ?? 0;
        if (count == 0)
        {
            return -1f;
        }

        if (indexA < 0 || indexB < 0 || indexA >= count || indexB >= count)
        {
            return -1f;
        }

        var a = PathNodes[indexA];
        var b = PathNodes[indexB];
        if (a == null || b == null)
        {
            return -1f;
        }
        return Vector3.Distance(a.NodePosition, b.NodePosition);
    }

    /// <summary>
    /// Distance between the last two nodes; returns 0f if there are fewer than two nodes.
    /// </summary>
    public float DistanceBetweenLastTwoNodes()
    {
        int c = PathNodes?.Count ?? 0;
        if (c < 2)
        {
            return -1f;
        }
        return DistanceBetweenTwoNodes(c - 1, c - 2);
    }

    public float TotalLength => (_distances != null && _distances.Length > 0) ? _distances[^1] : 0f;

    public void Clear()
    {
        PathNodes?.Clear();
        _distances = new float[0];
        _nodePositions = new Vector3[0];

        FinalDestinationNode = null;
        _finalDestination = Vector3.zero;
    }

    public void SetNonExpertRanges(int noviceRange, int midSnrRange)
    {
        noviceMaxRange = Mathf.Max(1, noviceRange);
        midSnrMaxRange = Mathf.Max(1, midSnrRange);
    }
}

#endregion