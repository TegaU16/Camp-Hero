using System.Linq;
using Game.Inventory;
using Game.Terrain.Structures.Trials;
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
        private PlayerDeathUI deathUI;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            characterController = GetComponent<CharacterController>();
            ragdollController = GetComponent<SimpleRagdollController>();
            player = GetComponent<Player>();

            deathUI = FindFirstObjectByType<PlayerDeathUI>();
        }

        public void Die()
        {
            if (characterController != null)
                characterController.enabled = false;

            if (ragdollController != null)
                ragdollController.EnableRagdoll();
            else
                Debug.LogWarning("No SimpleRagdollController found!");

            InventoryManager.Instance.ResetExtensions();
            InventoryManager.Instance.DropAllItems();

            InteractableItemManager.Instance.ForceMergeAll();

            foreach (TrialAltar trialAltar in KeyStructureSpawner.Instance.activeTrialAltars.ToList())
            {
                if (trialAltar != null && trialAltar.IsWaveInProgress())
                    trialAltar.FailTrial();
            }

            if (deathUI != null)
                StartCoroutine(deathUI.Show(deathDuration));

            Invoke(nameof(Despawn), deathDuration);
        }

        private void Despawn()
        {
            ragdollController.DisableRagdoll();
            StartCoroutine(GameManager.Instance.RespawnPlayer(player));
        }
    }
}
