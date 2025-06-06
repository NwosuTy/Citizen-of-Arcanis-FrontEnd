using UnityEngine;
using UnityEngine.Pool;

[System.Serializable]
public struct Boundary
{
    public float minValue;
    public float maxValue;

    public Boundary(float min, float max)
    {
        minValue = min;
        maxValue = max;
    }
}

public static class ObjectPooler
{
    public static ObjectPool<TrailRenderer> TrailPool(TrailRenderer objectToPool)
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

    public static ObjectPool<BulletFX> BulletFXPool(GameObject decalPrefab, ParticleSystem fxPrefab, MonoBehaviour context)
    {
        ObjectPool<BulletFX> pool = null;

        pool = new ObjectPool<BulletFX>
        (
            () =>
            {
                var fx = new BulletFX(decalPrefab, fxPrefab, context);
                fx.SetPool(pool);
                return fx;
            },
            fx => fx.GetObject(),
            fx => fx.Release(),
            null,
            false, 75, 200
        );
        return pool;
    }
}

public static class GameObjectFinder
{
    public static T GetComponentByName<T>(string name) where T : Component
    {
        GameObject obj = GameObject.Find(name);
        return (obj != null) ? obj.GetComponent<T>() : null;
    }
}
