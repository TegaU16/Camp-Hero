using UnityEngine;

namespace Game.Players
{
    public class PlayerAttributes : MonoBehaviour
    {
        public int MaxHealth => 100 + PlayerStatsManager.Instance.stats.vitality.Value * 10;
        public int MaxStamina => 100 + PlayerStatsManager.Instance.stats.stamina.Value * 5;
        public float MeleeDamageMultiplier => 1f + PlayerStatsManager.Instance.stats.strength.Value * 0.075f;
        public float StaminaRegenRate => 1f + PlayerStatsManager.Instance.stats.endurance.Value * 0.05f; // 5% per point
        public float CritChanceMultiplier => 1f + PlayerStatsManager.Instance.stats.luck.Value * 0.005f; // 0.5% per point
        public float MovementSpeedMultiplier => 1f + PlayerStatsManager.Instance.stats.speed.Value * 0.05f;
    }
}
