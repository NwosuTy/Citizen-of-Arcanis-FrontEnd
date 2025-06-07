using UnityEngine;
using Cinemachine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

#region Enums

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

public enum DuelState
{
    Win,
    Draw,
    Lost,
    OnGoing
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
public struct BoundInt
{
    [Range(1,360)] public int minValue;
    [Range(1,360)] public int maxValue;
}


[System.Serializable]
public struct BoundFloat
{
    [Range(-10, 10)] public float minValue;
    [Range(-10, 10)] public float maxValue;
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
    public bool finishedCleaning { get; private set; }

    public void FillUpBox(ItemBox itemBox, ItemClass reward)
    {
        finishedCleaning = false;
        boxname = itemBox.boxName;
        itemsList.Add(reward);
    }

    public void CleanBox()
    {
        itemsList.RemoveAll(x => x.pickedObj == null);
        finishedCleaning = true;
    }

    public void EmptyBox()
    {
        boxname = "";
        itemsList.Clear();
        finishedCleaning = true;
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
        bulletDecalPool = ObjectPooler.GameObjectPool(decalPrefab);
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

public static class TrailFX
{
    public static void HandleTrailFX(float simulationSpeed, Vector3 start, Vector3 end, ObjectPool<TrailRenderer> trailPool, MonoBehaviour mb)
    {
        mb.StartCoroutine(TrailFXRoutine(simulationSpeed, start, end, trailPool));
    }

    private static IEnumerator TrailFXRoutine(float simulationSpeed, Vector3 start, Vector3 end, ObjectPool<TrailRenderer> trailPool)
    {
        TrailRenderer trail = trailPool.Get();
        Transform trailTransform = trail.transform;

        trailTransform.position = start;
        yield return null;

        trail.emitting = true;
        float distance = Vector3.Distance(start, end);
        float remainingDistance = distance;
        while (remainingDistance > 0f)
        {
            trailTransform.position = Vector3.Lerp(start, end, Mathf.Clamp01(1 - (remainingDistance / distance)));
            remainingDistance -= simulationSpeed * Time.deltaTime;
            yield return null;
        }

        trailTransform.position = end;
        yield return new WaitForSeconds(trail.time);
        yield return null;

        trail.emitting = false;
        trailTransform.position = end;
        trailPool.Release(trail);
    }
}
#endregion