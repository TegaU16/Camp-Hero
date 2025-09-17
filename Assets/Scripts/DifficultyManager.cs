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

    public event System.Action<Difficulty> OnDifficultyChanged;

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
            (EntityType.Organism, Difficulty.Medium) => 1f,
            (EntityType.Organism, Difficulty.Hard) => 1.6f,
            (EntityType.Organism, Difficulty.Gamer) => 2.5f,

            (EntityType.Static, Difficulty.Easy) => 0.7f,
            (EntityType.Static, Difficulty.Medium) => 1f,
            (EntityType.Static, Difficulty.Hard) => 1.3f,
            (EntityType.Static, Difficulty.Gamer) => 1.8f,

            _ => 1f,
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
            _ => 1f,
        };
    }

    public void SetDifficulty(Difficulty difficulty)
    {
        if (currentDifficulty == difficulty)
            return;

        currentDifficulty = difficulty;

        // Notify all listeners
        OnDifficultyChanged?.Invoke(currentDifficulty);
    }

    public Difficulty GetDifficulty() => currentDifficulty;
}
