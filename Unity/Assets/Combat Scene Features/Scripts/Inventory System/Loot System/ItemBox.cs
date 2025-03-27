using UnityEngine;

[CreateAssetMenu(fileName = "Reward Box", menuName = "ScriptableObject/ItemBox")]
public class ItemBox : ScriptableObject
{
    [fiels: Header("Item Box Information")]
    [field: SerializeField] public string boxName { get; private set; }
    [field: SerializeField] public ItemBoxTier boxTier { get; private set; }

    [field: Header("Possible Rewards")]
    [field: SerializeField] public ItemClass[] CurrencyItems { get; private set; }
    [field: SerializeField] public ItemClass[] CollectibleItems { get; private set; }

    [field: Header("Gacha System")]
    [field: SerializeField] public int MaxRate { get; private set; }
    [field: SerializeField] public BoundInt RewardBoxSize { get; private set; }
    [field: SerializeField] public BoundInt CurrencyCountSize { get; private set; }
    [field: SerializeField] public BoundInt CollectibleCountSize { get; private set; }
}
