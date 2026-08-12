using System.Collections.Generic;
using Game.Players;
using UnityEngine;

namespace Game.AI.Enemies
{
    [RequireComponent(typeof(Enemy))]
    public class EnemyTargeting : MonoBehaviour
    {
        [SerializeField] private EnemyPersonality personality;
        private float nextTargetSwitchTime;
        private readonly float retaliateMemoryDuration = 5f;
        private float lastAttackedTime = -999f;

        [SerializeField] private TargetPriority targetPriority = new();

        [SerializeField] private float targetStickTime = 3f;

        private Transform myTransform;
        private Transform currentTarget;
        private Transform recentAttacker;
        private Transform campfireTarget;

        [HideInInspector] public bool pendingTargetReassign;

        public Transform CurrentTarget => currentTarget;

        private Enemy enemy;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            targetPriority.Validate();
        }
        #endif

        public void ValidateTarget(List<Targetable> potentialTargets)
        {
            if (Time.time >= nextTargetSwitchTime)
            {
                nextTargetSwitchTime = 0f;
                AssignBestTarget(potentialTargets);
                return;
            }

            if (currentTarget != null && currentTarget.gameObject.activeInHierarchy) return;

            nextTargetSwitchTime = 0f;
            AssignBestTarget(potentialTargets);
        }

        public void AssignBestTarget(List<Targetable> potentialTargets)
        {
            if (enemy.IsCurrentState(Enemy.State.Attacking) || enemy.IsCurrentState(Enemy.State.RangedAttacking)) return;
            if (currentTarget != null && Time.time < nextTargetSwitchTime) return;

            Transform bestTarget = null;
            float bestScore = float.MinValue;

            foreach (Targetable target in potentialTargets)
            {
                if (target == null || !target.gameObject.activeInHierarchy) continue;

                TargetScore targetScore = CreateTargetScore(target.transform);
                float score = personality.CalculateScore(targetScore);

                if (score <= bestScore) continue;

                bestScore = score;
                bestTarget = target.transform;
            }

            if (bestTarget != null)
                SetCurrentTarget(bestTarget);
            else if (campfireTarget != null)
                SetCurrentTarget(campfireTarget);
            else
                SetCurrentTarget(GetPlayer());

            nextTargetSwitchTime = Time.time + targetStickTime;
            enemy.ResetPath();
        }

        private TargetScore CreateTargetScore(Transform target)
        {
            float distance = Vector3.Distance(myTransform.position, target.position);

            Targetable targetable = target.GetComponent<Targetable>();
            int priority = targetable != null ? targetPriority.GetPriority(targetable.targetType) : int.MaxValue;

            float retaliationBias = 0f;
            if (target == recentAttacker && Time.time - lastAttackedTime <= retaliateMemoryDuration)
            {
                float freshness = 1f - ((Time.time - lastAttackedTime) / retaliateMemoryDuration);
                retaliationBias = personality.lastDamageWeight * freshness;
            }

            float objectiveScore = 0f;
            if (targetable != null)
                objectiveScore = GetObjectiveThreat(targetable, personality);

            return new TargetScore(target, priority, distance, retaliationBias, objectiveScore);
        }

        private float GetObjectiveThreat(Targetable targetable, EnemyPersonality personality)
        {
            float objWeight = personality.objectiveThreatWeight;

            return targetable.targetType switch
            {
                Targetable.TargetType.Campfire => objWeight * 4,
                Targetable.TargetType.Player => objWeight * 5,
                Targetable.TargetType.Defense => objWeight * 6,
                Targetable.TargetType.Wall => objWeight * 3,
                Targetable.TargetType.Structure => objWeight * 2,
                _ => objWeight,
            };
        }

        public void SetCurrentTarget(Transform target) => currentTarget = target;

        public void SetCampfireTarget(Transform target) => campfireTarget = target;

        public void FallbackToCampfire() => currentTarget = campfireTarget;

        public void SetRecentAttacker(Transform attacker)
        {
            Debug.Log(
                $"[{name}] SetRecentAttacker called. " +
                $"Attacker: {(attacker != null ? attacker.name : "NULL")}, " +
                $"current target: {(currentTarget != null ? currentTarget.name : "None")}"
            );

            recentAttacker = attacker;
            lastAttackedTime = Time.time;
            nextTargetSwitchTime = 0f;
        }

        public void SetTransform(Transform transform) => myTransform = transform;

        private Transform GetPlayer() => FindAnyObjectByType<Player>().transform;

        public void ReassignTarget()
        {
            if (!pendingTargetReassign) return;

            SetCurrentTarget(null);
            AssignBestTarget(EnemyManager.Instance.GetActiveTargets());

            pendingTargetReassign = false;
        }
    }
}
