using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ContentSlotUI : MonoBehaviour
{
    private float elapsed;
    private ItemClass selectedItem;
    private WaitForSeconds imageShuffleDuration;

    private Reward finalReward;
    public bool hasRevealed {get; private set;}
    private List<int> unexcludedItems = new List<int>();
    private List<ItemClass> itemsList = new List<ItemClass>();

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

    public void Initialize(Reward reward, ItemClass[] itemArray)
    {
        itemsList.Clear();
        hasRevealed = false;

        finalReward = reward;
        itemsList = new List<ItemClass>(itemArray);
    }

    private void DisplayContent(int count, ItemClass item)
    {
        itemName.text = item.ItemName;
        itemIcon.sprite = item.ItemImage;
        itemCountUI.text = count.ToString("00");
    }

    private ItemClass RandomIcon(ItemClass exclude)
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
        DisplayContent(finalReward.itemCount, finalReward.itemClass);
        hasRevealed = true;
    }
}
