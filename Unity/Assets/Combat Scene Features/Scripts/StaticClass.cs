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

public struct RoutePoint
{
    public Vector3 position;
    public Vector3 direction;

    public RoutePoint(Vector3 pos, Vector3 dir)
    {
        position = pos;
        direction = dir;
    }
}

[System.Serializable]
public struct WayPointProperty
{
    public WayPointNode node;

    [Header("Property")]
    public float distance;
    public Vector3 position;

    public WayPointProperty(float d, Vector3 pos, WayPointNode n)
    {
        node = n;
        distance = d;
        position = pos;
    }
}

public struct TransformContainer
{
    public Vector3 position;
    public Quaternion rotation;

    public TransformContainer(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
    }
}

public class EvenOrOddAttribute : PropertyAttribute
{
    public bool isEven;
    public BoundInt boundary;

    public EvenOrOddAttribute(int min, int max, bool isEven = true)
    {
        this.isEven = isEven;
        boundary = new(min, max);
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

public static class MathPhysics_Helper
{
    public static float ULerpCurve(float from, float to, float factor)
    {
        return ULerp(from, to, CurveFactor(factor));
    }

    public static float CurveFactor(float factor)
    {
        return 1 - (1 - factor) * (1 - factor);
    }

    public static float ULerp(float from, float to, float value)
    {
        return (1.0f - value) * from + value * to;
    }

    public static RoutePoint GetRoutePoint(Vector3[] nodes, float[] distances, float dist)
    {
        int count = nodes.Length;
        if (count < 4 || distances.Length != count)
        {
            return new RoutePoint(Vector3.zero, Vector3.forward);
        }

        int i;
        dist = Mathf.Repeat(dist, distances[count - 1]);
        for (i = 1; i < count; i++)
        {
            if (distances[i] > dist)
            {
                break;
            }
        }

        float segStart = distances[i - 1];
        float t = Mathf.InverseLerp(segStart, distances[i], dist);

        Vector3 p0 = nodes[(i - 2 + count) % count];
        Vector3 p1 = nodes[(i - 1 + count) % count];
        Vector3 p2 = nodes[i % count];
        Vector3 p3 = nodes[(i + 1) % count];

        Vector3 pos = CatmullRom(p0, p1, p2, p3, t);
        Vector3 dir = (CatmullRom(p0, p1, p2, p3, t + 0.01f) - pos).normalized;
        return new RoutePoint(pos, dir);
    }

    public static Vector3 GetRoutePosition(Vector3[] nodes, float[] distances, float dist)
    {
        return GetRoutePoint(nodes, distances, dist).position;
    }

    public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * (
            2f * p1 +
            (p2 - p0) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }

}
