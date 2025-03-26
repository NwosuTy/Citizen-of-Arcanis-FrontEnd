using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryManagerPanel_UI : MonoBehaviour
{
    private bool hasInitialized;
    
    private float inactivityTimer;
    private bool isMouseOverPanel;
    
    private List<InventorySlotUI> currencySlotList;
    private List<InventorySlotUI> collectiblesSlotList;

    public Camera MainCamera { get; private set; }
    public CanvasGroup CanvasGrp { get; private set; }

    public static InventoryManagerPanel_UI Instance { get; private set; }
    public CharacterInventoryManager InventoryManager { get; private set; }

    [Header("Parameters")]
    [SerializeField] private Transform currencySlotParent;
    [SerializeField] private Transform collectiblesSlotParent;
    [Range(1f, 10f)] [SerializeField] private float idleTime;

    [Header("Panel UI Parameters")]
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private List<NotificationPanel> notifPanels;

    private void Awake()
    {
        if(Instance != null)
        {
            Debug.Log("Multiple Instances In Scene");
            Destroy(Instance.gameObject);
            return;
        }

        Instance = this;
        isMouseOverPanel = false;
        MainCamera = Camera.main;
        CanvasGrp = GetComponent<CanvasGroup>();

        notifPanels = new(GetComponentsInChildren<NotificationPanel>());
        currencySlotList = new(currencySlotParent.GetComponentsInChildren<InventorySlotUI>());
        collectiblesSlotList = new(collectiblesSlotParent.GetComponentsInChildren<InventorySlotUI>());
    }

    private void OnEnable()
    {
        if(hasInitialized)
        {
            return;
        }

        hasInitialized = true;

        DisablePanel();
        closeButton.onClick.AddListener(DisablePanel);
        notifPanels.ForEach(x => x.gameObject.SetActive(false));
    }

    private void Update()
    {
        if(isMouseOverPanel)
        {
            inactivityTimer = 0.0f;
            return;
        }

        if(inventoryPanel.activeSelf)
        {
            inactivityTimer += Time.deltaTime;
            if(inactivityTimer >= idleTime)
            {
                DisablePanel();
            }
        }
    }

    public void HandleSlotInitialization(ItemClass itemClass)
    {
        if(itemClass.ItemType == ItemType.Currency)
        {
            InitializeSlotUI(itemClass, currencySlotList);
            return;
        }
        InitializeSlotUI(itemClass, collectiblesSlotList);
    }

    public void SubscribeInventoryManager(CharacterInventoryManager inventoryManager)
    {
        InventoryManager = inventoryManager;
    }

    public void HandSlotUpdate(InventorySlotUI slotUI)
    {
        slotUI.UpdateItemCount();
    }

    private void InitializeSlotUI(ItemClass itemClass, List<InventorySlotUI> slotList)
    {
        InventorySlotUI slotUI = slotList.Find(x => x.IsActive != true);

        if(slotUI == null)
        {
            DisplayNotification("No More Collectible Space, Remove An Item");
            return;
        }
        slotUI.Initialize(itemClass);
        DisplayNotification($"{itemClass.ItemName} Has Been Added To Inventory");
    }

    public void DisplayNotification(string text)
    {
        NotificationPanel notif = notifPanels.Find(x => x.gameObject.activeSelf != true);

        if(notif != null)
        {
            notif.ShowNotification(text);
            return;
        }
        notifPanels[0].ShowNotification(text);
    }

    public void ResetInactiveTime()
    {
        inactivityTimer = 0.0f;
    }

    public void EnablePanel()
    {
        inventoryPanel.SetActive(true);
    }

    public void DisablePanel()
    {
        ResetInactiveTime();
        inventoryPanel.SetActive(false);
    }

    public void SetIsMouseOverPanel(bool status)
    {
        isMouseOverPanel = status;
    }

    public InventorySlotUI FindSlotUI(ItemClass item)
    {
        InventorySlotUI slot = null;
        bool isCurrency = item.ItemType == ItemType.Currency;

        if(isCurrency)
        {
            slot = GetSlot(item, currencySlotList);
        }
        else { slot = GetSlot(item, collectiblesSlotList); }
        return slot;
    }

    private InventorySlotUI GetSlot(ItemClass item, List<InventorySlotUI> slotList)
    {
        for(int i = 0; i < slotList.Count; i++)
        {
            InventorySlotUI slot = slotList[i];
            if(slot.IsActive != true)
            {
                continue;
            }

            if(slot.Item.ItemName == item.ItemName)
            {
                return slot;
            }
        }
        return null;
    }
}
