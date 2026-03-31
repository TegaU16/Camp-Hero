using System.Collections.Generic;
using UnityEngine;

namespace Game.Terrain
{
    [CreateAssetMenu(fileName = "NewBiome", menuName = "Voxel/Biome")]
    public class BiomeData : ScriptableObject
    {
        public string biomeName;
        public NoiseSettings noiseSettings;
        public List<GameObject> treePrefabs = new();
        public List<GameObject> rockPrefabs = new();
    }
}
