using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Terrain
{
    public class TerrainPreview : MonoBehaviour
    {
        public struct PreviewData
        {
            public GameObject targetObject;
            public BiomeData biome;
            public Vector3 chunkPosition;
            public int customChunkSize;
            public int customSeed;
        };

        [SerializeField] private VoxelGrid voxelGrid;
        [SerializeField] private BiomeData previewBiome;

        [SerializeField] private int previewChunkSize = 32;
        [SerializeField] private int previewRadius = 1; // 1 = 3x3 grid
        [SerializeField] private int seed = 12345;

        [SerializeField] private Vector3 previewPosition = Vector3.zero;

        public void GeneratePreview()
        {
            if (voxelGrid == null || previewBiome == null) return;

            ClearPreview();

            for (int x = -previewRadius; x <= previewRadius; x++)
            {
                for (int z = -previewRadius; z <= previewRadius; z++)
                {
                    GameObject chunkObj = new($"Chunk_{x}_{z}");
                    chunkObj.transform.parent = transform;

                    Vector3 chunkPos = previewPosition + new Vector3(
                        x * previewChunkSize,
                        0,
                        z * previewChunkSize
                    );

                    chunkObj.transform.localPosition = new Vector3(
                        x * previewChunkSize,
                        0,
                        z * previewChunkSize
                    );

                    PreviewData chunkPreview = new()
                    {
                        targetObject = chunkObj,
                        biome = previewBiome,
                        chunkPosition = chunkPos,
                        customChunkSize = previewChunkSize,
                        customSeed = seed
                    };

                    GeneratePreviewChunk(chunkPreview);
                }
            }
        }

        private void GeneratePreviewChunk(PreviewData previewData)
        {
            seed = previewData.customSeed;
            VoxelChunk previewChunk = new(previewData.targetObject, previewData.customChunkSize);
            GenerateChunkTerrainPreview(previewChunk, previewData);
        }

        private void GenerateChunkTerrainPreview(VoxelChunk chunk, PreviewData previewData)
        {
            chunk.biome = previewData.biome;
            int customChunkSize = previewData.customChunkSize;
            Vector3 chunkPosition = previewData.chunkPosition;

            // Map biome.noiseSettings to BurstNoise / NoiseLayer
            NoiseSettings biomeNoiseSettings = chunk.biome.noiseSettings;

            // Build a single NoiseLayer for this chunk (B1)
            NoiseLayer[] managedLayer = new NoiseLayer[1];

            BurstNoise burstNoise = BurstNoise.Default(seed);

            burstNoise.SetFrequency(biomeNoiseSettings.frequency);
            burstNoise.SetFractalOctaves(biomeNoiseSettings.octaves);
            burstNoise.SetLacunarity(biomeNoiseSettings.lacunarity);
            burstNoise.SetFractalGain(biomeNoiseSettings.persistence);

            NoiseLayer nl = new()
            {
                noise = burstNoise,
                offset = new float3(0, 0, 0),
                scale = biomeNoiseSettings.baseScale,
                weight = 1f
            };

            managedLayer[0] = nl;

            // Bake into NativeArray<NoiseLayer> (TempJob lifetime)
            NativeArray<NoiseLayer> bakedLayers = new(1, Allocator.TempJob);
            bakedLayers[0] = managedLayer[0];

            // --- Allocate native arrays for noise & height ---
            int total = previewData.customChunkSize * previewData.customChunkSize;
            NativeArray<float> noiseNative = new(total, Allocator.TempJob);

            // Prepare height curve table if needed
            NativeArray<float> curveTable = new(256, Allocator.TempJob);
            if (biomeNoiseSettings.heightCurve != null)
            {
                // Bake animation curve into 256 samples
                for (int i = 0; i < 256; i++)
                {
                    float t = i / 255f;
                    curveTable[i] = biomeNoiseSettings.heightCurve.Evaluate(t);
                }
            }
            else
            {
                // keep as zeros (will not be used)
                for (int i = 0; i < 256; i++)
                    curveTable[i] = 0f;
            }

            // --- Schedule noise job ---
            TerrainGenerator.NoiseMapJob2D noiseJob = new()
            {
                size = new int2(customChunkSize, customChunkSize),
                worldOffset = new float2(chunkPosition.x, chunkPosition.z),
                layers = bakedLayers,
                noiseOut = noiseNative
            };

            JobHandle noiseHandle = noiseJob.Schedule(total, 64);

            // New voxel count array
            NativeArray<float> heights = new(total, Allocator.TempJob);
            int maxStackHeight = Mathf.CeilToInt(biomeNoiseSettings.heightScale / voxelGrid.voxelSize);

            // --- Schedule height generation job ---
            TerrainGenerator.GenerateHeightMapJob heightJob = new()
            {
                noiseIn = noiseNative,
                heightCurve = curveTable,
                heightExponent = biomeNoiseSettings.heightExponent,
                heightMultiplier = biomeNoiseSettings.heightScale,
                heightOffset = voxelGrid.heightOffset,

                heightOut = heights
            };

            JobHandle heightHandle = heightJob.Schedule(total, 64, noiseHandle);

            // Mesh arrays (same as before)
            int maxFaces = total * maxStackHeight * 5; // conservative estimate
            NativeArray<float3> vertsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<int> trisNative = new(maxFaces * 6, Allocator.TempJob);
            NativeArray<float2> uvsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<uint> colsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<float3> normsNative = new(maxFaces * 4, Allocator.TempJob);
            NativeArray<int> outVertCount = new(1, Allocator.TempJob);
            NativeArray<int> outTriCount = new(1, Allocator.TempJob);
            NativeArray<float> outHeightMap = new(customChunkSize * customChunkSize, Allocator.TempJob);

            TerrainGenerator.MeshBuildJob meshJob = new()
            {
                chunkSizeX = customChunkSize,
                chunkSizeZ = customChunkSize,
                voxelSize = voxelGrid.voxelSize,
                voxelHeights = heights, // use the new integer array

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

            chunk.heightMap = new float[customChunkSize * customChunkSize];

            for (int z = 0; z < customChunkSize; z++)
            {
                for (int x = 0; x < customChunkSize; x++)
                {
                    int i = x + z * customChunkSize;
                    chunk.heightMap[i] = outHeightMap[i];
                }
            }

            // --- Read back and create managed mesh arrays ---
            int vertCount = outVertCount[0];
            int triCount = outTriCount[0];

            vertCount = math.min(vertCount, vertsNative.Length);
            triCount = math.min(triCount, trisNative.Length);

            Vector3[] meshVerts = new Vector3[vertCount];
            Vector3[] meshNormals = new Vector3[vertCount];
            Vector2[] meshUVs = new Vector2[vertCount];
            Color32[] meshColors = new Color32[vertCount];

            for (int i = 0; i < vertCount; i++)
            {
                float3 v = vertsNative[i];
                meshVerts[i] = new Vector3(v.x, v.y, v.z);

                float3 n = normsNative[i];
                meshNormals[i] = new Vector3(n.x, n.y, n.z);

                float2 uv = uvsNative[i];
                meshUVs[i] = new Vector2(uv.x, uv.y);

                uint c = colsNative[i];
                byte r = (byte)(c & 0xFF);
                byte g = (byte)((c >> 8) & 0xFF);
                byte b = (byte)((c >> 16) & 0xFF);
                byte a = (byte)((c >> 24) & 0xFF);
                meshColors[i] = new Color32(r, g, b, a);
            }

            int[] meshTris = new int[triCount];
            for (int i = 0; i < triCount; i++)
                meshTris[i] = trisNative[i];

            Mesh mesh = new()
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = meshVerts,
                triangles = meshTris,
                uv = meshUVs,
                colors32 = meshColors,
                normals = meshNormals
            };
            mesh.RecalculateBounds();

            if (!chunk.chunkObject.TryGetComponent(out MeshFilter mf))
                mf = chunk.chunkObject.AddComponent<MeshFilter>();

            if (mf.sharedMesh != null)
                DestroyImmediate(mf.sharedMesh);

            mf.sharedMesh = mesh;

            if (!chunk.chunkObject.TryGetComponent(out MeshRenderer mr))
                mr = chunk.chunkObject.AddComponent<MeshRenderer>();

            mr.sharedMaterial = voxelGrid.voxelMaterial;

            chunk.chunkObject.SetActive(true);
            chunk.generatedMesh = mesh;

            // --- Dispose native arrays ---
            noiseNative.Dispose();
            bakedLayers.Dispose();
            curveTable.Dispose();
            heights.Dispose();

            vertsNative.Dispose();
            trisNative.Dispose();
            uvsNative.Dispose();
            colsNative.Dispose();
            normsNative.Dispose();
            outVertCount.Dispose();
            outTriCount.Dispose();
            outHeightMap.Dispose();
        }

        public void ClearPreview()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                GeneratePreview();
            }
        }
    }
}
