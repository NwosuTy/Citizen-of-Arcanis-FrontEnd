using UnityEngine;
using System.Collections.Generic;

public class CharacterDamageCollider : MonoBehaviour
{
    private CharacterManager characterCausingDamage;

    private Collider[] collidersArray;
    private List<CharacterManager> charactersBeingDamaged = new();

    [Header("Parameters")]
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private Collider damageCollider;

    [Header("No Rigidbody Parametes")]
    [SerializeField] private float attackRadius;
    [SerializeField] private LayerMask enemyLayerMask;

    public void SetCharacter(CharacterManager cm)
    {
        characterCausingDamage = cm;
        if(rigidBody != null)
        {
            rigidBody.isKinematic = true;
            damageCollider.enabled = false;

            damageCollider.isTrigger = true;
            rigidBody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void Start()
    {
        if(rigidBody == null)
        {
            collidersArray = new Collider[10];
        }
    }

    public void SetColliderStatus(bool status)
    {
        if(status == false)
        {
            charactersBeingDamaged.Clear();
        }

        if (rigidBody == null)
        {
            NoRigidBodyAttack();
            return;
        }
        rigidBody.isKinematic = !status;
        damageCollider.enabled = status;
    }

    public void SetParameters(float radius, LayerMask layerMask)
    {
        attackRadius = radius;
        enemyLayerMask = layerMask;
    }

    private void NoRigidBodyAttack()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, attackRadius, collidersArray, enemyLayerMask);
        for(int i = 0; i <  count; i++)
        {
            DamgeEnemy(collidersArray[i]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if(rigidBody == null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRadius);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        DamgeEnemy(other);
    }

    private void DamgeEnemy(Collider other)
    {
        CharacterManager damaged = other.GetComponentInParent<CharacterManager>();

        if (damaged == null)
        {
            return;
        }
        bool sameObjectCC = (characterCausingDamage != null && characterCausingDamage == damaged);

        if (sameObjectCC)
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
