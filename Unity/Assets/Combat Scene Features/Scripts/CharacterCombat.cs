using UnityEngine;
using UnityEngine.TextCore.Text;

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
    [SerializeField] private Transform GunWeaponHolder;
    [SerializeField] private Transform MeleeWeaponHolder;
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

    public Transform WeaponHolder(WeaponManager weapon)
    {
        if(weapon == null || weapon.type == WeaponType.Melee)
        {
            return MeleeWeaponHolder;
        }
        return GunWeaponHolder;
    }

    public Vector3 GetTargetPosition()
    {
        if(characterManager.characterType == CharacterType.AI)
        {
            Vector3 target = characterManager.PositionOfTarget + targetOffset;
            target += Random.insideUnitSphere * inaccuracy;
            return target;
        }
        return crossHairTransform.position;
    }

    public void SetCrossHair(Transform crossHair)
    {
        crossHairTransform = crossHair;
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
        if(weaponManager != null)
        {
            weaponManager.WeaponManager_Update(characterManager);
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
        characterManager.isAttacking = (input.lightAttackInput == true || input.heavyAttackInput == true);
        if(characterManager.isAttacking != true)
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
            input.ResetInput();
            return;
        }
        HandleWeaponAction(delta);
        input.ResetInput();
    }

    public void HandleWeaponAction(float delta)
    {
        Vector3 targePosition = GetTargetPosition();
        weaponManager.HandleAction(delta, targePosition, characterManager);
    }

    public void EnableCollider()
    {
        if(weaponManager == null)
        {
            return;
        }
        weaponManager.DamageCollider.SetColliderStatus(true);
    }

    public void DisableCollider()
    {
        if (weaponManager == null)
        {
            return;
        }
        weaponManager.DamageCollider.SetColliderStatus(false);
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
