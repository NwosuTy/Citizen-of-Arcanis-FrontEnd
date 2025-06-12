using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LootBox : MonoBehaviour, IInteractable
{
    private WaitForSeconds rewardDelay;
    private WaitForSeconds destroyDelay;
    private List<ItemClass> itemClasses = new();

    [Header("Parameters")]
    [SerializeField] private float f_Rdelay;
    [SerializeField] private float f_Ddelay;
    [SerializeField] private List<PickableObject> objList = new();

    [Header("UI Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private ContentSlotUI[] contentArray;

    private void Start()
    {
        rewardDelay = new WaitForSeconds(f_Rdelay);
        destroyDelay = new WaitForSeconds(f_Ddelay);
    }

    public string GetInteractText()
    {
        return gameObject.name;
    }

    public void Interact()
    {
        StartCoroutine(DisplayLoot());
    }

    public void AddItem(PickableObject obj)
    {
        objList.Add(obj);
    }

    private IEnumerator DisplayLoot()
    {
        DisplayUIContent();
        yield return rewardDelay;

        panel.SetActive(false);
        itemClasses.ForEach(x => CharacterInventoryManager.Instance.AddUnExistingItem(x));
        yield return destroyDelay;
        Destroy(gameObject);
    }

    private void DisplayUIContent()
    {
        for(int i = 0; i < objList.Count; i++)
        {
            PickableObject pick = objList[i];
            int rnd = RandomItemCount(pick.ItemType);

            itemClasses.Add(new(rnd, pick));
            contentArray[i].DisplayContent(rnd, pick);
        }
        panel.SetActive(true);
    }

    private int RandomItemCount(ItemType itemType)
    {
        if(itemType == ItemType.Currency)
        {
            int rnd = Random.Range(10, 40);
            return ((rnd - 1) / 10 + 1) * 10;
        }
        return Random.Range(1, 4);
    }
}
