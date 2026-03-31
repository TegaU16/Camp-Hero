using System.Collections.Generic;

public class MultiplierStat
{
    private readonly Dictionary<object, float> sources = new();
    private readonly float baseMultiplier;

    public MultiplierStat(float baseMultiplier = 1f)
    {
        this.baseMultiplier = baseMultiplier;
    }

    public float Total
    {
        get
        {
            float total = baseMultiplier;
            foreach (float mult in sources.Values)
                total *= mult;

            return total;
        }
    }

    public void SetSource(object source, float multiplier)
    {
        if (source == null || multiplier == 0) return;
        sources[source] = multiplier;
    }

    public void RemoveSource(object source)
    {
        if (source == null) return;
        sources.Remove(source);
    }
}
