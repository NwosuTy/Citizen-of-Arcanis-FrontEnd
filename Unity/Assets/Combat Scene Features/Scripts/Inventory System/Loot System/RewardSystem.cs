using UnityEngine;
using System.Collections;

public class RewardSystem : MonoBehaviour
{
    private ItemBox selectedBox;
    private ItemClass pickedItem;

    public bool hasFinished { get; private set; }
    public RewardBox rewardBox {  get; private set; }

    [SerializeField] private int rate; //Can Be Used To Guage What Item Box To Reward
    [SerializeField] private ItemBox[] itemBoxes;
    [SerializeField] private NotificationPanel notifPanel;

    private void Start()
    {
        hasFinished = false;
        rewardBox = new RewardBox();
        DuelManager.Instance.AddRewardSystem(this);
    }

    public IEnumerator HandleFillUpRewardBox(DuelState duelState)
    {
        hasFinished = false;
        bool isDraw = (duelState == DuelState.Draw);
        selectedBox = LootSystem.GetRandomBox(rate, selectedBox, itemBoxes);
        int itemCount = (isDraw) ? 2 : Random.Range(selectedBox.RewardBoxSize.minValue, selectedBox.RewardBoxSize.maxValue);

        if(rewardBox.rewardBoxItems.Count < itemCount)
        {
            while (rewardBox.rewardBoxItems.Count < itemCount - 1)
            {
                FillRewardBox(ItemType.Collectible);
            }
            FillRewardBox(ItemType.Currency);
        }

        yield return new WaitUntil(() => rewardBox.rewardBoxItems.Count >= itemCount);
        rewardBox.CleanBox();

        yield return new WaitUntil(() => rewardBox.finishedCleaning);

        string notification = $"Congratulations You Have Won {rewardBox.boxname}";
        notifPanel.ShowNotification(notification, selectedBox, rewardBox);

        yield return new WaitUntil(() => notifPanel.gameObject.activeSelf != true);
        hasFinished = true;
    }

    private void FillRewardBox(ItemType itemType)
    {
        int min, max;
        ItemClass[] items;

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

        Reward reward = new(random, pickedItem);
        rewardBox.FillUpBox(selectedBox, reward);
    }
}
