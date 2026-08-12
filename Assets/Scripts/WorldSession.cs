namespace Worlds
{
    public static class WorldSession
    {
        public static string CurrentWorldName;
        public static string CurrentSeed;

        public static RunStats CurrentRunStats;
    }

    public enum WorldState
    {
        Active,
        Failed,
        Won
    }

    public enum WorldType
    {
        Normal,
        Tutorial
    }
}
