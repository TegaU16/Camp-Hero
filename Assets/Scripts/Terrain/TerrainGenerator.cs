using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static Game.Terrain.VoxelGrid;

namespace Game.Terrain
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexData
    {
        public float3 position;
        public float3 normal;
        public float2 uv;
    }

    public class TerrainGenerator : MonoBehaviour
    {
        public static TerrainGenerator Instance;

        public List<BiomeData> biomes;
        public float biomeNoiseScale;

        private readonly Dictionary<Vector3Int, VoxelState> voxelStates = new();

        private int seed;
        private int chunkSize;
        private float voxelSize;
        private float heightOffset;
        private Material voxelMaterial;

        private long totalChunkTime;
        private int chunkCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            seed = VoxelGrid.Instance.seed;
            chunkSize = VoxelGrid.Instance.chunkSize;
            voxelSize = VoxelGrid.Instance.voxelSize;
            heightOffset = VoxelGrid.Instance.heightOffset;
            voxelMaterial = VoxelGrid.Instance.voxelMaterial;
        }

        private void OnDestroy()
        {
            foreach (BiomeData biome in biomes)
            {
                if (!biome.CachedCurveTable.IsCreated) continue;
                biome.CachedCurveTable.Dispose();
            }
        }

        #region Terrain Generation

        public void GenerateChunkTerrain(VoxelChunk chunk, Vector3 chunkPosition)
        {
            Stopwatch sw = Stopwatch.StartNew();
            
            BiomeData biome = SelectBiome(chunkPosition);
            if (biome == null)
            {
                UnityEngine.Debug.LogWarning("Biome not found at " + chunkPosition + " - using default settings.");
                biome = ScriptableObject.CreateInstance<BiomeData>();
                biome.noiseSettings = new NoiseSettings();
            }

            chunk.biome = biome;

            NoiseSettings biomeNoiseSettings = biome.noiseSettings;

            if (!biome.NoiseInitialized)
            {
                BurstNoise burstNoise = BurstNoise.Default(seed);

                burstNoise.SetFrequency(biomeNoiseSettings.frequency);
                burstNoise.SetFractalOctaves(biomeNoiseSettings.octaves);
                burstNoise.SetLacunarity(biomeNoiseSettings.lacunarity);
                burstNoise.SetFractalGain(biomeNoiseSettings.persistence);

                biome.CachedNoiseLayer = new NoiseLayer
                {
                    noise = burstNoise,
                    offset = float3.zero,
                    scale = biomeNoiseSettings.baseScale,
                    weight = 1f
                };

                biome.NoiseInitialized = true;
            }

            NativeArray<NoiseLayer> bakedLayers = new(1, Allocator.TempJob);
            bakedLayers[0] = biome.CachedNoiseLayer;

            int total = chunkSize * chunkSize;
            NativeArray<float> noiseNative = new(total, Allocator.TempJob);

            if (!biome.CachedCurveTable.IsCreated)
            {
                biome.CachedCurveTable = new NativeArray<float>(256, Allocator.Persistent);

                for (int i = 0; i < 256; i++)
                {
                    float t = i / 255f;
                    biome.CachedCurveTable[i] = biomeNoiseSettings.heightCurve.Evaluate(t);
                }
            }

            NoiseMapJob2D noiseJob = new()
            {
                size = new int2(chunkSize, chunkSize),
                worldOffset = new float2(chunkPosition.x, chunkPosition.z),
                layers = bakedLayers,
                noiseOut = noiseNative
            };

            JobHandle noiseHandle = noiseJob.Schedule(total, 64);

            NativeArray<float> heights = new(total, Allocator.TempJob);

            GenerateHeightMapJob heightJob = new()
            {
                noiseIn = noiseNative,
                heightCurve = biome.CachedCurveTable,
                heightExponent = biomeNoiseSettings.heightExponent,
                heightMultiplier = biomeNoiseSettings.heightScale,
                heightOffset = this.heightOffset,

                heightOut = heights
            };

            JobHandle heightHandle = heightJob.Schedule(total, 64, noiseHandle);

            int maxFaces = total * 5;
            NativeArray<float3> vertsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<int> trisNative = new(maxFaces * 6, Allocator.TempJob);
            NativeArray<float2> uvsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<uint> colsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<float3> normsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<int> outVertCount = new(1, Allocator.TempJob);
            NativeArray<int> outTriCount = new(1, Allocator.TempJob);
            NativeArray<float> outHeightMap = new(chunkSize * chunkSize, Allocator.TempJob);

            MeshBuildJob meshJob = new()
            {
                chunkSizeX = chunkSize,
                chunkSizeZ = chunkSize,
                voxelSize = voxelSize,
                voxelHeights = heights,

                outVertices = vertsNative,
                outTriangles = trisNative,
                outUVs = uvsNative,
                outColors = colsNative,
                outNormals = normsNative,
                outVertCount = outVertCount,
                outTriCount = outTriCount,
                outHeightMap = outHeightMap
            };

            JobHandle meshHandle = meshJob.Schedule(heightHandle);
            meshHandle.Complete();

            chunk.heightMap = new float[chunkSize * chunkSize];

            for (int z = 0; z < chunkSize; z++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    int i = x + z * chunkSize;
                    chunk.heightMap[i] = outHeightMap[i];
                }
            }

            int vertCount = outVertCount[0];
            int triCount = outTriCount[0];

            vertCount = math.min(vertCount, vertsNative.Length);
            triCount = math.min(triCount, trisNative.Length);

            Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData meshData = meshDataArray[0];

            meshData.SetVertexBufferParams(
                vertCount,
                new VertexAttributeDescriptor(
                    VertexAttribute.Position,
                    VertexAttributeFormat.Float32,
                    3),

                new VertexAttributeDescriptor(
                    VertexAttribute.Normal,
                    VertexAttributeFormat.Float32,
                    3),

                new VertexAttributeDescriptor(
                    VertexAttribute.TexCoord0,
                    VertexAttributeFormat.Float32,
                    2)
            );

            meshData.SetIndexBufferParams(
                triCount,
                IndexFormat.UInt32
            );

            NativeArray<VertexData> vertexBuffer = meshData.GetVertexData<VertexData>();

            NativeArray<uint> indexBuffer = meshData.GetIndexData<uint>();

            for (int i = 0; i < vertCount; i++)
            {
                vertexBuffer[i] = new VertexData
                {
                    position = vertsNative[i],
                    normal = normsNative[i],
                    uv = uvsNative[i]
                };
            }

            for (int i = 0; i < triCount; i++)
                indexBuffer[i] = (uint)trisNative[i];

            meshData.subMeshCount = 1;

            meshData.SetSubMesh(
                0,
                new SubMeshDescriptor(0, triCount)
                {
                    topology = MeshTopology.Triangles
                },
                MeshUpdateFlags.DontRecalculateBounds
            );

            Mesh mesh = new();

            Mesh.ApplyAndDisposeWritableMeshData(
                meshDataArray,
                mesh,
                MeshUpdateFlags.DontRecalculateBounds
            );

            mesh.RecalculateBounds();

            chunk.meshFilter.mesh = mesh;
            if (voxelMaterial != null)
                chunk.meshRenderer.sharedMaterial = voxelMaterial;

            chunk.meshCollider.sharedMesh = mesh;

            chunk.chunkObject.layer = LayerMask.NameToLayer("Ground");
            chunk.chunkObject.SetActive(true);
            chunk.generatedMesh = mesh;

            noiseNative.Dispose();
            bakedLayers.Dispose();
            heights.Dispose();

            vertsNative.Dispose();
            trisNative.Dispose();
            uvsNative.Dispose();
            colsNative.Dispose();
            normsNative.Dispose();
            outVertCount.Dispose();
            outTriCount.Dispose();
            outHeightMap.Dispose();

            totalChunkTime += sw.ElapsedMilliseconds;
            chunkCount++;

            if (chunkCount % 100 == 0)
            {
                UnityEngine.Debug.Log(
                    $"Average Chunk Time: {(float)totalChunkTime / chunkCount} ms");
            }
        }

        #endregion

        #region Voxel States

        public bool IsOccupied(Vector3Int pos) =>
            voxelStates.TryGetValue(pos, out VoxelState state) && state.HasFlag(VoxelState.Occupied);

        public bool IsWalkable(Vector3Int pos)
        {
            if (!voxelStates.TryGetValue(pos, out VoxelState state)) return true;
            return state.HasFlag(VoxelState.Walkable);
        }

        public bool IsBuildable(Vector3Int pos)
        {
            if (!voxelStates.TryGetValue(pos, out VoxelState state)) return true;
            return state.HasFlag(VoxelState.Buildable);
        }

        public void SetVoxelState(Vector3Int pos, VoxelState flag, bool enable)
        {
            if (!voxelStates.TryGetValue(pos, out VoxelState current))
                current = VoxelState.None;

            if (enable)
            {
                voxelStates[pos] = current | flag;
                return;
            }

            current &= ~flag;
            if (current == VoxelState.None)
                voxelStates.Remove(pos); // cleanup
            else
                voxelStates[pos] = current;
        }

        public void MarkVoxelArea(GameObject instance, bool occupy = true, bool walkable = false, bool buildable = true)
        {
            Bounds bounds = instance.GetComponentInChildren<Renderer>().bounds;

            Vector3Int min = Utility.WorldToVoxelCoord(bounds.min);
            Vector3Int max = Utility.WorldToVoxelCoord(bounds.max);

            for (int x = min.x; x <= max.x; x++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    Vector3Int voxelPos = new(x, 0, z);

                    SetVoxelState(voxelPos, VoxelState.Occupied, occupy);
                    SetVoxelState(voxelPos, VoxelState.Walkable, walkable);
                    SetVoxelState(voxelPos, VoxelState.Buildable, buildable);
                }
            }
        }

        public void MarkVoxelArea(float radius, Vector3 center, bool occupy = true, bool walkable = false, bool buildable = true)
        {
            int radiusInBlocks = Mathf.FloorToInt(radius);
            Vector3Int centerCell = Utility.WorldToVoxelCoord(center);

            for (int x = -radiusInBlocks; x <= radiusInBlocks; x++)
            {
                for (int z = -radiusInBlocks; z <= radiusInBlocks; z++)
                {
                    Vector3Int voxelPos = new(centerCell.x + x, 0, centerCell.z + z);

                    SetVoxelState(voxelPos, VoxelState.Occupied, occupy);
                    SetVoxelState(voxelPos, VoxelState.Walkable, walkable);
                    SetVoxelState(voxelPos, VoxelState.Buildable, buildable);
                }
            }
        }

        #endregion

        #region Biome

        private BiomeData SelectBiome(Vector3 chunkPosition)
        {
            float biomeNoise = GenerateBiomeNoise(chunkPosition.x, chunkPosition.z);
            float normalizedNoise = Mathf.InverseLerp(-1f, 1f, biomeNoise);

            for (int i = 0; i < biomes.Count; i++)
            {
                float t = (i + 1) / (float)biomes.Count;
                if (normalizedNoise < t) return biomes[i];
            }

            return biomes[^1];
        }

        private float GenerateBiomeNoise(float x, float z)
        {
            float scale = biomeNoiseScale;
            return Mathf.PerlinNoise((x + seed) * scale, (z + seed) * scale) * 2f - 1f;
        }

        #endregion

        #region Terrain Burst

        [BurstCompile]
        public struct NoiseMapJob2D : IJobParallelFor
        {
            public int2 size; // X,Z
            public float2 worldOffset; // world X,Z to add

            [ReadOnly] public NativeArray<NoiseLayer> layers; // length >= 1 for B1
            [WriteOnly] public NativeArray<float> noiseOut; // length = size.x*size.y

            public void Execute(int index)
            {
                int cx = index / size.y; // row (x)
                int cz = index % size.y; // col (z)
                float2 point = new(worldOffset.x + cx, worldOffset.y + cz);

                float sum = 0f;
                float sumWeight = 0f;
                for (int i = 0; i < layers.Length; i++)
                {
                    NoiseLayer layer = layers[i];
                    float2 sample = (point + layer.offset.xz) * layer.scale;
                    float s = layer.noise.Sample(sample);
                    sum += s * layer.weight;
                    sumWeight += layer.weight;
                }

                float final = (sumWeight > 0f) ? (sum / sumWeight) : 0f;
                noiseOut[index] = final;
            }
        }

        [BurstCompile]
        public struct GenerateHeightMapJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> noiseIn;
            [ReadOnly] public NativeArray<float> heightCurve;
            [ReadOnly] public float heightExponent;
            [ReadOnly] public float heightMultiplier; // world height scale
            [ReadOnly] public float heightOffset;     // voxel height in world units

            [WriteOnly] public NativeArray<float> heightOut;

            public void Execute(int index)
            {
                float n = noiseIn[index];
                n = (n + 1f) * 0.5f;
                n = math.clamp(n, 0f, 1f);

                if (heightCurve.Length == 256)
                {
                    int ci = (int)(n * 255f);
                    n = heightCurve[math.clamp(ci, 0, 255)];
                }
                else
                {
                    n = math.pow(n, heightExponent);
                }

                float worldHeight = n * heightMultiplier;
                float voxelH = worldHeight / heightOffset;
                voxelH = math.round(voxelH);
                voxelH *= heightOffset;          // convert back to world height
                heightOut[index] = voxelH;
            }
        }

        [BurstCompile]
        public struct MeshBuildJob : IJob
        {
            [ReadOnly] public int chunkSizeX;
            [ReadOnly] public int chunkSizeZ;
            [ReadOnly] public float voxelSize;
            [ReadOnly] public NativeArray<float> voxelHeights;

            [WriteOnly] public NativeArray<float3> outVertices;
            [WriteOnly] public NativeArray<int> outTriangles;
            [WriteOnly] public NativeArray<float2> outUVs;
            [WriteOnly] public NativeArray<uint> outColors;
            [WriteOnly] public NativeArray<float3> outNormals;
            [WriteOnly] public NativeArray<float> outHeightMap;

            public NativeArray<int> outVertCount;
            public NativeArray<int> outTriCount;

            public void Execute()
            {
                int vertCursor = 0;
                int triCursor = 0;

                for (int x = 0; x < chunkSizeX; x++)
                {
                    for (int z = 0; z < chunkSizeZ; z++)
                    {
                        int index = x * chunkSizeZ + z;
                        float topY = voxelHeights[index];
                        if (topY <= 0f) continue;

                        float blockTop = topY;
                        outHeightMap[index] = blockTop;

                        float blockBottom = topY - voxelSize; // always one full block

                        float3 posBase = new(x * voxelSize, 0f, z * voxelSize);

                        // corners
                        float3 b00 = posBase + new float3(0f, blockBottom, 0f);
                        float3 b10 = posBase + new float3(voxelSize, blockBottom, 0f);
                        float3 b01 = posBase + new float3(0f, blockBottom, voxelSize);
                        float3 b11 = posBase + new float3(voxelSize, blockBottom, voxelSize);

                        float3 t00 = posBase + new float3(0f, blockTop, 0f);
                        float3 t10 = posBase + new float3(voxelSize, blockTop, 0f);
                        float3 t01 = posBase + new float3(0f, blockTop, voxelSize);
                        float3 t11 = posBase + new float3(voxelSize, blockTop, voxelSize);

                        // --- Top face
                        AddQuad(outVertices, outTriangles, outNormals, outUVs, outColors,
                            ref vertCursor, ref triCursor,
                            t00, t10, t01, t11,
                            new float3(0, 1, 0));

                        // --- Side faces (emit only if neighbor is LOWER)

                        // +X
                        if (x == chunkSizeX - 1 || voxelHeights[(x + 1) * chunkSizeZ + z] < blockTop)
                            AddQuad(outVertices, outTriangles, outNormals, outUVs, outColors,
                                ref vertCursor, ref triCursor,
                                b10, t10, b11, t11,
                                new float3(1, 0, 0));

                        // -X
                        if (x == 0 || voxelHeights[(x - 1) * chunkSizeZ + z] < blockTop)
                            AddQuad(outVertices, outTriangles, outNormals, outUVs, outColors,
                                ref vertCursor, ref triCursor,
                                b01, t01, b00, t00,
                                new float3(-1, 0, 0));

                        // +Z
                        if (z == chunkSizeZ - 1 || voxelHeights[x * chunkSizeZ + (z + 1)] < blockTop)
                            AddQuad(outVertices, outTriangles, outNormals, outUVs, outColors,
                                ref vertCursor, ref triCursor,
                                b01, b11, t01, t11,
                                new float3(0, 0, 1));

                        // -Z
                        if (z == 0 || voxelHeights[x * chunkSizeZ + (z - 1)] < blockTop)
                            AddQuad(outVertices, outTriangles, outNormals, outUVs, outColors,
                                ref vertCursor, ref triCursor,
                                b00, t00, b10, t10,
                                new float3(0, 0, -1));
                    }
                }

                outVertCount[0] = vertCursor;
                outTriCount[0] = triCursor;
            }

            static void AddQuad(
                NativeArray<float3> verts, NativeArray<int> tris,
                NativeArray<float3> norms, NativeArray<float2> uvs,
                NativeArray<uint> cols,
                ref int vert, ref int tri,
                float3 v0, float3 v1, float3 v2, float3 v3,
                float3 normal)
            {
                if (vert + 4 > verts.Length || tri + 6 > tris.Length) return;

                verts[vert + 0] = v0;
                verts[vert + 1] = v1;
                verts[vert + 2] = v2;
                verts[vert + 3] = v3;

                norms[vert + 0] = normal;
                norms[vert + 1] = normal;
                norms[vert + 2] = normal;
                norms[vert + 3] = normal;

                if (normal.x > 0.5f)         // +X face
                {
                    uvs[vert + 0] = new float2(0, 0);
                    uvs[vert + 1] = new float2(0, 1);
                    uvs[vert + 2] = new float2(1, 0);
                    uvs[vert + 3] = new float2(1, 1);
                }
                else if (normal.x < -0.5f)   // -X face
                {
                    uvs[vert + 0] = new float2(1, 0);
                    uvs[vert + 1] = new float2(1, 1);
                    uvs[vert + 2] = new float2(0, 0);
                    uvs[vert + 3] = new float2(0, 1);
                }
                else if (normal.z > 0.5f)    // +Z face
                {
                    uvs[vert + 0] = new float2(1, 0);
                    uvs[vert + 1] = new float2(0, 0);
                    uvs[vert + 2] = new float2(1, 1);
                    uvs[vert + 3] = new float2(0, 1);
                }
                else if (normal.z < -0.5f)   // -Z face
                {
                    uvs[vert + 0] = new float2(1, 0);
                    uvs[vert + 1] = new float2(1, 1);
                    uvs[vert + 2] = new float2(0, 0);
                    uvs[vert + 3] = new float2(0, 1);
                }
                else                         // Top/bottom (you can tweak to taste)
                {
                    uvs[vert + 0] = new float2(0, 0);
                    uvs[vert + 1] = new float2(1, 0);
                    uvs[vert + 2] = new float2(0, 1);
                    uvs[vert + 3] = new float2(1, 1);
                }

                uint col = 0xFFFFFFFFu;
                cols[vert + 0] = col;
                cols[vert + 1] = col;
                cols[vert + 2] = col;
                cols[vert + 3] = col;

                if (math.abs(normal.y) < 0.1f) // horizontal side
                {
                    // Reversed triangle order for correct CCW winding
                    tris[tri + 0] = vert + 0;
                    tris[tri + 1] = vert + 1;
                    tris[tri + 2] = vert + 2;

                    tris[tri + 3] = vert + 1;
                    tris[tri + 4] = vert + 3;
                    tris[tri + 5] = vert + 2;
                }
                else
                {
                    // Top face: keep original
                    tris[tri + 0] = vert + 0;
                    tris[tri + 1] = vert + 2;
                    tris[tri + 2] = vert + 1;
                    tris[tri + 3] = vert + 1;
                    tris[tri + 4] = vert + 2;
                    tris[tri + 5] = vert + 3;
                }

                vert += 4;
                tri += 6;
            }
        }

        #endregion
    }
}
