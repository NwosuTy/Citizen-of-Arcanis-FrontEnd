using UnityEngine;
using System.Collections.Generic;

public class CharacterInventoryManager : MonoBehaviour
{
    InventorySlotUI slotUI;
    PickableObject spawnedItem;
    private Transform cameraObject;
    public static CharacterInventoryManager Instance { get; private set; }

    private List<ItemClass> itemList = new();
    private CharacterManager characterManager;

    [Header("Parameters")]
    [SerializeField] private ItemClass activeItem = new(0, null);
    [field: SerializeField] public InventoryManagerPanel_UI Panel { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("Multiple Instances In Scene");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        cameraObject = Camera.main.transform;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            Panel.EnablePanel();
        }

        if(Input.GetKeyDown(KeyCode.U))
        {
            UnEquipWeapon();
        }
    }

    public void SetCharacterManager(CharacterManager cm)
    {
        characterManager = cm;
    }

    public void UnEquipWeapon()
    {
        if (activeItem.pickedObj == null)
        {
            return;
        }
        PickableObject activeObj = activeItem.pickedObj;

        HandleItemAddition(activeObj);
        Panel.DisplayNotification($"UnEquiped {activeObj.ItemName}");

        Destroy(spawnedItem.gameObject);

        spawnedItem = null;
        activeItem = new(0, spawnedItem);
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
        slotUI = activeItem.SlotUI;
        activeObj = activeItem.pickedObj;

        Transform weaponHolder = WeaponHolder(activeObj.weaponManager);
        spawnedItem = Instantiate(activeObj, weaponHolder);

        spawnedItem.SetPhysicsSystem(false);
        spawnedItem.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        if(spawnedItem.weaponManager != null)
        {
            spawnedItem.weaponManager.Initialize(cameraObject, characterManager);
        }
        item.UpdateItemCount(false);
        spawnedItem.RemoveRigidBody();
        spawnedItem.gameObject.SetActive(true);

        slotUI.DropItem();
        itemList.Remove(item);
        Panel.DisplayNotification($"Equiped {equipedObj.ItemName}");
    }

    private Transform WeaponHolder(WeaponManager weaponManager)
    {
        return characterManager.CombatManager.WeaponHolder(weaponManager);
    }

    public void HandleItemDeletion(ItemClass itemClass)
    {
        itemClass.UpdateItemCount(false);
        itemList.Remove(itemClass);
    }

    public void HandleItemAddition(PickableObject pickedObj)
    {
        ItemClass existingItem = itemList.Find(x => x.pickedObj == pickedObj);

        if(existingItem != null)
        {
            existingItem.UpdateItemCount(true);
            return;
        }

        ItemClass itemClass = new(1, pickedObj.ItemObject.objectPrefab);
        AddUnExistingItem(itemClass);
    }

    public void AddUnExistingItem(ItemClass itemClass)
    {
        itemList.Add(itemClass);
        Panel.HandleSlotInitialization(itemClass);
    }
}
