using UnityEngine;
using System.Collections;
using Game.Players;

namespace Game.Upgrades
{
    [CreateAssetMenu(menuName = "Upgrades/Active/Dash")]
    public class DashUpgrade : ActiveUpgradeEffect
    {
        [Header("Dash Settings")]
        [SerializeField] private float dashForce = 20f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private AnimationCurve dashSpeedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private string dashAnimationTrigger = "Dash";

        [Header("Trail Settings")]
        [SerializeField] private float meshRefreshRate = 0.1f;
        [SerializeField] private float meshDestroyDelay = 2f;
        [SerializeField] private Material trailMaterial;
        [SerializeField] private string shaderVariableReference;
        [SerializeField] private float shaderVariableRate = 0.1f;
        [SerializeField] private float shaderVariableRefreshRate = 0.05f;

        private MeshRenderer[] meshRenderers;
        private bool isTrailActive;

        public override void Activate(Player player)
        {
            // Don’t allow activation if on cooldown or not enough stamina
            if (isOnCooldown) return;
            if (player.CurrentStamina < staminaCost) return;

            player.StartCoroutine(DashRoutine(player));

            if (!isTrailActive)
            {
                isTrailActive = true;
                player.StartCoroutine(ActivateTrail(player));
            }

            if (player.TryGetComponent(out Health health))
                health.isImmune = true;

            if (player.TryGetComponent(out PlayerCombat playerCombat))
                playerCombat.canAttack = false;

            // Trigger the slam animation
            if (player.TryGetComponent(out Animator animator))
                animator.SetTrigger(dashAnimationTrigger);

            player.StartCoroutine(CooldownRoutine());
        }

        private IEnumerator DashRoutine(Player player)
        {
            // Deduct stamina
            player.UseStamina(staminaCost);

            if (!player.TryGetComponent(out CharacterController cc)) yield break;

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

        private IEnumerator ActivateTrail(Player player)
        {
            float timeActive = 0;

            while (timeActive < dashDuration)
            {
                timeActive += meshRefreshRate;
                meshRenderers ??= player.GetComponentsInChildren<MeshRenderer>();

                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    MeshFilter sourceFilter = meshRenderer.GetComponent<MeshFilter>();
                    if (sourceFilter == null || sourceFilter.sharedMesh == null) continue;

                    GameObject snapshot = new();

                    snapshot.transform.SetPositionAndRotation(meshRenderer.transform.position, meshRenderer.transform.rotation);
                    snapshot.transform.localScale = meshRenderer.transform.lossyScale;

                    MeshFilter filter = snapshot.AddComponent<MeshFilter>();
                    MeshRenderer renderer = snapshot.AddComponent<MeshRenderer>();

                    filter.mesh = Instantiate(sourceFilter.sharedMesh);

                    renderer.sharedMaterial = trailMaterial;

                    player.StartCoroutine(AnimateMaterialFloat(
                        material: renderer.sharedMaterial,
                        goal: 0f,
                        rate: shaderVariableRate,
                        refreshRate: shaderVariableRefreshRate
                    ));

                    Destroy(snapshot, meshDestroyDelay);
                }

                yield return new WaitForSeconds(meshRefreshRate);
            }

            isTrailActive = false;
        }

        private IEnumerator AnimateMaterialFloat(Material material, float goal, float rate, float refreshRate)
        {
            float valueToAnimate = material.GetFloat(shaderVariableReference);

            while (valueToAnimate > goal)
            {
                valueToAnimate -= rate;
                material.SetFloat(shaderVariableReference, valueToAnimate);
                yield return new WaitForSeconds(refreshRate);
            }
        }
    }
}
