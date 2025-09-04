using System.Collections.Generic;
using UnityEngine;

public class Voxel
{
    public Vector3 position;

    public Voxel(Vector3 pos)
    {
        position = pos;
    }
}

public class SpawnedObjectData
{
    public Vector3 position;
    public string prefabName;
    public string savedStateJson;

    public SpawnedObjectData(Vector3 pos, GameObject obj)
    {
        position = pos;
        prefabName = obj.name;

        if (obj.TryGetComponent(out ISaveableObject saveable))
        {
            savedStateJson = saveable.SaveState();
        }
        else
        {
            savedStateJson = null;
        }
    }
}

public class VoxelChunk
{
    public GameObject chunkObject;
    public Vector3 chunkPosition; // Use as cached world pos

    [System.NonSerialized]
    public Voxel[,] voxels;

    public List<GameObject> objects = new();
    public List<Vector3> savedObjectPositions = new();
    public List<SpawnedObjectData> savedObjects = new();
    public List<ISimulatable> simulatedEntities = new();
    public BiomeData biome;
    public Mesh generatedMesh;
    public float[,] heightMap;

    public bool visualsEnabled = false;
    public bool simulationEnabled = false;
    public bool objectsSpawned = false;
    public bool structureSpawned = false;
    public bool hasNaturalObjects = false;
    public bool hasKeyStructure = false;
    public bool wasLoadedFromSave = false;

    public MeshRenderer[] cachedRenderers;
    public Collider[] cachedColliders;

    public VoxelChunk(GameObject chunkObject, int chunkSize)
    {
        this.chunkObject = chunkObject;
        chunkPosition = chunkObject.transform.position;
        voxels = new Voxel[chunkSize, chunkSize];
        heightMap = new float[chunkSize, chunkSize];

        // Cache renderers and colliders at creation
        cachedRenderers = chunkObject.GetComponentsInChildren<MeshRenderer>();
        cachedColliders = chunkObject.GetComponentsInChildren<Collider>();
    }

    public void SaveChunkFurnaces(ChunkSaveData data)
    {
        data.furnaceStates.Clear();
        FurnaceUnit[] furnaces = chunkObject.GetComponentsInChildren<FurnaceUnit>();
        foreach (FurnaceUnit furnace in furnaces)
        {
            data.furnaceStates.Add(furnace.SaveState());
        }
    }

    public void LoadChunkFurnaces(ChunkSaveData data)
    {
        FurnaceUnit[] furnaces = chunkObject.GetComponentsInChildren<FurnaceUnit>();
        for (int i = 0; i < furnaces.Length && i < data.furnaceStates.Count; i++)
        {
            furnaces[i].LoadState(data.furnaceStates[i]);
        }
    }

    public void SaveChunkStorages(ChunkSaveData data)
    {
        data.storageStates.Clear();
        StorageUnit[] storages = chunkObject.GetComponentsInChildren<StorageUnit>();
        foreach (StorageUnit storage in storages)
        {
            data.storageStates.Add(storage.SaveState());
        }
    }

    public void LoadChunkStorages(ChunkSaveData data)
    {
        StorageUnit[] storages = chunkObject.GetComponentsInChildren<StorageUnit>();
        for (int i = 0; i < storages.Length && i < data.storageStates.Count; i++)
        {
            storages[i].LoadState(data.storageStates[i]);
        }
    }
}

[System.Serializable]
public class NoiseSettings
{
    public float baseScale = 0.1f;
    public float persistence = 0.5f;
    public int octaves = 4;
    public float lacunarity = 2f;
    public float heightScale = 10f;
    public bool useHeightCurve = true;
    public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public float heightExponent = 2f;
}
