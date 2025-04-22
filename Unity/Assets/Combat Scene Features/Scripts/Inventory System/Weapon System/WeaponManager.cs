using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PickableObject))]
public class WeaponManager : MonoBehaviour
{
    //Private Variables
    private Ray ray;
    private float accumulatedTime;
    private List<Ammo> bulletList = new();

    [Header("Gun Status")]
    [SerializeField] protected int fireRate = 25;
    [SerializeField] protected float bulletDrop = 300f;
    [SerializeField] protected float bulletSpeed = 1000f;
    [SerializeField] protected float maxBulletTime = 3.0f;

    [field: Header("Parameters")]
    [field: SerializeField] public WeaponType type { get; private set; }
    [SerializeField] private AnimatorOverrideController _overrideController;
    [field: SerializeField] public LayerMask EnemyLayerMask { get; protected set; }
    [field: SerializeField] public PickableObject pickableObject { get; private set; }

    [Header("Gun Parameters")]
    [SerializeField] private bool isShooting;
    [SerializeField] private float maxBullets;
    [SerializeField] private Transform grip, rest;
    [field: SerializeField] public Vector3 RestLockedPosition { get; protected set; }
    [field: SerializeField] public Vector3 RestOriginalPosition { get; protected set; }

    [Header("Gun Parameters (FX)")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private ParticleSystem[] muzzleFlash;

    [Header("Melee Parameters")]
    [SerializeField] private CharacterDamageCollider damageCollider;

    private void Awake()
    {
        pickableObject = GetComponent<PickableObject>();
    }

    public void Initialize(CharacterManager character)
    {
        if(character == null)
        {
            return;
        }

        character.CombatManager.AssignWeapon(this);
        if(damageCollider != null)
        {
            damageCollider.SetCharacter(character, null);
        }
        character.Anim.runtimeAnimatorController = _overrideController;
        character.rigController.SetTwoBoneIKConstraint(grip, rest);
    }

    public void EnableCollider()
    {
        damageCollider.SetColliderStatus(true);
    }

    public void DisableCollider()
    {
        damageCollider.SetColliderStatus(false);
    }

    public void HandleAction(float delta, Vector3 targetPosition, CharacterManager characterManager)
    {
        if(type == WeaponType.Gun)
        {
            HandleAction_Gun(delta, targetPosition, characterManager);
        }
    }

    private void HandleAction_Gun(float delta, Vector3 targetPosition, CharacterManager characterManager)
    {
        characterManager.CombatManager.currentAction = null;
        HandleShooting(characterManager, targetPosition, Time.deltaTime);
    }

    #region Gun Parameters

    private void HandleVFX()
    {
        foreach (var particle in muzzleFlash)
        {
            particle.Emit(1);
        }
    }

    private void HandleShooting(CharacterManager character, Vector3 targetPosition, float delta)
    {
        if (character == null)
        {
            return;
        }

        if (character.performingAction)
        {
            return;
        }

        if (character.isAttacking != true)
        {
            return;
        }

        accumulatedTime += delta;
        float fireInterval = 1.0f / fireRate;

        while (accumulatedTime > 0.0f)
        {
            FireBullet(targetPosition);
            accumulatedTime -= fireInterval;
        }
    }

    public void UpdateBullet(float delta, CharacterManager character)
    {
        SimulateBullet(delta, character);
        bulletList.RemoveAll(x => x.time >= maxBulletTime);
    }

    #endregion

    #region Bullet Functions

    protected Ammo CreateBullet(Vector3 pos, Vector3 vel)
    {
        Ammo bullet = new(pos, vel);
        return bullet;
    }

    protected Vector3 GetBulletPosition(Ammo bullet)
    {
        //Pos = bPos + bVel * bTime + 0.5 * grv * bTime^2
        Vector3 gravity = Vector3.down * bulletDrop;
        return bullet.initialPosition + (bullet.initialVelocity * bullet.time) + (0.5f * bullet.time * bullet.time * gravity);
    }

    protected void SimulateBullet(float delta, CharacterManager characterManager)
    {
        bulletList.ForEach
        (
            bullet =>
            {
                Vector3 p0 = GetBulletPosition(bullet);
                bullet.time += delta;
                Vector3 p1 = GetBulletPosition(bullet);
                HandleRaycastSegment(p0, p1, bullet, characterManager);
            }
        );
    }

    protected void HandleRaycastSegment(Vector3 start, Vector3 end, Ammo bullet, CharacterManager characterManager)
    {
        Vector3 dir = end - start;
        float distance = dir.magnitude;

        ray.origin = start;
        ray.direction = dir;

        if (Physics.Raycast(ray, out RaycastHit raycastHit, distance, EnemyLayerMask))
        {
            CharacterManager shotCharacter = raycastHit.collider.GetComponentInParent<CharacterManager>();
            CharacterStatistic shotCharacterStat = shotCharacter.StatsManager;
            //StartCoroutine(HandleTrailFX(start, raycastHit.point));

            if (shotCharacter == null)
            {
                InstantiateBulletHoles(raycastHit);
            }

            if (shotCharacter != null && shotCharacter.characterType != characterManager.characterType)
            {
                float directionFromHit = Vector3.SignedAngle(characterManager.transform.position, shotCharacter.transform.position, Vector3.up);
                shotCharacterStat.TakeDamage(7, AttackType.Heavy);
            }
            bullet.time = maxBulletTime;
            return;
        }
        //StartCoroutine(HandleTrailFX(start, end));
    }

    protected virtual void FireBullet(Vector3 targetPosition)
    {
        HandleVFX();
    }

    protected void InstantiateBulletHoles(RaycastHit raycastHit)
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

    protected GameObject ImpactHole(Collider damagedCollider)
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

    #endregion
}

public enum WeaponType
{
    Gun,
    Melee
}
