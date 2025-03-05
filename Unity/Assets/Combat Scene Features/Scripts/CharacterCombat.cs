using UnityEngine;

public enum AttackType
{
    Light,
    Heavy
}

public enum ComboStatus
{
    Can,
    Cannot
}

public class CharacterCombat : MonoBehaviour
{
    CharacterManager characterManager;

    [Header("Status")]
    public bool canCombo;
    public AttackType attackType;
    
    [Header("Parameters")]
    public int damageModifier;
    public float currentRecovery;
    public AttackActions currentAction;

    [field: Header("Combat Character")]
    [SerializeField] private Transform WeaponHolder;
    [field: SerializeField] public CharacterCombatData CombatCharacter { get; private set; }

    [Header("Tools")]
    [SerializeField] private CharacterDamageCollider damageCollider;
    [field: SerializeField] public AttackActions[] LightActions { get; private set; }
    [field: SerializeField] public AttackActions[] HeavyActions { get; private set; }

    private void Awake()
    {
        characterManager = GetComponent<CharacterManager>();
    }

    private void Start()
    {
        PrepareActions();
    }

    public void AssignWeapon(CharacterDamageCollider weapon)
    {
        damageCollider = GetComponentInChildren<CharacterDamageCollider>();

        if(damageCollider  == null)
        {
            damageCollider = Instantiate(weapon, WeaponHolder);
            damageCollider.SetCharacter(characterManager, null);
        }
    }

    public void Combat_Update(float delta)
    {
        CharacterType type = characterManager.characterType;
        
        if (type == CharacterType.AI)
        {
            HandleRecoveryTimer(delta);
        }
        else if (type == CharacterType.Player)
        {
            Attack(characterManager.PlayerInput);
        }
    }

    public void EnableCollider()
    {
        damageCollider.SetColliderStatus(true);
    }

    public void DisableCollider()
    {
        damageCollider.SetColliderStatus(false);
    }

    public void SetComboStatus(ComboStatus status)
    {
        canCombo = (status == ComboStatus.Can) ? true : false;
    }

    private void HandleRecoveryTimer(float delta)
    {
        if (currentRecovery <= 0.0f)
        {
            currentRecovery = 0.0f;
            return;
        }

        if (characterManager.performingAction)
        {
            return;
        }

        currentRecovery -= delta;
    }

    private void Attack(InputManager input)
    {
        if(input.lightAttackInput != true && input.heavyAttackInput != true)
        {
            return;
        }

        if(input.lightAttackInput)
        {
            int random = Random.Range(0, LightActions.Length);
            currentAction = LightActions[random];
        }
        else if(input.heavyAttackInput)
        {
            int random = Random.Range(0, HeavyActions.Length);
            currentAction = HeavyActions[random];
        }
        if(currentAction != null) { currentAction.PerformAction(characterManager); }
        input.ResetInput();
    }

    private void PrepareActions()
    {
        CombatState combat = characterManager.Combat;

        for(int i = 0; i < LightActions.Length; i++)
        {
            LightActions[i] = Instantiate(LightActions[i]);
            LightActions[i].Initialize();
        }

        for (int i = 0; i < HeavyActions.Length; i++)
        {
            HeavyActions[i] = Instantiate(HeavyActions[i]);
            HeavyActions[i].Initialize();
        }
    }
}
