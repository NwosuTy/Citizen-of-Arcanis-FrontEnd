using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CharacterAnimatorRigController : MonoBehaviour
{
    protected Transform weaponRestIK;
    protected CharacterManager characterManager;

    [Header("Stats")]
    [SerializeField] protected float aimDuration;

    [field: Header("Rigs")]
    public RigBuilder rigBuilder;
    [field: SerializeField] public Rig HandIKConstraints { get; protected set; }
    [field: SerializeField] public Rig BodyAimConstraints { get; protected set; }
    [field: SerializeField] public Rig WeaponAimConstraint { get; protected set; }

    [field: Header("Hand_IK Parameters")]
    [field: SerializeField] public TwoBoneIKConstraint LeftHandIKConstraint { get; protected set; }
    [field: SerializeField] public TwoBoneIKConstraint RightHandIKConstraint { get; protected set; }

    [field: Header("Aiming Constraints")]
    [field: SerializeField] public MultiAimConstraint[] MultiAimConstraintArray { get; protected set; }

    protected virtual void Awake()
    {
        rigBuilder = GetComponentInParent<RigBuilder>();
        characterManager = GetComponentInParent<CharacterManager>();
    }

    public virtual void CharacterAnimationRig_Updater(float delta)
    {
        Lock_In(delta);
    }

    public void SetTwoBoneIKConstraint(Transform weaponGrip, Transform weaponRest)
    {
        weaponRestIK = weaponRest;

        LeftHandIKConstraint.data.target = weaponRest;
        RightHandIKConstraint.data.target = weaponGrip;

        LeftHandIKConstraint.weight = (weaponRest == null) ? 0.0f : 0.55f;
        RightHandIKConstraint.weight = (weaponGrip == null) ? 0.0f : 1.0f;
        rigBuilder.Build();
    }

    public void SetAimTarget(Transform aimedTarget)
    {
        foreach (MultiAimConstraint multiAimConstraint in MultiAimConstraintArray)
        {
            var sources = multiAimConstraint.data.sourceObjects;
            WeightedTransform weightedTransform = new(aimedTarget, 1f);

            sources.Clear();
            sources.Add(weightedTransform);
            multiAimConstraint.data.sourceObjects = sources;
        }
        rigBuilder.Build();
    }

    public void StopAllRigs()
    {
        HandIKConstraints.weight = 0.0f;
        BodyAimConstraints.weight = 0.0f;
        WeaponAimConstraint.weight = 0.0f;
    }

    private void Lock_In(float delta)
    {
        float moveDuration = delta / aimDuration;
        WeaponManager currentWeapon = characterManager.CombatManager.CurrentWeapon;

        if (currentWeapon == null || currentWeapon.type == WeaponType.Melee || weaponRestIK == null)
        {
            return;
        }

        if (characterManager.isLockedIn)
        {
            WeaponAimConstraint.weight += moveDuration;
            weaponRestIK.localPosition = Vector3.MoveTowards(weaponRestIK.localPosition, currentWeapon.RestLockedPosition, moveDuration);
            return;
        }

        //Instantly Set Aim if isAttacking and not locked in
        if (characterManager.isAttacking)
        {
            WeaponAimConstraint.weight = 1.0f;
            weaponRestIK.localPosition = currentWeapon.RestLockedPosition;
            return;
        }
        WeaponAimConstraint.weight -= moveDuration;
        weaponRestIK.localPosition = Vector3.MoveTowards(weaponRestIK.localPosition, currentWeapon.RestOriginalPosition, moveDuration);
    }
}
