using UnityEditor;
using UnityEngine;
using Game.Inventory;

public static class ItemAutoPopulator
{
    public static void AutoPopulate(Item item)
    {
        item.itemName = item.name;

        item.icon = FindAsset<Sprite>(
            $"{item.itemName} Icon",
            new[] { "Assets/Sprites/Icons/Items" }
        );

        item.equippedPrefab = FindAsset<GameObject>(
            $"{item.itemName} Equip",
            new[] { "Assets/Prefabs" }
        );

        item.itemDrop = FindAsset<GameObject>(
            $"{item.itemName} Drop",
            new[] { "Assets/Prefabs" }
        );

        item.buildingGhost = FindAsset<GameObject>(
            $"{item.itemName} Build",
            new[] { "Assets/Prefabs" }
        );

        EditorUtility.SetDirty(item);
    }

    private static T FindAsset<T>(string assetName, string[] folders) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets(assetName, folders);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null && asset.name == assetName) return asset;
        }

        return null;
    }

    [MenuItem("Tools/Items/Auto-Populate All Items")]
    public static void AutoPopulateAllItems()
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:Item",
            new[] { "Assets/Items" }
        );

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Item item = AssetDatabase.LoadAssetAtPath<Item>(path);

            if (item == null) continue;

            AutoPopulate(item);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Auto-populated all items.");
    }
}
