using Game.Inventory;
using UnityEngine;

namespace Game.Players
{
    public class PlayerInteractor : MonoBehaviour
    {
        public float interactionRange = 3f;
        public KeyCode interactKey = KeyCode.E;
        public LayerMask interactableMask;
        public GameObject worldUIIndicatorPrefab;

        private GameObject currentUIInstance;
        private WorldInteractUI currentUI;
        private IInteractable currentInteractable;

        private float searchTimer;
        private const float SearchInterval = 0.1f;

        public IInteractable CurrentInteractable => currentInteractable;

        private readonly Collider[] hits = new Collider[32];

        private void Start()
        {
            if (currentUIInstance != null) return;

            currentUIInstance = Instantiate(worldUIIndicatorPrefab);
            currentUI = currentUIInstance.GetComponent<WorldInteractUI>();

            currentUIInstance.SetActive(false);
        }

        void Update()
        {
            if (!GameManager.Instance.IsGameActive) return;
            if (InventoryManager.Instance.IsExtensionOpen()) return;

            searchTimer += Time.deltaTime;
            if (searchTimer >= SearchInterval)
            {
                searchTimer = 0f;

                IInteractable nearest = FindNearestInteractable(out float dist);
                bool valid = nearest != null && dist <= interactionRange && IsAlive(nearest);

                if (!valid)
                {
                    ClearInteractable();
                    return;
                }

                if (currentInteractable != nearest)
                {
                    currentInteractable = nearest;
                    currentUIInstance.SetActive(true);
                    currentUI.Setup(currentInteractable);
                }
            }

            if (Input.GetKeyDown(interactKey) && currentInteractable != null)
            {
                currentInteractable.Interact();
                ClearInteractable();
            }
        }

        private IInteractable FindNearestInteractable(out float closest)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactionRange, hits, interactableMask);

            closest = float.MaxValue;
            IInteractable nearest = null;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hits[i];
                if (hit == null) continue;

                IInteractable interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null || !IsAlive(interactable)) continue;

                InteractableItem item = hit.GetComponentInParent<InteractableItem>();
                if (item != null)
                    item.Wake();

                float dist = Vector3.Distance(transform.position, interactable.GetTransform().position);
                if (dist >= closest) continue;

                closest = dist;
                nearest = interactable;
            }

            return nearest;
        }

        private bool IsAlive(IInteractable interactable) => interactable is not Object unityObj || unityObj != null;

        public void ClearInteractable()
        {
            if (currentInteractable == null) return;

            currentInteractable = null;
            if (currentUIInstance == null) return;

            currentUIInstance.SetActive(false);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!hit.gameObject.CompareTag("Pickable")) return;

            InteractableItem interactable = hit.gameObject.GetComponentInParent<InteractableItem>();
            if (interactable != null)
                interactable.Interact();

            if (currentInteractable == interactable.GetComponent<IInteractable>())
                ClearInteractable();
        }
    }
}
