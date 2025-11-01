[System.Serializable]
public class RunStats
{
    public int totalEnemiesDefeated;
    public int eliteEnemiesDefeated;
    public int blightEnemiesDefeated;

    public int resourcesCollected;
    public int structuresBuilt;
    public int animalsKilled;
    public int cowsViolated;
    public int playerDeaths;
    public int damageTaken;

    public int totalDamageDealt;
    public int damageDealtToEnemies;
    public int damageDealtToResources;

    public int totalExpGained;
    public float daysSurvived;

    public void ResetStats()
    {
        totalEnemiesDefeated = 0;
        eliteEnemiesDefeated = 0;
        blightEnemiesDefeated = 0;

        resourcesCollected = 0;
        structuresBuilt = 0;
        animalsKilled = 0;
        cowsViolated = 0;
        playerDeaths = 0;
        damageTaken = 0;

        totalDamageDealt = 0;
        damageDealtToEnemies = 0;
        damageDealtToResources = 0;

        totalExpGained = 0;
        daysSurvived = 0f;
    }
}
