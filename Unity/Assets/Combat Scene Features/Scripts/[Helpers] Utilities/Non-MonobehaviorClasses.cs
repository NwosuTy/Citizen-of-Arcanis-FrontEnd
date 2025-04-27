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

[System.Serializable]
public class WeaponRecoil
{
    private int index;    
    private Transform cameraObject;

    private float time;
    private float verticalRecoil;
    private float horizontalRecoil;

    [Header("Parameters")]
    [SerializeField] private float duration;
    [SerializeField] private Vector2[] recoilPattern;

    [Header("Components")]
    [SerializeField] private CinemachineVirtualCameraBase freeLook;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    public void Initialize(Transform cameraObj, CinemachineVirtualCameraBase virtualCamera, CinemachineImpulseSource impulseSource)
    {
        this.freeLook = virtualCamera;
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

        verticalRecoil = recoilPattern[index].y;
        horizontalRecoil = recoilPattern[index].x;

        index = NextIndex();
    }

    private int NextIndex()
    {
        return (index + 1) % recoilPattern.Length;
    }
}
#endregion