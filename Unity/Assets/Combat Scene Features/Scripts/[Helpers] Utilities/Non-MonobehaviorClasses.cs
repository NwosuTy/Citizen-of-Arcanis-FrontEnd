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
public class Reward
{
    public int itemCount;
    public ItemClass itemClass;
   
    public Reward(int count, ItemClass item)
    {
        itemClass = item;
        itemCount = count;
    }
}

[System.Serializable]
public class RewardBox
{
    public string boxname;
    public List<Reward> rewardBoxItems = new();
    public bool finishedCleaning { get; private set; }

    public void FillUpBox(ItemBox itemBox, Reward reward)
    {
        finishedCleaning = false;
        boxname = itemBox.boxName;
        rewardBoxItems.Add(reward);
    }

    public void CleanBox()
    {
        rewardBoxItems.RemoveAll(x => x.itemClass == null);
        finishedCleaning = true;
    }

    public void EmptyBox()
    {
        boxname = "";
        rewardBoxItems.Clear();
    }
}

#endregion