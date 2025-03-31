using UnityEngine;
using System.Collections.Generic;
using DevionGames.InventorySystem;

public class CharacterInventoryManager : MonoBehaviour
{
    InventorySlotUI slotUI;
    PickableObject spawnedItem;
    private InventoryManagerPanel_UI panel;

    private RewardBox rewardBox = new();
    private List<ItemClass> itemList = new();

    [SerializeField] private ItemClass activeItem;
    [SerializeField] private Transform weaponHolder;

    private void Start()
    {
        panel = InventoryManagerPanel_UI.Instance;

        panel.SubscribeInventoryManager(this);
        CombatManager.Instance.AddRewardsToInventory(this);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            panel.EnablePanel();
        }

        if(Input.GetKeyDown(KeyCode.U))
        {
            UnEquipWeapon();
        }
    }

    //Would Change Later, PlaceHolder Method
    public void ParseToCombatmanager()
    {
        ResetLists();
        CombatManager.Instance.AssignRewardBox(rewardBox);
    }

    private void ResetLists()
    {
        rewardBox.CleanBox();
        for (int i = 0; i < itemList.Count; i++)
        {
            ItemClass item = itemList[i];
            ItemClass newItem = new(item.itemCount, item.pickedObj.ItemObject.objectPrefab);
            rewardBox.itemsList.Add(newItem);
        }
    }

    public void UnEquipWeapon()
    {
        if (activeItem == null)
        {
            return;
        }
        PickableObject activeObj = activeItem.pickedObj;

        HandleItemAddition(activeObj);
        panel.DisplayNotification($"UnEquiped {activeObj.ItemName}");

        Destroy(spawnedItem.gameObject);

        activeItem = null;
        spawnedItem = null;
    }

    public void EquipWeapon(ItemClass item)
    {
        PickableObject equipedObj = item.pickedObj;
        PickableObject activeObj = activeItem.pickedObj;
        //UnEquip current selected Item before Equipping New Item
        if(activeObj != null)
        {
            //If Item Is The Same Active Item No Need To Run Function
            if (activeObj.ItemName == equipedObj.ItemName)
            {
                return;
            }
            UnEquipWeapon();
        }

        activeItem = item;
        activeObj = activeItem.pickedObj;

        slotUI = activeItem.SlotUI;
        activeObj.SetPhysicsSystem(false);

        spawnedItem = Instantiate(activeObj, weaponHolder.transform);
        spawnedItem.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        item.UpdateItemCount(false);
        spawnedItem.RemoveRigidBody();
        spawnedItem.gameObject.SetActive(true);

        slotUI.DropItem();
        itemList.Remove(item);
        panel.DisplayNotification($"Equiped {equipedObj.ItemName}");
    }

    public void HandleItemDeletion(ItemClass itemClass)
    {
        itemClass.UpdateItemCount(false);
        itemList.Remove(itemClass);
    }

    public void HandleItemAddition(PickableObject pickedObj)
    {
        InventoryManagerPanel_UI inventoryPanel = InventoryManagerPanel_UI.Instance;
        ItemClass existingItem = itemList.Find(x => x.pickedObj == pickedObj);

        if(existingItem != null)
        {
            existingItem.UpdateItemCount(true);
            return;
        }

        ItemClass itemClass = new(1, pickedObj);
        AddUnExistingItem(itemClass);
    }

    public void AddUnExistingItem(ItemClass itemClass)
    {
        itemList.Add(itemClass);
        panel.HandleSlotInitialization(itemClass);
    }
}
