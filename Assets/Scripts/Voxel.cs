using System.Collections.Generic;
using UnityEngine;

public class Voxel
{
    public Vector3 position;
    public byte lightLevel;

    public Voxel(Vector3 pos)
    {
        position = pos;
        lightLevel = 0;
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

[System.Serializable]
public class ChunkSaveData
{
    public Vector3 chunkPosition;
    public List<SpawnedObjectData> spawnedObjects = new();
}

public class VoxelChunk
{
    public GameObject chunkObject;
    public Vector3 chunkPosition;

    [System.NonSerialized]
    public Voxel[,] voxels;

    public List<GameObject> objects;
    public List<Vector3> savedObjectPositions;
    public List<SpawnedObjectData> savedObjects;
    public List<ISimulatable> simulatedEntities;
    public BiomeData biome;
    public Mesh generatedMesh;
    public float[,] heightMap;
    public bool visualsEnabled;

    public bool objectsSpawned;
    public bool structureSpawned;

    public bool wasLoadedFromSave;

    public VoxelChunk(GameObject chunkObject, int chunkSize)
    {
        this.chunkObject = chunkObject;
        this.chunkPosition = chunkObject.transform.position;
        this.voxels = new Voxel[chunkSize, chunkSize];
        this.objects = new List<GameObject>();
        this.savedObjectPositions = new List<Vector3>();
        this.savedObjects = new List<SpawnedObjectData>();

        this.objectsSpawned = false;
        this.structureSpawned = false;

        this.simulatedEntities = new List<ISimulatable>();
        this.heightMap = new float[chunkSize, chunkSize];
        this.wasLoadedFromSave = false;
    }
}

[CreateAssetMenu(fileName = "NewBiome", menuName = "Voxel/Biome")]
public class BiomeData : ScriptableObject
{
    public string biomeName;
    public List<VoxelType> voxelTypes = new();
    public NoiseSettings noiseSettings;
    public List<GameObject> treePrefabs = new();
    public List<GameObject> rockPrefabs = new();
    public List<GameObject> treeClusterPrefabs = new();
    public List<GameObject> rockClusterPrefabs = new();
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

[System.Serializable]
public class VoxelType
{
    public string name;
    public float minHeight;
    public float maxHeight;
    public int atlasIndex;
}
