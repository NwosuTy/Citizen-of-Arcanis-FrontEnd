using UnityEngine;

[CreateAssetMenu(fileName = "Reward Box", menuName = "ScriptableObject/ItemBox")]
public class ItemBox : ScriptableObject
{
    [field: Header("Item Box Information")]
    [field: SerializeField] public string boxName { get; private set; }
    [field: SerializeField] public ItemBoxTier boxTier { get; private set; }

    [field: Header("Possible Rewards")]
    [field: SerializeField] public PickableObject[] CurrencyItems { get; private set; }
    [field: SerializeField] public PickableObject[] CollectibleItems { get; private set; }

    [field: Header("Gacha System")]
    [field: SerializeField] public int MaxRate { get; private set; }
    [field: SerializeField] public BoundInt RewardBoxSize { get; private set; }
    [field: SerializeField] public BoundInt CurrencyCountSize { get; private set; }
    [field: SerializeField] public BoundInt CollectibleCountSize { get; private set; }
}
