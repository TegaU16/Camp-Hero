using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PersonalityComparer : EditorWindow
{
    private readonly List<EnemyPersonality> personalities = new();
    private readonly List<TargetScore> targets = new();

    private Vector3 enemySimPosition = Vector3.zero;
    private float simulatedRetaliateDuration = 3f;

    private Vector2 scrollPos;

    // Foldout toggles
    private bool showTargets = true;
    private bool showPersonalities = true;
    private bool showResults = true;

    [MenuItem("AI Tools/Personality Comparer")]
    public static void ShowWindow() => GetWindow<PersonalityComparer>("Personality Comparer");

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Enemy Personality Comparer", EditorStyles.boldLabel);

        // Enemy simulated position
        enemySimPosition = EditorGUILayout.Vector3Field("Enemy Position", enemySimPosition);
        simulatedRetaliateDuration = EditorGUILayout.FloatField("Retaliation Duration", simulatedRetaliateDuration);

        GUILayout.Space(10);

        // Targets
        showTargets = EditorGUILayout.Foldout(showTargets, "Targets", true);
        if (showTargets)
        {
            int removeTargetIndex = -1;
            for (int i = 0; i < targets.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                targets[i].targetName = EditorGUILayout.TextField("Name", targets[i].targetName);
                targets[i].simulatedPosition = EditorGUILayout.Vector3Field("Position", targets[i].simulatedPosition);
                targets[i].priority = EditorGUILayout.IntField("Priority", targets[i].priority);
                targets[i].objectiveThreatScore = EditorGUILayout.FloatField("Objective", targets[i].objectiveThreatScore);
                targets[i].wasAttacker = EditorGUILayout.Toggle("Was Attacker", targets[i].wasAttacker);
                targets[i].timeSinceAttack = EditorGUILayout.FloatField("Time Since Attack", targets[i].timeSinceAttack);

                if (GUILayout.Button("Remove Target"))
                    removeTargetIndex = i;

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
            if (removeTargetIndex >= 0) targets.RemoveAt(removeTargetIndex);
            if (GUILayout.Button("Add Target")) targets.Add(new TargetScore("Target", Vector3.zero));
        }

        GUILayout.Space(10);

        // Personalities
        showPersonalities = EditorGUILayout.Foldout(showPersonalities, "Personalities", true);
        if (showPersonalities)
        {
            int removePersonalityIndex = -1;
            for (int i = 0; i < personalities.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                personalities[i] = (EnemyPersonality)EditorGUILayout.ObjectField(personalities[i], typeof(EnemyPersonality), false);
                if (GUILayout.Button("X", GUILayout.Width(20))) removePersonalityIndex = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removePersonalityIndex >= 0) personalities.RemoveAt(removePersonalityIndex);
            if (GUILayout.Button("Add Personality")) personalities.Add(null);
        }

        GUILayout.Space(10);

        // Results Foldout
        showResults = EditorGUILayout.Foldout(showResults, "Results", true);
        if (showResults)
        {
            foreach (EnemyPersonality p in personalities)
            {
                if (p == null) continue;

                TargetScore bestTarget = null;
                float bestScore = float.MinValue;

                foreach (TargetScore ts in targets)
                {
                    float score = p.CalculateEditorScore(ts, enemySimPosition, simulatedRetaliateDuration);
                    ts.finalScore = score;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = ts;
                    }
                }

                if (bestTarget != null)
                {
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField($"{p.name} chooses: {bestTarget.targetName} (Score: {bestScore:F2})", EditorStyles.boldLabel);
                    GUILayout.Space(3);

                    // Table header
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Target", GUILayout.Width(120));
                    EditorGUILayout.LabelField("Priority", GUILayout.Width(70));
                    EditorGUILayout.LabelField("Distance", GUILayout.Width(70));
                    EditorGUILayout.LabelField("Retaliation", GUILayout.Width(90));
                    EditorGUILayout.LabelField("Objective", GUILayout.Width(80));
                    EditorGUILayout.LabelField("Total", GUILayout.Width(70));
                    EditorGUILayout.EndHorizontal();

                    // Table rows
                    foreach (TargetScore ts in targets)
                    {
                        bool isBest = ts == bestTarget;
                        GUIStyle style = new(EditorStyles.label);
                        style.normal.textColor = isBest ? Color.green : Color.red;

                        float priorityPart = ts.priority * p.priorityWeight;
                        float distancePart = -p.distanceWeight * Vector3.Distance(enemySimPosition, ts.GetPosition());
                        float retaliationPart = ts.wasAttacker
                            ? p.lastDamageWeight * Mathf.Max(0f, 1f - ts.timeSinceAttack / simulatedRetaliateDuration)
                            : 0f;
                        float objectivePart = ts.objectiveThreatScore * p.objectiveThreatWeight;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(ts.targetName, style, GUILayout.Width(120));
                        EditorGUILayout.LabelField(priorityPart.ToString("F2"), style, GUILayout.Width(70));
                        EditorGUILayout.LabelField(distancePart.ToString("F2"), style, GUILayout.Width(70));
                        EditorGUILayout.LabelField(retaliationPart.ToString("F2"), style, GUILayout.Width(90));
                        EditorGUILayout.LabelField(objectivePart.ToString("F2"), style, GUILayout.Width(80));
                        EditorGUILayout.LabelField(ts.finalScore.ToString("F2"), style, GUILayout.Width(70));
                        EditorGUILayout.EndHorizontal();
                    }

                    GUILayout.Space(5);
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }
}
