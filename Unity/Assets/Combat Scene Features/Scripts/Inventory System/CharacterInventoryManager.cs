using UnityEngine;
using System.Collections.Generic;

public class CharacterInventoryManager : MonoBehaviour
{
    //private CharacterInteractionScript interactionScript;

    //Would Make Run On Its Own For Now But Would Change Over Time
    private const int MAX_SIZE_CURRENCY = 3;
    private const int MAX_SIZE_COLLECTIBLES = 15;

    private List<ItemClass> currencyItems = new();
    private List<ItemClass> collectibleItems = new();

    private void Awake()
    {
        //interactionScript = GetComponent<CharacterInteractionScript>();
    }

    private void Start()
    {
        InventoryManagerPanel_UI.Instance.SubscribeInventoryManager(this);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            InventoryManagerPanel_UI.Instance.EnablePanel();
        }
    }

    public void HandleItemAddition(ItemClass itemClass)
    {
        if(itemClass.ItemType == ItemType.Currency)
        {
            AddItem(MAX_SIZE_CURRENCY, itemClass, currencyItems);
            return;
        }
        AddItem(MAX_SIZE_COLLECTIBLES, itemClass, collectibleItems);
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

    private void AddItem(ItemClass item, List<ItemClass> itemList)
    {
        InventoryManagerPanel_UI panel = InventoryManagerPanel_UI.Instance;
        InventorySlotUI existItem = InventoryManagerPanel_UI.Instance.FindSlotUI(item);

        panel.EnablePanel();

        if (existItem != null)
        {
            existItem.itemCount++;
            panel.HandSlotUpdate(existItem);
            return;
        }
        itemList.Add(item);
        panel.HandleSlotInitialization(item);
    }

    private void AddItem(int maxCount, ItemClass item, List<ItemClass> itemList)
    {
        if(itemList.Count > maxCount)
        {
            print(itemList.Count + " maxCount " + maxCount);     
            InventoryManagerPanel_UI.Instance.DisplayNotification("No More Collectible Space, Remove An Item");
            return;
        }
        AddItem(item, itemList);
    }
}
