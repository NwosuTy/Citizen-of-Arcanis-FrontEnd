using UnityEngine;

public class PlaceHolderCombatScript : MonoBehaviour
{
    //Input Parameters
    private int clickCount = 0;
    private bool lightAttackInput;
    private bool heavyAttackInput;
    private float lastClickTime = 0.0f;
    [SerializeField] private float doubleClickTime = 0.3f;

    //Animator Parameters
    private Animator animator;
    private int performActionHash;
    private bool hasAssignedWeapon;
    public WeaponManager weaponManager { get; private set; }

    [Header("Status")]
    public bool isAttacking;
    public bool performingAction;
    public AttackType attackType;

    [Header("Parameters")]
    public int damageModifier;
    public AttackActions currentAction;
    [SerializeField] private Transform GunWeaponHolder;
    [SerializeField] private Transform MeleeWeaponHolder;
    [SerializeField] private Transform crossHairTransform;

    [Header("Attack Actions")]
    [SerializeField] private AttackActions[] lightActions;
    [SerializeField] private AttackActions[] heavyActions;

    [field: Header("Combat Parameters")]
    [SerializeField] private CharacterDamageCollider handCollider;
    [SerializeField] private CharacterDamageCollider damageCollider;
    [field: SerializeField] public CharacterCombatData CombatCharacter { get; private set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        PrepareActions();
        CombatManager combatManager = CombatManager.Instance;
        CharacterInventoryManager.Instance.SetCharacterManager(null, this);
        if (combatManager != null) { combatManager.AssignPlayer(CombatCharacter.characterManager); }
    }

    private void Update()
    {
        InventoryManagerPanel_UI inventory = CharacterInventoryManager.Instance.Panel;
        if(DialogueManager.Instance.dialogueIsPlaying) { return; }
        if(inventory.isMouseOverPanel) { return; }

        HandleInput();
        CombatManager combatManager = CombatManager.Instance;
        bool hasGun = (weaponManager != null && weaponManager.type == WeaponType.Gun);
        if (hasGun)
        {
            weaponManager.WeaponManager_Update(Time.deltaTime);
        }

        if (damageCollider == null)
        {
            //Would Move To When Player Picks Up Weapon Function
            damageCollider = GetComponentInChildren<CharacterDamageCollider>();
            if(damageCollider != null) { damageCollider.SetCharacter(null, this); }
        }

        HandleAttackAction();
        combatManager.CrossHairImg.gameObject.SetActive(hasGun);
        combatManager.FreeLookCamera.gameObject.SetActive(hasGun);
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

    public void SetCrossHair(Transform crossHair)
    {
        crossHairTransform = crossHair;
    }

    #region Input

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            clickCount++;
            if (clickCount == 1)
            {
                lastClickTime = Time.time;
                Invoke(nameof(SingleClickAction), doubleClickTime);
            }
            else if (clickCount == 2 && (Time.time - lastClickTime) < doubleClickTime)
            {
                CancelInvoke(nameof(SingleClickAction));
                heavyAttackInput = true;
                clickCount = 0;
            }
        }

        if (Time.time - lastClickTime > doubleClickTime)
        {
            clickCount = 0;
        }
    }

    private void ResetInput()
    {
        lightAttackInput = false;
        heavyAttackInput = false;
    }

    private void SingleClickAction()
    {
        if (clickCount == 1)
        {
            lightAttackInput = true;
        }
        clickCount = 0;
    }
    #endregion


    #region Attack Actions
    
    private void PrepareActions()
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

    private void HandleAttackAction()
    {
        isAttacking = (lightAttackInput == true || heavyAttackInput == true);
        if (isAttacking != true || CharacterInventoryManager.Instance.Panel.isMouseOverPanel)
        {
            return;
        }

        if (weaponManager == null || weaponManager.type == WeaponType.Melee)
        {
            if (lightAttackInput)
            {
                int random = Random.Range(0, lightActions.Length);
                currentAction = lightActions[random];
            }
            else
            {
                int random = Random.Range(0, heavyActions.Length);
                currentAction = heavyActions[random];
            }
            currentAction.PerformAction(this);
            ResetInput();
            return;
        }
        HandleWeaponAction(Time.deltaTime);
        ResetInput();
    }

    public void HandleWeaponAction(float delta)
    {
        Vector3 targePosition = crossHairTransform.position;
        weaponManager.HandleAction(targePosition, null, this);
    }

    public void PlayTargetAnimation(int targetAnimation, bool performingAction, float transitionDuration = 0.2f, bool canRotate = true)
    {
        Animator animator = this.animator;

        animator.applyRootMotion = performingAction;

        animator.SetBool(performActionHash, performingAction);
        animator.CrossFade(targetAnimation, transitionDuration);
    }
    #endregion
}
