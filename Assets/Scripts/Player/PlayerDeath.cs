using System.Collections;
using Game.Inventory;
using Game.Terrain.Structures.Trials;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.Players
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(SimpleRagdollController))]
    [RequireComponent(typeof(Player))]
    public class PlayerDeath : MonoBehaviour
    {
        private CharacterController characterController;
        private SimpleRagdollController ragdollController;
        private Player player;

        public float deathDuration = 5f;
        public float cameraMoveDuration = 2f;
        [SerializeField] private Transform deathCameraPoint;
        private bool isDead;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            characterController = GetComponent<CharacterController>();
            ragdollController = GetComponent<SimpleRagdollController>();
            player = GetComponent<Player>();
        }

        public void Die()
        {
            if (isDead) return;
            isDead = true;

            if (characterController != null)
                characterController.enabled = false;

            if (ragdollController != null)
                ragdollController.EnableRagdoll();
            else
                Debug.LogWarning("No SimpleRagdollController found!");

            InventoryManager.Instance.ResetExtensions();
            InventoryManager.Instance.DropAllItems();

            foreach (TrialAltar trialAltar in KeyStructureSpawner.Instance.activeTrialAltars)
            {
                if (trialAltar != null && trialAltar.IsWaveInProgress())
                    trialAltar.FailTrial();
            }

            StartCoroutine(MoveCamera());

            StartCoroutine(PlayerDeathUI.Instance.Show(deathDuration));

            Invoke(nameof(Despawn), deathDuration);
        }

        private IEnumerator MoveCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) yield break;

            if (mainCamera.TryGetComponent(out CinemachineBrain brain))
                brain.enabled = false;

            deathCameraPoint.GetPositionAndRotation(out Vector3 newPosition, out Quaternion newRotation);
            mainCamera.transform.GetPositionAndRotation(out Vector3 startPos, out Quaternion startRot);

            float elapsed = 0f;
            while (elapsed < cameraMoveDuration)
            {
                elapsed += Time.deltaTime;
                float tMove = Mathf.Clamp01(elapsed / cameraMoveDuration);

                tMove = Mathf.SmoothStep(0, 1, tMove);

                mainCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPos, newPosition, tMove),
                    Quaternion.Slerp(startRot, newRotation, tMove)
                );

                yield return null;
            }

            mainCamera.transform.SetPositionAndRotation(newPosition, newRotation);
        }

        private void Despawn()
        {
            ragdollController.DisableRagdoll();
            StartCoroutine(GameManager.Instance.RespawnPlayer(player));
        }
    }
}
