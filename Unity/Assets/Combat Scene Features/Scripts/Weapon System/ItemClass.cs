using UnityEngine;

public class ItemClass : MonoBehaviour, IInteractable
{
    [SerializeField] private Rigidbody rigidBody;

    [field: Header("Item Details")]
    [field: SerializeField] public string ItemName { get; private set; }
    [field: SerializeField] public Sprite ItemImage { get; private set; }
    [field: SerializeField] public ItemType ItemType { get; private set; }

    public void Interact()
    {
        InventoryManagerPanel_UI.Instance.InventoryManager.HandleItemAddition(this);
        gameObject.SetActive(false);
    }

    public string GetInteractText()
    {
        return $"Pick Up {ItemName}";
    }

    public void SetPhysicsSystem(bool status)
    {
        rigidBody.useGravity = status;
        rigidBody.isKinematic = status;
        rigidBody.constraints = (status) ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
    }
}
