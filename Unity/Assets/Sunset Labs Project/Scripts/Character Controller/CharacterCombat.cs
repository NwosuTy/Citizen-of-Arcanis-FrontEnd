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

    private bool hasHashed;
    private int shouldMirrorHash;
    private Vector3 targetPosition;
    public WeaponManager CurrentWeapon { get; private set; }

    [Header("Combat Status")]
    public bool canCombo;
    public AttackType attackType;

    [Header("Parameters")]
    public int damageModifier;
    public float currentRecovery;
    [SerializeField] private bool mirrorAttack;
    [SerializeField] private Transform crossHairTransform;

    [Header("Gun Parameters")]
    [SerializeField] private float inaccuracy;
    [SerializeField] private Vector3 targetOffset;

    [Header("Melee Parameters")]
    [SerializeField] private AttackActions[] lightActions;
    [SerializeField] private AttackActions[] heavyActions;
    [SerializeField] private CharacterDamageCollider[] damageColliders;

    [field: Header("Combat Character")]
    public AttackActions currentAction;
    [SerializeField] private Transform GunWeaponHolder;
    [SerializeField] private Transform MeleeWeaponHolder;
    [field: SerializeField] public CharacterCombatData CombatCharacter { get; private set; }

    private void Awake()
    {
        characterManager = GetComponent<CharacterManager>();
    }

    private void OnEnable()
    {
        if (hasHashed == true)
        {
            return;
        }

        InitializeAttackActions();
        foreach (var c in damageColliders)
        {
            c.SetCharacter(characterManager);
        }
        shouldMirrorHash = Animator.StringToHash("shouldMirror");
    }

    private void OnDisable()
    {
        if (hasHashed == true)
        {
            hasHashed = false;
        }
    }

    public void AssignWeapon(WeaponManager weapon)
    {
        CurrentWeapon = weapon;
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
        if (crossHairTransform != null)
        {
            targetPosition = GetTargetPosition();
        }
        CharacterType type = characterManager.characterType;

        if (hasGun)
        {
            CurrentWeapon.WeaponManager_Update(targetPosition, characterManager, delta);
        }
        if (type == CharacterType.AI)
        {
            HandleRecoveryTimer(delta);
        }
        else if (type == CharacterType.Player)
        {
            Attack(characterManager.PlayerInput);
            characterManager.CameraController.EnableShooterGraphics(hasGun, characterManager.isLockedIn, delta);
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

    private bool DoNotAttack()
    {
        if (DialogueManager.Instance != null)
        {
            return DialogueManager.Instance.dialogueIsPlaying;
        }
        if (CharacterInventoryManager.Instance != null)
        {
            return CharacterInventoryManager.Instance.Panel.isMouseOverPanel;
        }
        return false;
    }

    private void Attack(InputManager input)
    {
        characterManager.isAttacking = (input.lightAttackInput == true || input.heavyAttackInput == true);

        bool cantAttack = DoNotAttack();
        if(characterManager.isAttacking != true || cantAttack)
        {
            return;
        }

        bool noWeapon = (CurrentWeapon == null);
        if (noWeapon || CurrentWeapon.type == WeaponType.Melee)
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
            currentAction.PerformAction(noWeapon, characterManager);
            return;
        }
        HandleWeaponAction();
    }

    public void HandleWeaponAction()
    {
        CurrentWeapon.HandleAction(targetPosition, characterManager);
    }    

    public bool HasGun()
    {
        return CurrentWeapon != null && CurrentWeapon.type == WeaponType.Gun;
    }

    public void SetMirrorStatus(bool status, Animator animator)
    {
        mirrorAttack = status;
        animator.SetBool(shouldMirrorHash, mirrorAttack);
    }

    public void EnableCollider(int colliderIndex)
    {
        if (HasGun())
        {
            return;
        }
        int index = (mirrorAttack) ? colliderIndex + 1 : colliderIndex;
        var damage = (CurrentWeapon == null) ? damageColliders[index] : CurrentWeapon.DamageCollider;
        damage.SetColliderStatus(true);
    }

    public void DisableCollider(int colliderIndex)
    {
        if (HasGun())
        {
            return;
        }
        int index = (mirrorAttack) ? colliderIndex + 1 : colliderIndex;
        var damage = (CurrentWeapon == null) ? damageColliders[index] : CurrentWeapon.DamageCollider;
        damage.SetColliderStatus(false);
    }

    public void ResetPerformAttack()
    {
        characterManager.Attack.ResetPerformAttack();
    }

    public void SetDuellingCharacter()
    {
        CombatManager.Instance.AssignPlayer(CombatCharacter.characterManager);
    }

    public void SetDamageColliders()
    {
        GameObject newObject = new();

        CharacterDamageCollider leftLeg = GetDamageCollider("Ball_L", newObject);
        CharacterDamageCollider rightLeg = GetDamageCollider("Ball_R", newObject);
        CharacterDamageCollider leftHand = GetDamageCollider("Hand_L", newObject);
        CharacterDamageCollider rightHand = GetDamageCollider("Hand_R", newObject);

        CreateHurtBox(newObject, 11);
        damageColliders = new[]{ leftHand, rightHand, leftLeg, rightLeg };

        DestroyImmediate(newObject);
    }

    private CharacterDamageCollider GetDamageCollider(string name, GameObject go)
    {
        LayerMask layer = 11;
        string colliderName = name + " Damage Collider";
        Transform parent = GameObjectFinder.FindChildRecursively(transform, name);

        if(GameObjectFinder.TryFindChildRecursively(parent, colliderName, out Transform t))
        {
            DestroyImmediate(t.gameObject);
        }
        GameObject gameObject = Instantiate(go, parent);

        gameObject.layer = layer;
        gameObject.name = colliderName;
        gameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        CharacterDamageCollider damageCollider = gameObject.AddComponent<CharacterDamageCollider>();

        damageCollider.SetParameters(0.175f, LayerMask.GetMask("Damage Layer"));
        return damageCollider;
    }

    private void CreateHurtBox(GameObject go, LayerMask layer)
    {
        Transform t;
        string objectName = "Body Damage Collider";
        if (GameObjectFinder.TryFindChildRecursively(transform, objectName, out t))
        {
            DestroyImmediate(t.gameObject);
        }
        GameObject body = Instantiate(go, transform);

        body.name = objectName;
        CapsuleCollider capsule = body.AddComponent<CapsuleCollider>();
        capsule.height = 1.25f;
        capsule.radius = 0.30f;
        capsule.center = new Vector3(0, 0.85f, 0);

        objectName = "Head Damage Collider";
        Transform parent = GameObjectFinder.FindChildRecursively(transform, "Head");
        if (GameObjectFinder.TryFindChildRecursively(transform, objectName, out t))
        {
            DestroyImmediate(t.gameObject);
        }
        GameObject head = Instantiate(go, parent);

        head.name = objectName;
        SphereCollider sphere = head.AddComponent<SphereCollider>();
        sphere.radius = 0.02f;
        sphere.center = new Vector3(0, 0.005f, 0.003f);

        head.layer = body.layer = layer;
        body.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        head.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
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
