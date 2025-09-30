using System.Collections.Generic;
using UnityEngine;

public class PlantRegistry : MonoBehaviour
{
    public PlantData[] allPlants;

    private static Dictionary<string, PlantData> plantDict;

    void Awake()
    {
        if (plantDict != null && plantDict.Count > 0) return;

        plantDict = new();

        foreach (PlantData plantData in allPlants)
        {
            if (plantData == null) continue;

            string id = plantData.plant.itemName;
            if (id == null || string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"Plant {plantData.name} has no PlantID key!");
                continue;
            }

            plantDict[id] = plantData;
        }
    }

    public static PlantData GetPlantByKey(string key) =>
        plantDict.TryGetValue(key, out PlantData plant) ? plant : null;
}
