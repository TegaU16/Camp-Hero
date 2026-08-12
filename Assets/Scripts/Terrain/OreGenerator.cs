using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Terrain
{
    public class OreGenerator : MonoBehaviour
    {
        public static OreGenerator Instance;

        [Header("Ore Distribution")]
        [SerializeField] private List<OreBand> oreBands = new();

        [Header("Ore Prefabs")]
        [SerializeField] private List<OrePrefabSet> orePrefabSets = new();

        private readonly List<OreDeposit> deposits = new();

        private Dictionary<OreType, OrePrefabSet> prefabLookup;
        private readonly Dictionary<VoxelChunk, OreDeposit> depositsLookup = new();

        private float worldRadius;

        private int seed;
        private int chunkSize;

        private Vector3 halfChunk;

        private System.Random rng = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Initialize()
        {
            seed = VoxelGrid.Instance.seed;
            chunkSize = VoxelGrid.Instance.chunkSize;

            halfChunk = new Vector3(chunkSize * 0.5f, 0, chunkSize * 0.5f);

            float terrainWidth = chunkSize * VoxelGrid.Instance.gridSize;
            worldRadius = terrainWidth * VoxelGrid.Instance.outerRadius;

            rng = new System.Random(seed);

            BuildPrefabLookup();
            GenerateDeposits();
        }

        private void BuildPrefabLookup()
        {
            prefabLookup = new Dictionary<OreType, OrePrefabSet>();

            foreach (OrePrefabSet set in orePrefabSets)
            {
                if (!prefabLookup.ContainsKey(set.oreType))
                    prefabLookup.Add(set.oreType, set);
            }
        }

        private void GenerateDeposits()
        {
            foreach (VoxelChunk chunk in VoxelGrid.Instance.chunks)
                TryGenerateDepositsForChunk(chunk);
        }

        private void TryGenerateDepositsForChunk(VoxelChunk chunk)
        {
            Vector3 chunkPosition = chunk.chunkObject.transform.position;
            Vector2 chunkPos = new(chunkPosition.x, chunkPosition.z);
            Vector2 worldCenterPos = new(VoxelGrid.Instance.worldCenter.x, VoxelGrid.Instance.worldCenter.z);

            float distance = Vector2.Distance(chunkPos, worldCenterPos);
            float radiusPercent = distance / worldRadius;

            foreach (OreBand band in oreBands)
            {
                if (radiusPercent < band.minRadiusPercent) continue;
                if (radiusPercent > band.maxRadiusPercent) continue;

                int chunkX = Mathf.RoundToInt(chunkPosition.x / chunkSize);
                int chunkZ = Mathf.RoundToInt(chunkPosition.z / chunkSize);

                float seedOffsetX = (seed % 100000) * 0.0137f;
                float seedOffsetZ = ((seed / 100000) % 100000) * 0.0293f;

                float noiseX = chunkX * 0.08f + seedOffsetX;
                float noiseZ = chunkZ * 0.08f + seedOffsetZ;

                float noise = Mathf.PerlinNoise(noiseX, noiseZ);
                if (noise >= band.depositChance) continue;

                float depositRadius = Mathf.Lerp(band.minDepositRadius, band.maxDepositRadius, noise);
                Vector3 center = chunkPosition + halfChunk;

                List<Vector3> orePositions = GenerateOrePositions(band, center, depositRadius);

                OreDeposit deposit = new(band.oreType, center, depositRadius, seed, orePositions);
                depositsLookup[chunk] = deposit;
            }
        }

        public GameObject GetOrePrefab(OreType type, OreSize size)
        {
            if (!prefabLookup.TryGetValue(type, out OrePrefabSet set))
            {
                Debug.LogError($"Missing ore prefab set for {type}");
                return null;
            }

            return size == OreSize.Large ? set.largePrefab : set.smallPrefab;
        }

        public OreSize GetOreSize(Vector3 position, OreDeposit deposit)
        {
            Vector2 worldPos = new(position.x, position.z);
            Vector2 depositPos = new(deposit.center.x, deposit.center.z);

            float distance = Vector2.Distance(worldPos, depositPos);
            float percent = distance / deposit.radius;
            if (percent < 0.35f) return OreSize.Large;

            int hash = Mathf.FloorToInt(position.x) * 73856093 ^ Mathf.FloorToInt(position.z) * 19349663 ^ seed;
            return Mathf.Abs(hash % 100) < 20 ? OreSize.Large : OreSize.Small;
        }

        public bool TryGetDepositAt(VoxelChunk chunk, out OreDeposit found)
        {
            found = null;
            if (depositsLookup.TryGetValue(chunk, out OreDeposit deposit))
                found = deposit;

            return found != null;
        }

        public bool ShouldPlaceOre(OreDeposit deposit, Vector3Int position)
        {
            if (deposit == null) return false;
            return !TerrainGenerator.Instance.IsOccupied(position);
        }

        private List<Vector3> GenerateOrePositions(OreBand band, Vector3 center, float radius)
        {
            int targetCount = rng.Next(
                band.minOreCount,
                band.maxOreCount + 1
            );

            int spacing = Mathf.Max(1, band.minOreSpacing);

            Vector3Int centerCell = Vector3Int.RoundToInt(center);
            int radiusInBlocks = Mathf.FloorToInt(radius);
            int radiusSqr = radiusInBlocks * radiusInBlocks;

            List<Vector3Int> candidates = new();

            int phaseX = rng.Next(spacing);
            int phaseZ = rng.Next(spacing);

            for (int x = -radiusInBlocks; x <= radiusInBlocks; x++)
            {
                if (PositiveModulo(x - phaseX, spacing) != 0) continue;

                for (int z = -radiusInBlocks; z <= radiusInBlocks; z++)
                {
                    if (PositiveModulo(z - phaseZ, spacing) != 0) continue;
                    if (x * x + z * z > radiusSqr) continue;

                    candidates.Add(new Vector3Int(x, 0, z));
                }
            }

            List<Vector3> positions = new(targetCount)
            {
                centerCell
            };

            int spacingSqr = spacing * spacing;

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i].sqrMagnitude < spacingSqr)
                    candidates.RemoveAt(i);
            }

            int required = Mathf.Min(targetCount - 1, candidates.Count);

            for (int i = 0; i < required; i++)
            {
                int randomIndex = rng.Next(i, candidates.Count);
                (candidates[i], candidates[randomIndex]) = (candidates[randomIndex], candidates[i]);

                Vector3Int cell = centerCell + candidates[i];
                positions.Add(cell);
            }

            return positions;
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
