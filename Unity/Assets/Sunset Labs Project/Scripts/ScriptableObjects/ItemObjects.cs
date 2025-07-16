using UnityEngine;

[CreateAssetMenu(fileName = "Item Object", menuName = "ScriptableObject/ItemObject")]
public class ItemObjects : ScriptableObject
{
    [field: SerializeField] public PickableObject objectPrefab { get; private set; }
}
