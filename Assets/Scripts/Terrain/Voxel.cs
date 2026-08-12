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
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public MeshCollider meshCollider;
        public SurfaceType surfaceType;

        public GameObject chunkObject;
        public Vector3 chunkPosition; // Use as cached world pos

        [System.NonSerialized]
        public Voxel[,] voxels;

        public List<Vector3> savedObjectPositions = new();
        public List<Vector3Int> savedObjectPositionsInt = new();
        public List<SpawnedObjectData> savedObjects = new();
        public List<ISimulatable> simulatedEntities = new();
        public BiomeData biome;
        public Mesh generatedMesh;
        public float[] heightMap;
        public List<Matrix4x4> grassMatrices = new();

        public bool grassGenerated = false;
        public bool structureSpawned = false;
        public bool objectsGenerated = false;
        public bool objectsInstantiated = false;

        public MeshRenderer[] cachedRenderers;
        public Collider[] cachedColliders;

        public VoxelChunk(GameObject chunkObject, int chunkSize)
        {
            this.chunkObject = chunkObject;
            chunkPosition = chunkObject.transform.position;
            voxels = new Voxel[chunkSize, chunkSize];
            heightMap = new float[chunkSize * chunkSize];

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
        public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float heightExponent = 2f;
    }
}
