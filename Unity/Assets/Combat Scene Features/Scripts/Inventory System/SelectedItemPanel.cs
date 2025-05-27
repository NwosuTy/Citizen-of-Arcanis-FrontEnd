using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class SelectedItemPanel : MonoBehaviour
{
    private bool hasInitialized;
    private readonly List<MintedItem> itemList = new();

    [Header("UI Button Parameters")]
    [SerializeField] private Button mintButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button equipButton;

    [Header("Parameters")]
    [SerializeField] private GameObject panel;
    [SerializeField] private NotificationPanel notifPanel;
    [SerializeField] private List<SelectedItemSlot> selectItemSlots = new();

    private void OnEnable()
    {
        if(hasInitialized)
        {
            return;
        }

        hasInitialized = true;
        mintButton.onClick.AddListener(() => MintItem());
        exitButton.onClick.AddListener(() => ClosePanel());
    }

    public void SelectedItem_Update()
    {
        equipButton.interactable = (itemList.Count < 2);
    }

    public void OpenPanel(ItemClass i)
    {
        if(itemList.Count > selectItemSlots.Count)
        {
            return;
        }

        int index = itemList.Count;
        equipButton.onClick.RemoveAllListeners();
        MintedItem mint = new(i.id, i.itemCount, i.pickedObj.ItemName);

        itemList.Add(mint);
        SelectedItemSlot slot = selectItemSlots.Find(x => x.gameObject.activeSelf != true);

        slot.Initialize(index, i, this);
        equipButton.onClick.AddListener(() => EquipItem(i));

        if (panel.activeSelf != true) { panel.SetActive(true); }
    }

    public void ResetPanel()
    {
        selectItemSlots.ForEach(ResetSlot);
        itemList.Clear();
    }

    public void ResetSlot(SelectedItemSlot x)
    {
        x.ResetSlot();
        x.gameObject.SetActive(false);
    }

    public void ClosePanel()
    {
        ResetPanel();
        panel.SetActive(false);
    }

    private void EquipItem(ItemClass i)
    {
        CharacterInventoryManager.Instance.EquipWeapon(i);
    }

    private void MintItem()
    {
        int count = itemList.Count;
        int[] itemIDs = new int[count];
        int[] itemAmount = new int[count];

        for(int i = 0; i < count; i++)
        {
            itemIDs[i] = itemList[i].TokenID;
            itemAmount[i] = itemList[i].Count;
        }
        //Check If Item Has Already Been Minted Or Reduce Minted Item Count
        StartCoroutine(StarkAPILink.MintItem("001", itemIDs, itemAmount, OnSuccess, OnError));
    }

    public void RemoveItem(int i)
    {
        itemList.RemoveAt(i);
    }

    private void OnSuccess(string json)
    {
        Debug.Log($"✅ Success:\n{json}");
        string itemNames = string.Join(", ", itemList.Select(i => $"{i.Name}"));
        notifPanel.ShowNotification($"The Item(s) {itemNames} Minted Successfuly");
    }

    private void OnError(string error)
    {
        Debug.LogError($"❌ Error:\n{error}");
    }
}
