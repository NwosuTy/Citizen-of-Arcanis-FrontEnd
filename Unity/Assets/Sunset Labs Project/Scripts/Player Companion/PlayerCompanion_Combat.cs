using UnityEngine;
using System.Collections.Generic;

public class PlayerCompanion_Combat : MonoBehaviour
{
    private Collider[] enemyColliders;
    private PlayerCompanion manager;
    private readonly List<Transform> potentialTargets = new();

    private bool canShoot;
    public Transform target;
    private float shootingTimer = 0.0f;
    public CompanionState companionState = CompanionState.Friendly;

    [Header("Weapon Parameters")]
    [SerializeField] private float inaccuracy;
    [SerializeField] private Vector3 targetOffset;

    [Header("Combat Properties")]
    [SerializeField] private float shieldHealth;
    [SerializeField] private float shieldActiveTimer;
    [SerializeField] private float shootCoolDownTime = 5.0f;
    [SerializeField] private PlayerCompanion_Weapon weaponManager;
    
    [Header("Detection Parameters")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private LayerMask obstacleMask;
    [Range(-180, 180)][SerializeField] private float detectAngle;
    [Range(1.0f, 10.0f)][SerializeField] private float detectRadius;

    private void Awake()
    {
        manager = GetComponent<PlayerCompanion>();
        weaponManager = GetComponent<PlayerCompanion_Weapon>();
    }

    private void Start()
    {
        canShoot = true;
        enemyColliders = new Collider[10];
    }

    public void Combat_Update(float delta)
    {
        if(CompanionState.Friendly == companionState)
        {
            return;
        }

        if(Time.frameCount % 20 == 0)
        {
            GetTargets();
        }
        weaponManager.UpdateBullet(delta);

        if(target == null)
        {
            return;
        }
        HandleShooting(delta);
    }

    private void GetTargets()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, detectRadius, enemyColliders, enemyMask);
        potentialTargets.Clear();

        for(int i = 0; i < count; i++)
        {
            if(enemyColliders[i] == null)
            {
                continue;
            }

            //Change to CharacterManager
            Transform t = enemyColliders[i].transform;
            if (manager.FollowTarget == t)
            {
                continue;
            }

            if(potentialTargets.Contains(t) != true && InLineOfSight(t))
            {
                potentialTargets.Add(t);
            }
        }
        target = GetTarget();
    }

    private Transform GetTarget()
    {
        Transform target = null;
        float maxDis = float.MaxValue;
        for(int i = 0; i < potentialTargets.Count; i++)
        {
            float dis = (potentialTargets[i].position - transform.position).sqrMagnitude;
            if(dis < maxDis)
            {
                maxDis = dis;
                target = potentialTargets[i];
            }
        }
        return target;
    }

    private void HandleShooting(float delta)
    {
        if(canShoot != true)
        {
            shootingTimer -= delta;
            if(shootingTimer <= 0)
            {
                shootingTimer = 0;
                canShoot = true;
            }
            return;
        }
        Vector3 targetPos = target.position + targetOffset;
        targetPos += Random.insideUnitSphere * inaccuracy;
        weaponManager.HandleShooting(targetPos, delta);
        Invoke(nameof(DisableShooting), shootCoolDownTime);
    }

    private void DisableShooting()
    {
        canShoot = false;
        shootingTimer = shootCoolDownTime + 1.5f;
    }

    private bool InLineOfSight(Transform potentialTarget)
    {
        Vector3 direction = (potentialTarget.position - transform.position).normalized;

        float viewAngle = Vector3.Angle(direction, transform.forward);
        if(viewAngle < detectAngle/2)
        {
            return (Physics.Linecast(transform.position, potentialTarget.position, obstacleMask) != true);
        }
        return false;
    }
}
