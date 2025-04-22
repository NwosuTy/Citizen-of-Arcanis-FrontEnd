using UnityEngine;

public class PickableObject : MonoBehaviour, IInteractable
{
    [SerializeField] private Rigidbody rigidBody;
    [field: SerializeField] public ItemObjects ItemObject { get; private set; }
    [field: SerializeField] public WeaponManager weaponManager { get; private set; }

    [field: Header("Item Details")]
    [field: SerializeField] public int rewardRate { get; private set; }
    [field: SerializeField] public string ItemName { get; private set; }
    [field: SerializeField] public Sprite ItemImage { get; private set; }
    [field: SerializeField] public ItemType ItemType { get; private set; }

    public void Interact()
    {
        CharacterInventoryManager.Instance.HandleItemAddition(this);
        gameObject.SetActive(false);
    }

    public string GetInteractText()
    {
        return $"Pick Up {ItemName}";
    }

    public void RemoveRigidBody()
    {
        if (rigidBody == null)
        {
            return;
        }
        Destroy(rigidBody);
    }

    public void SetPhysicsSystem(bool status)
    {
        if(rigidBody == null)
        {
            return;
        }
        rigidBody.useGravity = status;
        rigidBody.isKinematic = !status;
        rigidBody.constraints = (status) ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
    }
}
