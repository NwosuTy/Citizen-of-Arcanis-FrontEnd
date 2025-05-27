using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StarkAPITestMonoBehavior : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button mintBtn;
    [SerializeField] private Button useItemBtn;
    [SerializeField] private Button getPlayerBtn;
    [SerializeField] private Button getInventoryBtn;

    [Header("UI References")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_InputField itemIdInput;
    [SerializeField] private TMP_InputField itemAmntInput;
    [SerializeField] private TMP_InputField playerIdInput;
    [SerializeField] private TMP_Dropdown itemTypeDropdown;

    private void Start()
    {
        //getPlayerBtn.onClick.AddListener(() =>
        //{
        //    StartCoroutine(StarkAPILink.Get(playerIdInput.text, true, OnSuccess, OnError));
        //});

        //getInventoryBtn.onClick.AddListener(() =>
        //{
        //    StartCoroutine(StarkAPILink.Get(itemIdInput.text, false, OnSuccess, OnError));
        //});

        //mintBtn.onClick.AddListener(() =>
        //{
        //    ItemType selectedItem = (ItemType)itemTypeDropdown.value;
        //    StartCoroutine(StarkAPILink.MintItem(playerIdInput.text, selectedItem, OnSuccess, OnError));
        //});

        //useItemBtn.onClick.AddListener(() =>
        //{
        //    int amount = int.TryParse(itemAmntInput.text, out int result) ? result : 1;
        //    StartCoroutine(StarkAPILink.UseItem(playerIdInput.text, itemIdInput.text, amount, OnSuccess, OnError));
        //});
    }
}
