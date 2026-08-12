using Game.Inventory;
using Game.Registries;
using Game.Saving;
using Game.Terrain;
using UnityEngine;

public static class ItemSpawner
{
    public static GameObject Spawn(
        Item item,
        Vector3 position,
        int itemCount,
        Transform torsoBone = null,
        bool scatter = false,
        float scatterDistance = 0f,
        float scatterForce = 0f)
    {
        using (ScriptPerformanceTracker.Measure("ItemSpawner.Total"))
        {
            if (item == null || item.itemDrop == null) return null;

            if (scatter)
            {
                using (ScriptPerformanceTracker.Measure("ItemSpawner.ScatterPosition"))
                {
                    position += new Vector3(
                        Random.Range(-scatterDistance, scatterDistance),
                        0f,
                        Random.Range(-scatterDistance, scatterDistance)
                    );
                }
            }

            VoxelGrid grid = VoxelGrid.Instance;
            Vector2Int chunkKey;

            using (ScriptPerformanceTracker.Measure("ItemSpawner.FindChunk"))
            {
                // FloorToInt is safer than casting when world positions can be negative.
                int chunkX = Mathf.FloorToInt(position.x / grid.chunkSize);
                int chunkZ = Mathf.FloorToInt(position.z / grid.chunkSize);

                chunkKey = new Vector2Int(chunkX, chunkZ);
            }

            if (!grid.chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk)) return null;

            GameObject prefab = item.itemDrop;
            GameObject spawnedObject;

            using (ScriptPerformanceTracker.Measure("ItemSpawner.Instantiate"))
            {
                Vector3 spawnPosition = torsoBone == null ? position : torsoBone.position;
                Quaternion rotation = torsoBone == null ? prefab.transform.rotation : Quaternion.identity;

                spawnedObject = Object.Instantiate(
                    prefab,
                    spawnPosition,
                    rotation
                );
            }

            using (ScriptPerformanceTracker.Measure("ItemSpawner.Physics"))
            {
                ApplySpawnPhysics(
                    spawnedObject,
                    torsoBone,
                    scatter,
                    scatterForce
                );
            }

            using (ScriptPerformanceTracker.Measure("ItemSpawner.Parent"))
            {
                spawnedObject.transform.SetParent(
                    chunk.chunkObject.transform,
                    worldPositionStays: true
                );
            }

            GameObject objectPrefab;

            using (ScriptPerformanceTracker.Measure("ItemSpawner.RegistryLookup"))
            {
                if (!spawnedObject.TryGetComponent(out PrefabID prefabID))
                {
                    Debug.LogError(
                        $"Spawned item '{spawnedObject.name}' has no PrefabID."
                    );

                    Object.Destroy(spawnedObject);
                    return null;
                }

                objectPrefab =
                    PrefabRegistry.Instance.GetByKey(prefabID.prefabKey);
            }

            using (ScriptPerformanceTracker.Measure("ItemSpawner.SetupInteractable"))
            {
                if (spawnedObject.TryGetComponent(out InteractableItem interactable))
                {
                    interactable.SetItem(item);
                    interactable.itemCount = itemCount;
                    interactable.owningChunk = chunk;
                    interactable.EnablePickupAfterDelay(1.5f);
                }
            }

            using (ScriptPerformanceTracker.Measure("ItemSpawner.SaveRegistration"))
            {
                SpawnedObjectData data = new(
                    position,
                    spawnedObject,
                    objectPrefab
                )
                {
                    instance = spawnedObject
                };

                Utility.AddObjectDataToChunk(data, position, chunk);
            }

            return spawnedObject;
        }
    }

    private static void ApplySpawnPhysics(
        GameObject spawnedObject,
        Transform torsoBone,
        bool scatter,
        float scatterForce)
    {
        if (!spawnedObject.TryGetComponent(out Rigidbody rb)) return;

        Vector3 force;

        if (!scatter && torsoBone != null)
        {
            force = torsoBone.forward * 5f;
        }
        else
        {
            force = new Vector3(
                Random.Range(-scatterForce, scatterForce),
                Random.Range(scatterForce, scatterForce * 2.5f),
                Random.Range(-scatterForce, scatterForce)
            );
        }

        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
    }
}
