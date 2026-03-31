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
        if (item == null || item.itemDrop == null) return null;

        if (scatter)
        {
            position += new Vector3(
                Random.Range(-scatterDistance, scatterDistance),
                0f,
                Random.Range(-scatterDistance, scatterDistance)
            );
        }

        int chunkX = (int)(position.x / VoxelGrid.Instance.chunkSize);
        int chunkZ = (int)(position.z / VoxelGrid.Instance.chunkSize);
        Vector2Int chunkKey = new(chunkX, chunkZ);

        if (!VoxelGrid.Instance.chunkMap.TryGetValue(chunkKey, out VoxelChunk chunk)) return null;

        GameObject prefab = item.itemDrop;
        GameObject spawnedObject = torsoBone == null
            ? Object.Instantiate(prefab, position, prefab.transform.rotation)
            : Object.Instantiate(prefab, torsoBone.position, Quaternion.identity);

        if (spawnedObject.TryGetComponent(out Rigidbody rb) && scatter)
        {
            Vector3 randomForce = new(
                Random.Range(-scatterForce, scatterForce),
                Random.Range(scatterForce, scatterForce * 2.5f),
                Random.Range(-scatterForce, scatterForce)
            );
            rb.AddForce(randomForce, ForceMode.Impulse);

            rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }

        spawnedObject.transform.parent = chunk.chunkObject.transform;

        chunk.objects.Add(spawnedObject);

        PrefabID prefabID = spawnedObject.GetComponent<PrefabID>();
        string prefabKey = prefabID.prefabKey;
        GameObject objectPrefab = PrefabRegistry.GetPrefabByKey(prefabKey);

        if (spawnedObject.TryGetComponent(out InteractableItem interactable))
        {
            interactable.SetItem(item);
            interactable.itemCount = itemCount;
            interactable.owningChunk = chunk;
            interactable.EnablePickupAfterDelay(0.25f);
        }

        SpawnedObjectData data = new(position, spawnedObject, objectPrefab)
        {
            instance = spawnedObject
        };
        Utility.AddObjectDataToChunk(data, position, chunk);

        return spawnedObject;
    }
}
