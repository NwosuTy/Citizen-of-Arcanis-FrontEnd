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
    [SerializeField] private TMP_InputField playerIdInput;
    [SerializeField] private TMP_Dropdown itemTypeDropdown;

    private void Start()
    {
        StarkAPILink.ApiBaseURL = "http://localhost:3000";

        getPlayerBtn.onClick.AddListener(() =>
        {
            StartCoroutine(StarkAPILink.Get(playerIdInput.text, true, OnSuccess, OnError));
        });

        getInventoryBtn.onClick.AddListener(() =>
        {
            StartCoroutine(StarkAPILink.Get(itemIdInput.text, false, OnSuccess, OnError));
        });

        mintBtn.onClick.AddListener(() =>
        {
            ItemType selectedItem = (ItemType)itemTypeDropdown.value;
            StartCoroutine(StarkAPILink.MintItem(playerIdInput.text, selectedItem, OnSuccess, OnError));
        });

        useItemBtn.onClick.AddListener(() =>
        {
            StartCoroutine(StarkAPILink.UseItem(playerIdInput.text, itemIdInput.text, OnSuccess, OnError));
        });
    }

    private void OnSuccess(string json)
    {
        Debug.Log("Success: " + json);
        resultText.text = $"✅ Success:\n{json}";
    }

    private void OnError(string error)
    {
        Debug.LogError("Error: " + error);
        resultText.text = $"❌ Error:\n{error}";
    }
}
