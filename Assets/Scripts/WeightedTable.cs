using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeightedEntry<T>
{
    public T value;
    public float weight;
}

[Serializable]
public class WeightedTable<T>
{
    [SerializeField] private List<WeightedEntry<T>> entries = new();

    public List<WeightedEntry<T>> Entries => entries;

    public void Add(T value, float weight)
    { 
        entries.Add(new WeightedEntry<T>
        {
            value = value,
            weight = weight
        });
    }

    public T Roll()
    {
        float totalWeight = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].weight > 0f)
                totalWeight += entries[i].weight;
        }

        if (totalWeight <= 0f) return default;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            WeightedEntry<T> entry = entries[i];
            if (entry.weight <= 0f) continue;

            cumulative += entry.weight;
            if (roll <= cumulative) return entry.value;
        }

        return entries[^1].value;
    }

    public T Roll(Func<T, bool> filter)
    {
        float totalWeight = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            WeightedEntry<T> entry = entries[i];

            if (entry.weight > 0f && filter(entry.value))
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f) return default;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            WeightedEntry<T> entry = entries[i];
            if (entry.weight <= 0f || !filter(entry.value)) continue;

            cumulative += entry.weight;
            if (roll <= cumulative) return entry.value;
        }

        return default;
    }
}