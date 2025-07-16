using UnityEngine;

[CreateAssetMenu(fileName = "Attack Action", menuName = "Attack Action")]
public class AttackActions : ScriptableObject
{
    private int attackHash;

    [Header("Attack Information")]
    public int damageValue;
    [SerializeField] private int enduranceCost;
    [SerializeField] public AttackType attackType;
    [SerializeField] private string attackAnimation;

    [field: Header("AI Parameters")]
    public int actionWeight;
    public float recoveryTime;
    [field: SerializeField] public AttackActions comboAction { get; private set; }
    [field: SerializeField] public Boundary angleBoundary { get; private set; }
    [field: SerializeField] public Boundary distanceBoundary { get; private set; }

    public void Initialize()
    {
        attackHash = Animator.StringToHash(attackAnimation);
    }

    public void PerformAction(bool canMirror, CharacterManager character)
    {
        CharacterCombat combat = character.CombatManager;

        if(character.StatsManager.currentEndurance <= enduranceCost)
        {
            combat.canCombo = false;
            return;
        }

        if(combat.canCombo == true)
        {
            HandleCombo(character, canMirror);
            return;
        }
        HandleAttack(character, canMirror);
    }

    private void HandleAttack(CharacterManager character, bool canMirror)
    {
        if (character.performingAction)
        {
            return;
        }
        character.isAttacking = true;
        character.CombatManager.attackType = attackType;

        character.StatsManager.ReduceEndurance(enduranceCost);

        bool shouldMirror = (Random.value > 0.75f && canMirror);
        character.AnimatorManagaer.PlayAttackAnimation(attackHash, true, shouldMirror);
    }

    private void HandleCombo(CharacterManager character, bool canMirror)
    {
        if (character.performingAction)
        {
            return;
        }
        CharacterCombat combat = character.CombatManager;

        combat.canCombo = false;
        combat.currentAction = comboAction;
        comboAction.HandleAttack(character, canMirror);  
    }
}
