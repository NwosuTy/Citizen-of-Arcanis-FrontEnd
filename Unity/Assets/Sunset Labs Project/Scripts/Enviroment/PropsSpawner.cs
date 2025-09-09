using UnityEngine;
using UnityEngine.AI;

public class PropsSpawner : MonoBehaviour
{
    int spawnCount;

    [Header("Parameters")]
    [SerializeField] private float sphere;
    [SerializeField] private int maxSpawnCount;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject[] propsToSpawn;

    public void HandleSpawn()
    {
        if(maxSpawnCount == 0)
        {
            return;
        }
        while(spawnCount < maxSpawnCount)
        {
            SpawnObject();
            spawnCount++;
        }
        spawnCount = 0;
    }

    private void SpawnObject()
    {
        GameObject obj = propsToSpawn[Random.Range(0, propsToSpawn.Length)];

        GameObject spawn = Instantiate(obj, spawnPoint);
        spawn.transform.SetPositionAndRotation(GetSpawnPosition(), GetSpawnRotation());
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 randomPositon = Random.insideUnitSphere * sphere + (transform.position);
        if(NavMesh.SamplePosition(randomPositon, out NavMeshHit hit, sphere, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero;
    }

    private Quaternion GetSpawnRotation()
    {
        float randomDodgeDestination = Random.Range(0, 360f);
        return Quaternion.Euler(transform.rotation.x, randomDodgeDestination, transform.rotation.z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, sphere);
    }
}