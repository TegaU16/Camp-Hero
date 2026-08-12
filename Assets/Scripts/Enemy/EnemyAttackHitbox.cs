using System.Collections.Generic;
using UnityEngine;

namespace Game.AI.Enemies
{
    public class EnemyCombatAttackHitbox : MonoBehaviour
    {
        private EnemyCombat enemyCombat;
        private readonly HashSet<Targetable> alreadyHit = new();

        public LayerMask targetableLayer;
        [SerializeField] private AudioClip hitSound;

        void Awake()
        {
            enemyCombat = GetComponentInParent<EnemyCombat>();
        }

        private void Start()
        {
            if (enemyCombat != null && TryGetComponent(out BoxCollider box))
            {
                box.size = new Vector3(box.size.x, box.size.y, enemyCombat.meleeAttackRange);
                box.center = new Vector3(0, box.center.y, enemyCombat.meleeAttackRange / 2f);
            }
        }

        // Called by animation event
        private void PerformHit()
        {
            if (!GameManager.Instance.IsGameActive) return;
            if (enemyCombat == null) return;
            if (!TryGetComponent(out BoxCollider box)) return;

            alreadyHit.Clear();

            Vector3 boxCenter = transform.TransformPoint(box.center);
            Vector3 boxHalfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);

            Collider[] hits = Physics.OverlapBox(boxCenter, boxHalfExtents, transform.rotation, targetableLayer);

            foreach (Collider hit in hits)
                ProcessHit(hit);
        }

        private void ProcessHit(Collider other)
        {
            Targetable target = other.GetComponentInParent<Targetable>();
            if (target == null) return;

            if (alreadyHit.Contains(target)) return;

            alreadyHit.Add(target);
            enemyCombat.DealDamage();

            AudioManager.Instance.PlaySFX(hitSound, position: transform.position);
        }
    }
}
