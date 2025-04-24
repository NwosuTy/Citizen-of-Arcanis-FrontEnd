using UnityEngine;
using UnityEngine.Pool;

public class GameObjectManager : MonoBehaviour
{
    public static ObjectPool<TrailRenderer> bulletTrailPool { get; private set; }
    public static ObjectPool<GameObject> woodBulletHolesPool { get; private set; }
    public static ObjectPool<GameObject> metalBulletHolesPool { get; private set; }
    public static ObjectPool<GameObject> cementBulletHolesPool { get; private set; }

    [Header("Impact Objects")]
    [SerializeField] private GameObject woodBulletHoles;
    [SerializeField] private GameObject metalBulletHoles;
    [SerializeField] private GameObject cementBulletHoles;
    [SerializeField] protected TrailRenderer bulletTrailPrefab;

    [Header("Objects To Spawn")]
    [SerializeField] private GameObject bloodPrefab;

    private void Awake()
    {
        bulletTrailPool = TrailFXPool(bulletTrailPrefab);

        //Impact Objects
        woodBulletHolesPool = GameObjectPool(woodBulletHoles);
        metalBulletHolesPool = GameObjectPool(metalBulletHoles);
        cementBulletHolesPool = GameObjectPool(cementBulletHoles);
    }

    public static ObjectPool<GameObject> GameObjectPool(GameObject objectToPool)
    {
        ObjectPool<GameObject> objectPool = new ObjectPool<GameObject>
        (
            () => { return GameObject.Instantiate(objectToPool); },
            spawnObject => { spawnObject.SetActive(true); },
            spawnObject => { spawnObject.SetActive(false); },
            spawnObject => { GameObject.Destroy(spawnObject); },
            false, 50, 100
        );
        return objectPool;
    }

    public static ObjectPool<TrailRenderer> TrailFXPool(TrailRenderer objectToPool)
    {
        ObjectPool<TrailRenderer> objectPool = new ObjectPool<TrailRenderer>
        (
            () => { return GameObject.Instantiate(objectToPool); },
            spawnObject => { spawnObject.gameObject.SetActive(true); },
            spawnObject => { spawnObject.gameObject.SetActive(false); },
            spawnObject => { GameObject.Destroy(spawnObject.gameObject); },
            false, 400, 500
        );
        return objectPool;
    }
}
