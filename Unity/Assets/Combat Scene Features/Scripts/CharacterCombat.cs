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

    private Vector3 targetPosition;
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
        bool hasGun = HasGun();
        targetPosition = GetTargetPosition();
        CharacterType type = characterManager.characterType;

        if (hasGun)
        {
            weaponManager.WeaponManager_Update(targetPosition, characterManager, delta);
        }

        if (type == CharacterType.AI)
        {
            HandleRecoveryTimer(delta);
        }
        else if (type == CharacterType.Player)
        {
            Attack(characterManager.PlayerInput);
            characterManager.CameraController.EnableShooterGraphics(hasGun);
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

    private void Attack(InputManager input)
    {
        characterManager.isAttacking = (input.lightAttackInput == true || input.heavyAttackInput == true);
        if(characterManager.isAttacking != true || CharacterInventoryManager.Instance.Panel.isMouseOverPanel)
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
        HandleWeaponAction();
    }

    public void HandleWeaponAction()
    {
        weaponManager.HandleAction(targetPosition, characterManager);
    }    

    public bool HasGun()
    {
        return weaponManager != null && weaponManager.type == WeaponType.Gun;
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

    public void ResetPerformAttack()
    {
        characterManager.Attack.ResetPerformAttack();
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
