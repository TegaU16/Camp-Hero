using System.Collections;
using UnityEngine;

namespace Game.Storage
{
    public class Chest : StorageUnit
    {
        [SerializeField] private Transform lidPivot;
        [SerializeField] private float openAngle = -110f;
        [SerializeField] private float speed = 4f;

        private bool isOpen;
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Coroutine currentAnimation;

        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;

        void Start()
        {
            closedRotation = lidPivot.localRotation;
            openRotation = Quaternion.Euler(openAngle, 0f, 0f);
        }

        public void ToggleChest()
        {
            isOpen = !isOpen;

            if (isOpen)
                AudioManager.Instance.PlaySFX(openSound);
            else
                AudioManager.Instance.PlaySFX(closeSound);

            if (currentAnimation != null)
                StopCoroutine(currentAnimation);

            currentAnimation = StartCoroutine(RotateLid(isOpen ? openRotation : closedRotation));
        }

        private IEnumerator RotateLid(Quaternion targetRotation)
        {
            while (Quaternion.Angle(lidPivot.localRotation, targetRotation) > 0.1f)
            {
                lidPivot.localRotation = Quaternion.Slerp(
                    lidPivot.localRotation,
                    targetRotation,
                    Time.deltaTime * speed
                );

                yield return null;
            }

            lidPivot.localRotation = targetRotation;
            currentAnimation = null;
        }
    }
}