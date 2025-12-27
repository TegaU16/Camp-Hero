using UnityEngine;

public enum Difficulty
{
    Easy,
    Medium,
    Hard,
    Gamer
}

public enum EntityType
{
    Organism,
    Static
}

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;
    private Difficulty currentDifficulty;

    public delegate void DifficultyChangedDelegate(bool reset);
    public event DifficultyChangedDelegate OnDifficultyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this);

        currentDifficulty = Difficulty.Easy;
    }

    public float GetHealthMultiplier(EntityType entityType)
    {
        return (entityType, currentDifficulty) switch
        {
            (EntityType.Organism, Difficulty.Easy) => 0.5f,
            (EntityType.Organism, Difficulty.Medium) => 0.8f,
            (EntityType.Organism, Difficulty.Hard) => 1f,
            (EntityType.Organism, Difficulty.Gamer) => 1.2f,

            (EntityType.Static, Difficulty.Easy) => 0.7f,
            (EntityType.Static, Difficulty.Medium) => 1f,
            (EntityType.Static, Difficulty.Hard) => 1.2f,
            (EntityType.Static, Difficulty.Gamer) => 1.5f,

            _ => 1f
        };
    }

    public float GetDamageMultiplier()
    {
        return currentDifficulty switch
        {
            Difficulty.Easy => 0.5f,
            Difficulty.Medium => 1f,
            Difficulty.Hard => 1.5f,
            Difficulty.Gamer => 3f,
            _ => 1f
        };
    }

    public float GetExpMultiplier()
    {
        return currentDifficulty switch
        {
            Difficulty.Easy => 1f,
            Difficulty.Medium => 1.5f,
            Difficulty.Hard => 2f,
            Difficulty.Gamer => 3f,
            _ => 1f
        };
    }

    public void SetDifficulty(Difficulty difficulty)
    {
        if (currentDifficulty == difficulty) return;

        currentDifficulty = difficulty;

        // Notify all listeners
        OnDifficultyChanged?.Invoke(reset: false);
    }

    public Difficulty GetDifficulty() => currentDifficulty;
}
