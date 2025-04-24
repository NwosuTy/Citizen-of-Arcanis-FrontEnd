using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(PickableObject))]
public class WeaponManager : MonoBehaviour
{
    //Private Variables
    private Ray ray;
    private int bulletLeft;

    private Image crossHairImg;
    private CinemachineFreeLook freeLookCamera;
    private CinemachineImpulseSource impulseSource;

    [Header("Gun Status")]
    [SerializeField] private int fireRate = 25;
    [SerializeField] private float gunRange = 15.0f;
    [SerializeField] private float simulationSpeed = 3.0f;

    [field: Header("Parameters")]
    [field: SerializeField] public WeaponType type { get; private set; }
    [SerializeField] private AnimatorOverrideController _overrideController;
    [field: SerializeField] public LayerMask DamageableMask { get; private set; }
    [field: SerializeField] public PickableObject pickableObject { get; private set; }

    [Header("Gun Parameters")]
    [SerializeField] private int maxBullets;
    [SerializeField] private bool isShooting;
    [SerializeField] private Transform grip, rest;
    [field: SerializeField] public Vector3 RestLockedPosition { get; private set; }
    [field: SerializeField] public Vector3 RestOriginalPosition { get; private set; }

    [Header("Gun Parameters (FX)")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private ParticleSystem[] muzzleFlash;

    [field: Header("Melee Parameters")]
    [field: SerializeField] public CharacterDamageCollider DamageCollider { get; private set; }

    [Header("Weapon Recoil")]
    [SerializeField] private WeaponRecoil weaponRecoil = new();

    private void Awake()
    {
        pickableObject = GetComponent<PickableObject>();

        impulseSource = GetComponent<CinemachineImpulseSource>();
        DamageCollider = GetComponentInChildren<CharacterDamageCollider>(); 
    }

    public void Initialize(CharacterManager character, PlaceHolderCombatScript pcs)
    {
        bulletLeft = maxBullets;
        if(pcs == null)
        {
            character.CombatManager.AssignWeapon(this);
            character.Anim.runtimeAnimatorController = _overrideController;
        }
        else
        {
            pcs.AssignWeapon(this);
        }
        if (DamageCollider != null)
        {
            DamageCollider.SetCharacter(character, pcs);
        }

        if(type == WeaponType.Gun)
        {
            CombatManager combatManager = CombatManager.Instance;

            crossHairImg = combatManager.CrossHairImg;
            freeLookCamera = combatManager.FreeLookCamera;

            weaponRecoil.Initialize(combatManager.CameraObject, freeLookCamera, impulseSource);
            if(character != null) character.rigController.SetTwoBoneIKConstraint(grip, rest);
        }
    }

    private void HandleReload()
    {
        if (bulletLeft >= maxBullets)
        {
            return;
        }
        int difference = maxBullets - bulletLeft;
        bulletLeft += difference;
    }

    public void SetCrossHairImage(Image crossHair)
    {
        crossHairImg = crossHair;
    }

    public void WeaponManager_Update(float delta)
    {
        weaponRecoil.HandleRecoil(delta);
    }

    public void HandleAction(Vector3 targetPosition, CharacterManager characterManager, PlaceHolderCombatScript pcs)
    {
        if(type == WeaponType.Gun)
        {
            HandleAction_Gun(targetPosition, characterManager, pcs);
        }
    }

    private void HandleAction_Gun(Vector3 targetPosition, CharacterManager characterManager, PlaceHolderCombatScript pcs)
    {
        if(characterManager != null)
        characterManager.CombatManager.currentAction = null;

        HandleVFX();
        HandleShooting(targetPosition, characterManager, pcs);
    }

    #region Gun Parameters

    private void HandleVFX()
    {
        foreach (var particle in muzzleFlash)
        {
            particle.Emit(1);
        }
    }

    private void HandleShooting(Vector3 targetPosition, CharacterManager character, PlaceHolderCombatScript pcs)
    {
        Vector3 direction = targetPosition - muzzlePoint.position;
        Ray ray = new(muzzlePoint.position, direction);

        crossHairImg.color = Color.white;
        weaponRecoil.GenerateRecoilPattern();
        if(Physics.Raycast(ray, out RaycastHit hitInfo, gunRange, DamageableMask))
        {
            CharacterManager shotCharacter = hitInfo.collider.GetComponentInParent<CharacterManager>();
            StartCoroutine(HandleTrailFX(muzzlePoint.position, hitInfo.point));
            bool sameTema = (pcs == null && shotCharacter.currentTeam != character.currentTeam);

            if (shotCharacter == null)
            {
                InstantiateBulletHoles(hitInfo);
            }
            else if(shotCharacter != null && sameTema)
            {
                crossHairImg.color = Color.green;
                shotCharacter.StatsManager.TakeDamage(2, AttackType.Heavy);
            }
            return;
        }
        StartCoroutine(HandleTrailFX(muzzlePoint.position, targetPosition));
    }

    private void InstantiateBulletHoles(RaycastHit raycastHit)
    {
        float positionMultiplier = 0.5f;
        float spawnX = raycastHit.point.x - ray.direction.x * positionMultiplier;
        float spawnY = raycastHit.point.y - ray.direction.y * positionMultiplier;
        float spawnZ = raycastHit.point.z - ray.direction.z * positionMultiplier;
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, spawnZ);

        GameObject bulletHoles = ImpactHole(raycastHit.collider);
        Quaternion targetRotation = Quaternion.LookRotation(ray.direction);
        bulletHoles.transform.SetPositionAndRotation(spawnPosition, targetRotation);
    }

    private GameObject ImpactHole(Collider damagedCollider)
    {
        if (damagedCollider.CompareTag("Wood"))
        {
            return GameObjectManager.woodBulletHolesPool.Get();
        }
        else if (damagedCollider.CompareTag("Metal"))
        {
            return GameObjectManager.metalBulletHolesPool.Get();
        }
        return GameObjectManager.cementBulletHolesPool.Get();
    }

    private IEnumerator HandleTrailFX(Vector3 start, Vector3 end)
    {
        TrailRenderer trail = GameObjectManager.bulletTrailPool.Get();
        trail.transform.position = start;
        yield return null;

        trail.emitting = true;
        float distance = Vector3.Distance(start, end);
        float remainingDistance = distance;
        while (remainingDistance > 0f)
        {
            trail.transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(1 - (remainingDistance / distance)));

            remainingDistance -= simulationSpeed * Time.deltaTime;
            yield return null;
        }

        trail.transform.position = end;
        yield return new WaitForSeconds(trail.time);
        yield return null;

        trail.emitting = false;
        trail.transform.position = end;

        GameObjectManager.bulletTrailPool.Release(trail);
    }

    #endregion
}
