using UnityEngine;

[CreateAssetMenu(fileName = "NewPersonality", menuName = "AI/Enemy Personality")]
public class EnemyPersonality : ScriptableObject
{
    [Header("Targeting Weights")]
    public float priorityWeight = 1f;
    public float distanceWeight = 1f;
    public float lastDamageWeight = 1f;
    public float objectiveThreatWeight = 1f;

    /// <summary>
    /// Applies weights to a TargetScore and returns the final score.
    /// </summary>
    public float CalculateScore(TargetScore ts)
    {
        float score = 0;
        score += ts.priority * -priorityWeight;
        score += ts.distance * -distanceWeight;
        score += ts.lastDamageScore * lastDamageWeight;
        score += ts.objectiveThreatScore * objectiveThreatWeight;

        ts.finalScore = score;
        return score;
    }

    public float CalculateEditorScore(TargetScore ts, Vector3 enemyPos, float simulatedRetaliateDuration)
    {
        float distanceScore = -distanceWeight * Vector3.Distance(enemyPos, ts.GetPosition());
        float retaliationScore = ts.wasAttacker
            ? lastDamageWeight * Mathf.Max(0f, 1f - ts.timeSinceAttack / simulatedRetaliateDuration)
            : 0f;
        float objectiveScore = ts.objectiveThreatScore * objectiveThreatWeight;
        float priorityScore = ts.priority * -priorityWeight;

        float baseScore = priorityScore + distanceScore + retaliationScore + objectiveScore;

        return baseScore;
    }
}
