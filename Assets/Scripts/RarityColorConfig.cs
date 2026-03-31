using System.Collections.Generic;
using System.Linq;
using Game.Inventory;
using UnityEngine;

[CreateAssetMenu(menuName = "Rarity/Colors")]
public class RarityColorConfig : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public Rarity rarity;
        public Color color;
    }

    [SerializeField] private Entry[] entries;

    private Dictionary<Rarity, Color> cache;

    public Color GetColor(Rarity rarity)
    {
        cache ??= entries.ToDictionary(entry => entry.rarity, entry => entry.color);
        return cache.TryGetValue(rarity, out Color color) ? color : Color.white;
    }
}
