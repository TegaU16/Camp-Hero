using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Upgrades/Active/Dash")]
public class DashUpgrade : ActiveUpgradeEffect
{
    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float dashDuration = 0.2f;
    public AnimationCurve dashSpeedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public ParticleSystem dashEffect; // Optional VFX prefab

    public override void Activate(Player player)
    {
        // Don’t allow activation if on cooldown or not enough stamina
        if (isOnCooldown) return;
        if (player.CurrentStamina < staminaCost) return;

        player.StartCoroutine(DashRoutine(player));
        player.StartCoroutine(CooldownRoutine());
    }

    private IEnumerator DashRoutine(Player player)
    {
        // Deduct stamina
        player.UseStamina(staminaCost);

        // Optional VFX
        if (dashEffect != null)
            Instantiate(dashEffect, player.transform.position, player.transform.rotation);

        if (!player.TryGetComponent(out CharacterController cc))
            yield break;

        Vector3 dashDirection = player.transform.forward.normalized;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            float t = elapsed / dashDuration;
            float speedMultiplier = dashSpeedCurve.Evaluate(t);

            // Move the player
            cc.Move(dashForce * speedMultiplier * Time.deltaTime * dashDirection);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
