using UnityEngine;

[System.Serializable]
public class TargetScore
{
    // Runtime target (live game)
    public Transform target;

    // Editor-only / simulation
    public string targetName = "Null";
    public Vector3 simulatedPosition;
    public bool wasAttacker;
    public float timeSinceAttack = Mathf.Infinity;

    // Scoring weights
    public int priority;
    public float distance;
    public float lastDamageScore;
    public float objectiveThreatScore;

    // Final score for personality comparison
    public float finalScore;

    // Constructor for runtime target
    public TargetScore(Transform target, int priority = 0, float distance = 0f, float lastDamageScore = 0f, float objectiveThreatScore = 0f)
    {
        this.target = target;
        this.targetName = target != null ? target.name : "Null";
        this.priority = priority;
        this.distance = distance;
        this.lastDamageScore = lastDamageScore;
        this.objectiveThreatScore = objectiveThreatScore;
        finalScore = priority + distance + lastDamageScore + objectiveThreatScore;

        simulatedPosition = target != null ? target.position : Vector3.zero;
    }

    // Constructor for editor/simulation target
    public TargetScore(string name, Vector3 simPos, int priority = 0, float objScore = 0f, bool wasAttacker = false, float timeSinceAttack = Mathf.Infinity)
    {
        target = null;
        targetName = name;
        simulatedPosition = simPos;
        this.priority = priority;
        objectiveThreatScore = objScore;
        this.wasAttacker = wasAttacker;
        this.timeSinceAttack = timeSinceAttack;
        finalScore = 0f;
    }

    // Helper to get the position for scoring
    public Vector3 GetPosition()
    {
        return target != null ? target.position : simulatedPosition;
    }
}
