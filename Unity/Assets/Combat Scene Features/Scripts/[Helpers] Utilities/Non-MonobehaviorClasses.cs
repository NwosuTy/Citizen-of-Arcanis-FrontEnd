using UnityEngine;
using System.Collections.Generic;
using Cinemachine;

#region Enums

public enum ItemType
{
    Currency,
    Collectible
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
public class ItemClass
{
    public int id;
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

public class MintedItem
{
    public int Count { get; private set; }
    public int TokenID { get; private set; }
    public string Name { get; private set; }

    public MintedItem(int tokenID, int itemCount, string name)
    {
        Name = name;
        TokenID = tokenID;
        Count = itemCount;
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
#endregion