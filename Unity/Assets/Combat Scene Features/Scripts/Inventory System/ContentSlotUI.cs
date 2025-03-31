using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ContentSlotUI : MonoBehaviour
{
    private float elapsed;
    private PickableObject selectedItem;
    private WaitForSeconds imageShuffleDuration;

    private ItemClass finalReward;
    public bool hasRevealed {get; private set;}
    private List<int> unexcludedItems = new List<int>();
    private List<PickableObject> itemsList = new List<PickableObject>();

    [Header("Item Properties")]
    [SerializeField] private ItemType itemType;
    [SerializeField] private float shuffleDuration;

    [Header("Slot UI Parameters")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemCountUI;

    private void Awake()
    {
        imageShuffleDuration = new WaitForSeconds(shuffleDuration);
    }

    public void Initialize(ItemClass reward, PickableObject[] itemArray)
    {
        itemsList.Clear();
        hasRevealed = false;

        finalReward = reward;
        itemsList = new List<PickableObject>(itemArray);
    }

    private void DisplayContent(int count, PickableObject item)
    {
        itemName.text = item.ItemName;
        itemIcon.sprite = item.ItemImage;
        itemCountUI.text = count.ToString("00");
    }

    private PickableObject RandomIcon(PickableObject exclude)
    {
        unexcludedItems.Clear();
        for(int i = 0; i < itemsList.Count; i++)
        {
            if(itemsList[i] == exclude)
            {
                continue;
            }
            unexcludedItems.Add(i);
        }

        int random = Random.Range(0, unexcludedItems.Count);
        int index = unexcludedItems[random];
        return itemsList[index];
    }

    public IEnumerator RevealRandomItem()
    {
        itemIcon.color = Color.white;

        elapsed = 0f;
        while(elapsed < shuffleDuration)
        {
            int count = Random.Range(1, 26);
            selectedItem = RandomIcon(selectedItem);
            DisplayContent(count, selectedItem);

            yield return new WaitForSeconds(0.05f);
            elapsed += Time.deltaTime;
        }
        DisplayContent(finalReward.itemCount, finalReward.pickedObj);
        hasRevealed = true;
    }
}
