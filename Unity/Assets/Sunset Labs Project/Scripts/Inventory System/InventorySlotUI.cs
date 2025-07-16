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

    private Sprite icon;
    private CanvasGroup canvasGroup;
    private PickableObject spawnedObject;
    
    //Contains Item Properties
    public bool IsActive { get; private set; }
    public ItemClass Item { get; private set; }
    private InventoryManagerPanel_UI inventoryManagerPanel;

    [Header("Item Properties")]
    [SerializeField] private ItemType itemType;

    [Header("Slot UI Parameters")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Button itemButton;
    [SerializeField] private TextMeshProUGUI itemName;
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
        inventoryManagerPanel = CharacterInventoryManager.Instance.Panel;
        canvasGroup = inventoryManagerPanel.CanvasGrp;
    }

    public void Initialize(ItemClass item)
    {
        AddItem(item);
        if (itemButton != null)
        {
            onItemDroppedEvent.AddListener(() => DropItem());
            onItemDraggedEvent.AddListener(() => ClickItem());
            onDoubleClickEvent.AddListener(() => SelectItem());
        }
    }

    public void AddItem(ItemClass item)
    {
        Item = item;
        IsActive = true;

        icon = item.pickedObj.ItemImage;
        UpdateSlotUI(1.0f);
    }

    public void SelectItem()
    {
        if (IsActive == false || itemType == ItemType.Currency)
        {
            Debug.Log("No Item In Slot To Select");
            return;
        }
        inventoryManagerPanel.ResetInactiveTime();
        CharacterInventoryManager.Instance.EquipWeapon(Item);
    }

    public void DropItem()
    {
        if (IsActive == false || itemType == ItemType.Currency)
        {
            Debug.Log("No Item In Slot To Drop");
            return;
        }

        if(Item.itemCount <= 0)
        {
            ClearSlot();
        }
    }

    private void ClickItem()
    {
        if (IsActive && Item.itemCount > 0)
        {
            PickableObject picked = Item.pickedObj;

            spawnedObject = Instantiate(picked);
            
            spawnedObject.gameObject.SetActive(true);
            spawnedObject.transform.SetPositionAndRotation(GetMouseWorldPosition(), picked.transform.rotation);
            spawnedObject.SetPhysicsSystem(false);
        }
    }

    private void ClearSlot()
    {
        IsActive = false;

        icon = null;
        spawnedObject = null;

        CharacterInventoryManager.Instance.HandleItemDeletion(Item);
        Item.SetSlotUI(null);

        if (itemButton != null)
        {
            onItemDroppedEvent.RemoveListener(() => DropItem());
            onItemDraggedEvent.RemoveListener(() => ClickItem());
            onDoubleClickEvent.RemoveListener(() => SelectItem());
        }
        UpdateSlotUI(0.04f);
        Item = null;
    }

    public void UpdateSlotUI(float alphaValue)
    {
        itemIcon.sprite = icon;
        SetColorAlpha(itemIcon, alphaValue);
        itemCountUI.text = Item.itemCount.ToString("00");
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
            if (itemButton.interactable == true)
            {
                onDoubleClickEvent?.Invoke();
            }
            clickCount = 0;
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

        Item.UpdateItemCount(false);
        onItemDroppedEvent?.Invoke();
    }

    #endregion

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = inventoryManagerPanel.MainCamera.ScreenPointToRay(Input.mousePosition);

        Vector3 originPosition = (Physics.Raycast(ray, out RaycastHit hitInfo)) ? hitInfo.point : ray.origin + ray.direction * 5f;

        originPosition.y = 0.25f;
        return originPosition;
    }

    private void SetColorAlpha(Image image, float alpha)
    {
        Color color = image.color;

        color.a = alpha;
        image.color = color;
    }
}
