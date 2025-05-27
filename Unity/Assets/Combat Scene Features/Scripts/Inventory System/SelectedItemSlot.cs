using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectedItemSlot : MonoBehaviour
{
    private int index;
    private SelectedItemPanel selectedItemPanel;

    [Header("Item Content")]
    [SerializeField] private Image itemImage;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemCount;

    private void Start()
    {
        closeButton.onClick.AddListener(() => ClosePanel());
    }

    public void Initialize(int index, ItemClass i, SelectedItemPanel panel)
    {
        this.index = index;
        selectedItemPanel = panel;

        itemName.text = i.pickedObj.ItemName;
        itemImage.sprite = i.pickedObj.ItemImage;
        itemCount.text = i.itemCount.ToString("00");
        gameObject.SetActive(true);
    }

    public void ResetSlot()
    {
        index = -1;
        itemName.text = "";
        itemCount.text = "";
        itemImage.sprite = null;
        selectedItemPanel = null;
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
        selectedItemPanel.RemoveItem(index);
        ResetSlot();
    }
}
