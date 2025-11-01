using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/Active/Ground Slam")]
public class GroundSlamUpgrade : ActiveUpgradeEffect
{
    [Header("Slam Settings")]
    public int damage = 50;
    public float radius = 5f;
    public StunEffect stun;
    public string slamAnimationName = "GroundSlam";

    public LayerMask enemyLayer;

    private Player player;

    public override void Activate(Player playerScript)
    {
        if (isOnCooldown) return;
        if (player.CurrentStamina < staminaCost) return;

        player = playerScript;
        player.UseStamina(staminaCost);

        // Trigger the slam animation
        Animator animator = player.GetComponentInChildren<Animator>();
        if (animator != null)
            animator.Play(slamAnimationName);

        player.StartCoroutine(CooldownRoutine()); // begins the cooldown timer
    }

    // Called by animation event at the right impact frame
    public void OnSlamImpact()
    {
        Vector3 origin = player.transform.position;

        Collider[] hits = new Collider[20];
        int hitCount = Physics.OverlapSphereNonAlloc(origin, radius, hits, enemyLayer);

        if (hitCount == hits.Length)
        {
            Collider[] expandedArray = new Collider[hitCount * 2];
            hitCount = Physics.OverlapSphereNonAlloc(origin, radius, expandedArray, enemyLayer);
            hits = expandedArray;
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hits[i];

            if (hit.TryGetComponent(out Enemy enemy) && hit.TryGetComponent(out BreakableObject breakable))
            {
                breakable.TakeDamage(damage, player.transform);

                if (stun != null)
                    stun.Apply(enemy);
            }
        }

        // Optionally add visual or sound effects here
        // e.g. Instantiate(slamEffectPrefab, origin, Quaternion.identity);
    }
}
