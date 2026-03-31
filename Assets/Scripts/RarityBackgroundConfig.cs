using System.Collections.Generic;
using System.Linq;
using Game.Inventory;
using UnityEngine;

[CreateAssetMenu(menuName = "Rarity/Backgrounds")]
public class RarityBackgroundConfig : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public Rarity rarity;
        public Sprite background;
    }

    [SerializeField] private Entry[] entries;
    public Sprite defaultBackground;

    private Dictionary<Rarity, Sprite> cache;

    public Sprite GetBackground(Rarity rarity)
    {
        cache ??= entries.ToDictionary(entry => entry.rarity, entry => entry.background);
        return cache.TryGetValue(rarity, out Sprite background) ? background : defaultBackground;
    }
}
