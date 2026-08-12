using DG.Tweening;
using UnityEngine;

namespace Game.Defenses
{
    public class Cannon : Defense
    {
        [SerializeField] private Transform barrel; // The part that should turn to face the enemy

        [Header("Recoil")]
        [SerializeField] private float recoilDistance = 0.2f;
        [SerializeField] private float recoilDuration = 0.08f;
        [SerializeField] private float returnDuration = 0.15f;

        private Vector3 originalLocalPos;

        private void Start()
        {
            if (barrel != null)
                originalLocalPos = barrel.localPosition;
        }

        protected override void Update()
        {
            if (!GameManager.Instance.IsGameActive) return;
            base.Update();

            if (currentTarget != null && barrel != null)
                RotateTowardTarget();
        }

        private void RotateTowardTarget()
        {
            Vector3 direction = currentTarget.position - barrel.position;
            direction.y = 0; // Optional: keep rotation only on Y-axis

            if (direction.sqrMagnitude < 0.01f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            barrel.rotation = Quaternion.Slerp(barrel.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        protected override void Fire()
        {
            base.Fire();
            if (currentTarget == null) return;

            PlayRecoil();
        }

        private void PlayRecoil()
        {
            Vector3 recoilPos = originalLocalPos - barrel.forward * recoilDistance;

            barrel.DOKill();

            barrel.DOLocalMove(recoilPos, recoilDuration)
                  .SetEase(Ease.OutQuad)
                  .OnComplete(() =>
                  {
                      barrel.DOLocalMove(originalLocalPos, returnDuration)
                            .SetEase(Ease.OutBack);
                  });
        }
    }
}
