using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private int clickCount = 0;
    private bool isDraggingItem;
    private float initalClickTime;

    private CanvasGroup canvasGroup;
    private ItemClass spawnedObject;
    
    //Contains Item Properties
    public bool IsActive { get; private set; }
    public ItemClass Item { get; private set; }
    private InventoryManagerPanel_UI inventoryManagerPanel;

    [Header("Item Properties")]
    public int itemCount;
    [SerializeField] private ItemType itemType;

    [Header("Slot UI Parameters")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Button itemButton;
    [SerializeField] private TextMeshProUGUI itemCountUI;

    [Header("Click Parameters")]
    [SerializeField] private UnityEvent onDoubleClickEvent;
    [Tooltip("Max Duration Between 2 Clicks")]
    [Range(0.01f, 0.55f)] [SerializeField] private float doubleClickDuration;

    [Header("Drag And Drop Parameters")]
    [SerializeField] private UnityEvent onItemDraggedEvent;
    [SerializeField] private UnityEvent onItemDroppedEvent;

    private void Start()
    {
        inventoryManagerPanel = InventoryManagerPanel_UI.Instance;
        canvasGroup = inventoryManagerPanel.CanvasGrp;
    }

    public void Initialize(ItemClass item)
    {
        Item = item;
        IsActive = true;

        SetColorAlpha(itemIcon, 1f);
        itemIcon.sprite = Item.ItemImage;

        itemCount = 1;
        itemCountUI.text = itemCount.ToString();
        if (itemButton != null)
        {
            onItemDroppedEvent.AddListener(() => DropItem());
            onItemDraggedEvent.AddListener(() => ClickItem());
            onDoubleClickEvent.AddListener(() => SelectItem());
        }
    }

    public void SelectItem()
    {
        if(IsActive == false || itemType == ItemType.Currency)
        {
            Debug.Log("No Item In Slot To Select");
            return;
        }

        DropItem();
        //Instantiate Item In Player Hand
        inventoryManagerPanel.ResetInactiveTime();
    }

    public void DropItem()
    {
        if (IsActive == false || itemType == ItemType.Currency)
        {
            Debug.Log("No Item In Slot To Drop");
            return;
        }

        itemCount--;
        if (itemCount == 0)
        {
            ClearSlot();
        }
    }

    private void ClickItem()
    {
        if (IsActive && itemCount > 0)
        {
            spawnedObject = Instantiate(Item);
            
            spawnedObject.gameObject.SetActive(true);
            spawnedObject.transform.position = GetMouseWorldPosition();
            spawnedObject.SetPhysicsSystem(false);
        }
    }

    private void ClearSlot()
    {
        IsActive = false;
        spawnedObject = null;

        itemCount = 0;
        itemCountUI.text = "";

        itemIcon.sprite = null;
        SetColorAlpha(itemIcon, 0.1f);

        inventoryManagerPanel.InventoryManager.HandleItemDeletion(Item);
        if (itemButton != null)
        {
            onItemDroppedEvent.RemoveListener(() => DropItem());
            onItemDraggedEvent.RemoveListener(() => ClickItem());
            onDoubleClickEvent.RemoveListener(() => SelectItem());
        }
        Item = null;
    }

    public void UpdateItemCount()
    {
        itemCountUI.text = itemCount.ToString();
    }

    #region Unity Event Functions

    public void OnPointerClick(PointerEventData eventData)
    {
        float elapsedTime = Time.unscaledTime - initalClickTime;
        if(elapsedTime > doubleClickDuration)
        {
            clickCount = 0;
        }

        clickCount++;
        if(clickCount == 1)
        {
            initalClickTime = Time.unscaledTime;
        }
        else if(clickCount > 1 && elapsedTime <= doubleClickDuration)
        {
            if(itemButton.interactable)
            {
                onDoubleClickEvent?.Invoke();
                clickCount = 0;
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDraggingItem = true;
        if(canvasGroup != null)
        {
            canvasGroup.alpha = 0.5f;
            //canvasGroup.blocksRaycasts = false;
        }
        onItemDraggedEvent?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(isDraggingItem != true || spawnedObject == null)
        {
            return;
        }
        spawnedObject.transform.position = GetMouseWorldPosition();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDraggingItem = false;
        if(canvasGroup != null)
        {
            canvasGroup.alpha = 1.0f;
            //canvasGroup.blocksRaycasts = true;
        }

        if(spawnedObject != null)
        {
            spawnedObject.SetPhysicsSystem(true);
        }
        onItemDroppedEvent?.Invoke();
    }

    #endregion

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = inventoryManagerPanel.MainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            return hitInfo.point;
        }
        return ray.origin + ray.direction * 5f;
    }

    private void SetColorAlpha(Image image, float alpha)
    {
        Color color = image.color;

        color.a = alpha;
        image.color = color;
    }
}
