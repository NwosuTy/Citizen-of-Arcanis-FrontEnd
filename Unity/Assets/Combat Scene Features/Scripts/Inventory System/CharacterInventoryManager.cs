using UnityEngine;
using System.Collections.Generic;
using System;
using DevionGames.InventorySystem;

public class CharacterInventoryManager : MonoBehaviour
{
    //private CharacterInteractionScript interactionScript;
    //private PlaceHolderCombatScript placeHolderCombatScript;;

    ItemClass spawnedItem;
    InventorySlotUI slotUI;
    private InventoryManagerPanel_UI panel;

    [SerializeField] private ItemClass activeItem;
    [SerializeField] private Transform weaponHolder;

    //Would Make Run On Its Own For Now But Would Change Over Time
    private const int MAX_SIZE_CURRENCY = 3;
    private const int MAX_SIZE_COLLECTIBLES = 15;

    private List<ItemClass> currencyItems = new();
    private List<ItemClass> collectibleItems = new();

    private void Awake()
    {
        //interactionScript = GetComponent<CharacterInteractionScript>();
        //placeHolderCombatScript = GetComponent<PlaceHolderCombatScript>();
    }

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

    public void EquipWeapon(ItemClass item)
    {
        //UnEquip current selected Item before Equipping New Item
        if(activeItem != null)
        {
            //If Item Is The Same Active Item No Need To Run Function
            if (activeItem.ItemName == item.ItemName)
            {
                return;
            }
            UnEquipWeapon();
        }

        activeItem = item;
        slotUI = activeItem.SlotUI;

        spawnedItem = Instantiate(activeItem, weaponHolder.transform);
        spawnedItem.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        spawnedItem.RemoveRigidBody();
        spawnedItem.gameObject.SetActive(true);

        slotUI.DropItem();
        HandleItemDeletion(item);
        panel.DisplayNotification($"Equiped {item.ItemName}");
    }

    public void UnEquipWeapon()
    {
        if(activeItem == null)
        {
            return;
        }

        HandleItemAddition(1, activeItem);
        panel.DisplayNotification($"UnEquiped {activeItem.ItemName}");

        Destroy(spawnedItem.gameObject);

        activeItem = null;
        spawnedItem = null;
    }

    public void HandleItemAddition(int itemCount, ItemClass itemClass)
    {
        InventoryManagerPanel_UI inventoryPanel = InventoryManagerPanel_UI.Instance;

        InventorySlotUI existingItem = inventoryPanel.FindSlotUI(itemClass);
        if(existingItem != null)
        {
            existingItem.AddItem(itemCount, itemClass);
            return;
        }
        AddUnExistingItem(itemCount, itemClass, inventoryPanel);
    }

    public void HandleItemDeletion(ItemClass itemClass)
    {
        if (itemClass.ItemType == ItemType.Currency)
        {
            currencyItems.Remove(itemClass);
            return;
        }
        collectibleItems.Remove(itemClass);
    }

    public void AddUnExistingItem(int count, ItemClass itemClass, InventoryManagerPanel_UI inventoryPanel)
    {
        bool isCurrency = (itemClass.ItemType == ItemType.Currency);

        if(isCurrency)
        {
            currencyItems.Add(itemClass);
        }
        else { collectibleItems.Add(itemClass); }
        inventoryPanel.HandleSlotInitialization(count, itemClass);
    }
}
