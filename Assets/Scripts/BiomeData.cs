using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBiome", menuName = "Voxel/Biome")]
public class BiomeData : ScriptableObject
{
    public string biomeName;
    public NoiseSettings noiseSettings;
    public List<GameObject> treePrefabs = new();
    public List<GameObject> rockPrefabs = new();
    public List<GameObject> treeClusterPrefabs = new();
    public List<GameObject> rockClusterPrefabs = new();
}
