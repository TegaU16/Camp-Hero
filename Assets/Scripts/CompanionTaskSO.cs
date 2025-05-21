using UnityEngine;

public abstract class CompanionTaskSO : ScriptableObject
{
    public string taskName;
    public bool isEnabled = true;

    public abstract void Execute(Companion companion);
}
