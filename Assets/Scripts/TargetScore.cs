using UnityEngine;

public class TargetScore
{
    public Transform target;
    public int priority; // index in preferredTargets list or int.MaxValue
    public float distance;
    public float lastDamageScore;
    public float objectiveThreatScore;
    public float finalScore;

    public TargetScore(Transform target, int priority, float distance, float lastDamageScore, float objectiveThreatScore)
    {
        this.target = target;
        this.priority = priority;
        this.distance = distance;
        this.lastDamageScore = lastDamageScore;
        this.objectiveThreatScore = objectiveThreatScore;

        finalScore = priority + distance + lastDamageScore + objectiveThreatScore;
    }
}
