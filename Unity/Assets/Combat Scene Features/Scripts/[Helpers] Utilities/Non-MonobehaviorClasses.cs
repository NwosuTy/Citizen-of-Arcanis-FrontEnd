using UnityEngine;
using System.Collections.Generic;

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

#endregion

#region Classes

[System.Serializable]
public struct BoundInt
{
    [Range(1,360)] public int minValue;
    [Range(1,360)] public int maxValue;
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
}

public class Ammo
{
    public float time;
    public Vector3 initialPosition;
    public Vector3 initialVelocity;

    public Ammo(Vector3 pos, Vector3 vel)
    {
        time = 0.0f;
        initialPosition = pos;
        initialVelocity = vel;
    }
}
#endregion