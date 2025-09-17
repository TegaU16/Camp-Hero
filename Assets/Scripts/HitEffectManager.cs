using UnityEngine;

public class HitEffectManager : MonoBehaviour
{
    public GameObject sparkPrefab;

    public void SpawnSparks(Vector3 position, Vector3 normal)
    {
        float offset = 0.1f;
        Vector3 spawnPoint = position + normal * offset;
        GameObject spark = Instantiate(sparkPrefab, spawnPoint, Quaternion.LookRotation(normal));

        Destroy(spark, 1f);
    }
}
