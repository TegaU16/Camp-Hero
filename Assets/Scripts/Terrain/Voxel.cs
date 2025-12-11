using System.Collections.Generic;
using Game.Saving;
using UnityEngine;

namespace Game.Terrain
{
    public class Voxel
    {
        public Vector3 position;

        public Voxel(Vector3 pos)
        {
            position = pos;
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
        public List<Vector3Int> savedObjectPositionsInt = new();
        public List<SpawnedObjectData> savedObjects = new();
        public List<ISimulatable> simulatedEntities = new();
        public BiomeData biome;
        public Mesh generatedMesh;
        public float[,] heightMap;

        public bool visualsEnabled = false;
        public bool simulationEnabled = false;
        public bool objectsSpawned = false;
        public bool structureSpawned = false;
        public bool objectsGenerated = false;
        public bool objectsInstantiated = false;
        public bool hasNaturalObjects = false;
        public bool hasKeyStructure = false;
        public bool wasLoadedFromSave = false;
        public bool isLoading = false;
        public bool isDirty = false;

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
    }

    [System.Serializable]
    public class NoiseSettings
    {
        public float baseScale = 0.1f;
        public float persistence = 0.5f;
        public float frequency = 1f;
        public int octaves = 4;
        public float lacunarity = 2f;
        public float heightScale = 10f;
        public bool useHeightCurve = true;
        public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float heightExponent = 2f;
    }
}
