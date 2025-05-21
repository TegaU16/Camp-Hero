public interface ISaveableObject
{
    string SaveState();
    void LoadState(string json);
}
