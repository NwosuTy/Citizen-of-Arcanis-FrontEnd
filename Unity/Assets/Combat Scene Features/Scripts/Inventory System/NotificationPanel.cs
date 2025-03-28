using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class NotificationPanel : MonoBehaviour
{
    private WaitForSeconds waitForSeconds;

    [Header("Panel UI")]
    [SerializeField] private float disableDelay;
    [SerializeField] private Transform contentDrawer;
    [SerializeField] private List<ContentSlotUI> contentList;
    [SerializeField] private TextMeshProUGUI notificationText;

    private void Awake()
    {
        waitForSeconds = new WaitForSeconds(disableDelay);
        notificationText.gameObject.SetActive(false);
    }

    public void ShowNotification(string text, ItemBox itemBox, RewardBox rewardBox)
    {
        DisableContents();
        gameObject.SetActive(true);
        StartCoroutine(DisplayRewards(text, itemBox, rewardBox));
    }

    public void ShowNotification(string notification)
    {
        gameObject.SetActive(true);
        notificationText.gameObject.SetActive(true);
        StartCoroutine(Notification(notification));
    }

    private IEnumerator DisplayRewards(string text, ItemBox itemBox, RewardBox rewardBox)
    {
        notificationText.gameObject.SetActive(false);

        for(int i = 0; i < rewardBox.rewardBoxItems.Count; i++)
        {
            ContentSlotUI content = contentList[i];
            Reward reward = rewardBox.rewardBoxItems[i];

            content.gameObject.SetActive(true);
            ItemType itemType = reward.itemClass.ItemType;

            if (itemType == ItemType.Currency)
            { 
                content.Initialize(reward, itemBox.CurrencyItems);
            }
            else {content.Initialize(reward, itemBox.CollectibleItems);}

            StartCoroutine(content.RevealRandomItem());
            yield return new WaitUntil(() => content.hasRevealed);
        }
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(Notification(text));
    }

    private IEnumerator Notification(string text)
    {
        notificationText.text = text;
        notificationText.gameObject.SetActive(true);

        yield return waitForSeconds;

        notificationText.text = "";
        gameObject.SetActive(false);
        DisableContents();
    }

    private void DisableContents()
    {
        foreach(ContentSlotUI content in contentList)
        {
            content.gameObject.SetActive(false);
        }
    }
}
