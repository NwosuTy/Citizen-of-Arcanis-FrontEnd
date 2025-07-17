using UnityEngine;
using System.Collections;

public class RewardSystem : MonoBehaviour
{
    private ItemBox selectedBox;
    private PickableObject pickedItem;

    public bool hasFinished { get; private set; }
    public RewardBox rewardBox {  get; private set; }

    [SerializeField] private int rate; //Can Be Used To Guage What Item Box To Reward
    [SerializeField] private ItemBox[] itemBoxes;
    [SerializeField] private NotificationPanel notifPanel;

    private void Start()
    {
        hasFinished = false;
        rewardBox = new RewardBox();
        DuelManager.Instance.SetRewardSystem(this);
    }

    public IEnumerator HandleFillUpRewardBox(DuelState duelState)
    {
        hasFinished = false;
        bool isDraw = (duelState == DuelState.Draw);
        selectedBox = LootSystem.GetRandomBox(rate, selectedBox, itemBoxes);
        int itemCount = (isDraw) ? 2 : Random.Range(selectedBox.RewardBoxSize.minValue, selectedBox.RewardBoxSize.maxValue);

        if(rewardBox.itemsList.Count < itemCount)
        {
            while (rewardBox.itemsList.Count < itemCount - 1)
            {
                FillRewardBox(ItemType.Collectible);
            }
            FillRewardBox(ItemType.Currency);
        }

        yield return new WaitUntil(() => rewardBox.itemsList.Count >= itemCount);
        rewardBox.CleanBox();

        yield return new WaitUntil(() => rewardBox.FinishedCleaning);

        string notification = $"Congratulations You Have Won {rewardBox.boxname}";
        notifPanel.ShowNotification(notification, selectedBox, rewardBox);

        yield return new WaitUntil(() => notifPanel.gameObject.activeSelf != true);
        hasFinished = true;
    }

    private void FillRewardBox(ItemType itemType)
    {
        int min, max;
        PickableObject[] items;

        if (itemType == ItemType.Currency)
        {
            items = selectedBox.CurrencyItems;
            min = selectedBox.CurrencyCountSize.minValue; 
            max = selectedBox.CurrencyCountSize.maxValue;
        }
        else
        {
            items = selectedBox.CollectibleItems;
            min = selectedBox.CollectibleCountSize.minValue;
            max = selectedBox.CollectibleCountSize.maxValue;
        }

        int random = Random.Range(min, max);
        pickedItem = LootSystem.GetRandomItem(pickedItem, items, selectedBox);

        ItemClass reward = new(random, pickedItem.ItemObject.objectPrefab);
        rewardBox.FillUpBox(selectedBox, reward);
    }
}
