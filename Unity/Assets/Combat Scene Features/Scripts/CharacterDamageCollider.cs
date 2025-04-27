using UnityEngine;
using System.Collections.Generic;

public class CharacterDamageCollider : MonoBehaviour
{
    private CharacterManager characterCausingDamage;
    private List<CharacterManager> charactersBeingDamaged = new();

    [Header("Parameters")]
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private Collider damageCollider;
    
    public void SetCharacter(CharacterManager cm)
    {
        characterCausingDamage = cm;

        rigidBody.isKinematic = true;
        damageCollider.enabled = false;

        damageCollider.isTrigger = true;
        rigidBody.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void SetColliderStatus(bool status)
    {
        if(status == false)
        {
            charactersBeingDamaged.Clear();
        }
        rigidBody.isKinematic = !status;
        damageCollider.enabled = status;
    }

    private void OnTriggerEnter(Collider other)
    {
        CharacterManager damaged = other.GetComponentInParent<CharacterManager>();

        if(damaged == null)
        {
            return;
        }
        bool sameObjectCC = (characterCausingDamage != null && characterCausingDamage == damaged);

        if(sameObjectCC)
        {
            return;
        }
        HandleDamage(damaged);
    }

    private void HandleDamage(CharacterManager damaged)
    {
        if (damaged.isDead == true)
        {
            return;
        }

        if (charactersBeingDamaged.Contains(damaged))
        {
            return;
        }

        charactersBeingDamaged.Add(damaged);

        CharacterCombat combat = characterCausingDamage.CombatManager;
        AttackActions currentAttack = combat.currentAction;
        int damage = combat.damageModifier * currentAttack.damageValue;
        damaged.StatsManager.TakeDamage(damage, currentAttack.attackType);
    }
}
