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
    public WeaponManager weaponManager { get; private set; }

    [Header("Combat Status")]
    public bool canCombo;
    public AttackType attackType;

    [Header("Parameters")]
    public int damageModifier;
    public float currentRecovery;
    [SerializeField] private Transform crossHairTransform;

    [Header("Gun Parameters")]
    [SerializeField] private float inaccuracy;
    [SerializeField] private Vector3 targetOffset;

    [Header("Melee Parameters")]
    [SerializeField] private AttackActions[] lightActions;
    [SerializeField] private AttackActions[] heavyActions;

    [field: Header("Combat Character")]
    public AttackActions currentAction;
    [field: SerializeField] public Transform WeaponHolder { get; private set; }
    [field: SerializeField] public CharacterCombatData CombatCharacter { get; private set; }

    private void Awake()
    {
        characterManager = GetComponent<CharacterManager>();
    }

    private void Start()
    {
        InitializeAttackActions();
    }

    public void AssignWeapon(WeaponManager weapon)
    {
        weaponManager = weapon;
    }

    public Vector3 GetTargetPosition()
    {
        if(characterManager.characterType == CharacterType.AI)
        {
            Vector3 target = characterManager.PositionOfTarget + targetOffset;
            target += Random.insideUnitSphere * inaccuracy;
        }
        return crossHairTransform.position;
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
            Attack(delta, characterManager.PlayerInput);
        }
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

    public void SetComboStatus(ComboStatus status)
    {
        canCombo = (status == ComboStatus.Can);
    }

    private void Attack(float delta, InputManager input)
    {
        if (input.lightAttackInput != true && input.heavyAttackInput != true)
        {
            return;
        }

        if (weaponManager == null || weaponManager.type == WeaponType.Melee)
        {
            if (input.lightAttackInput)
            {
                int random = Random.Range(0, lightActions.Length);
                currentAction = lightActions[random];
            }
            else
            {
                int random = Random.Range(0, heavyActions.Length);
                currentAction = heavyActions[random];
            }
            currentAction.PerformAction(characterManager);
            return;
        }

        
        Vector3 targePosition = GetTargetPosition();
        weaponManager.HandleAction(delta, targePosition, characterManager);
        input.ResetInput();
    }

    private void InitializeAttackActions()
    {
        for (int i = 0; i < lightActions.Length; i++)
        {
            lightActions[i] = Instantiate(lightActions[i]);
            lightActions[i].Initialize();
        }

        for (int i = 0; i < heavyActions.Length; i++)
        {
            heavyActions[i] = Instantiate(heavyActions[i]);
            heavyActions[i].Initialize();
        }
    }
}
