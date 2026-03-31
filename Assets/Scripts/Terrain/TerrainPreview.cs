using UnityEngine;

namespace Game.Terrain
{
    public class TerrainPreview : MonoBehaviour
    {
        public VoxelGrid terrainGenerator;
        public BiomeData previewBiome;

        public int previewChunkSize = 32;
        public int previewRadius = 1; // 1 = 3x3 grid

        public int seed = 12345;

        public Vector3 previewPosition = Vector3.zero;

        public void GeneratePreview()
        {
            if (terrainGenerator == null || previewBiome == null) return;

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

                    terrainGenerator.GeneratePreviewChunk(
                        chunkObj,
                        previewBiome,
                        chunkPos,
                        previewChunkSize,
                        seed
                    );
                }
            }
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
