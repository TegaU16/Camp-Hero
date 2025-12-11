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

        public IInteractable CurrentInteractable => currentInteractable;

        void Update()
        {
            if (!GameManager.Instance.IsGameManagerReady()) return;
            if (InventoryManager.Instance.IsExtensionOpen()) return;

            IInteractable nearest = FindNearestInteractable(out float dist);
            bool validNearest = nearest != null && dist <= interactionRange;

            if (currentInteractable != null && (!validNearest || !IsAlive(currentInteractable)))
                ClearInteractable();

            if (validNearest && IsAlive(nearest))
            {
                Object unityObj = nearest as Object;
                if (unityObj == null || !nearest.GetTransform().gameObject.activeInHierarchy)
                {
                    ClearInteractable();
                    return;
                }

                if (currentInteractable != nearest)
                {
                    currentInteractable = nearest;

                    if (currentUIInstance == null)
                    {
                        currentUIInstance = Instantiate(worldUIIndicatorPrefab);
                        currentUI = currentUIInstance.GetComponent<WorldInteractUI>();
                    }

                    currentUIInstance.SetActive(true);
                    currentUI.Setup(currentInteractable);
                }

                if (Input.GetKeyDown(interactKey))
                {
                    currentInteractable?.Interact();
                    ClearInteractable();
                }
            }
            else
            {
                ClearInteractable();
            }
        }

        private IInteractable FindNearestInteractable(out float closest)
        {
            Collider[] hits = new Collider[20];
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactionRange, hits, interactableMask);

            if (hitCount == hits.Length)
            {
                Collider[] expanded = new Collider[hitCount * 2];
                hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactionRange, expanded, interactableMask);
                hits = expanded;
            }

            closest = float.MaxValue;
            IInteractable nearest = null;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hits[i];
                if (hit == null) continue;

                IInteractable interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null) continue;
                if (!IsAlive(interactable)) continue;

                float dist = Vector3.Distance(transform.position, interactable.GetTransform().position);
                if (dist < closest)
                {
                    closest = dist;
                    nearest = interactable;
                }
            }

            return nearest;
        }

        private bool IsAlive(IInteractable interactable)
        {
            return !(interactable is Object unityObj && unityObj == null);
        }

        public void ClearInteractable()
        {
            currentInteractable = null;
            if (currentUIInstance != null)
                currentUIInstance.SetActive(false);
        }

        public void RefreshInteractable()
        {
            ClearInteractable();
            IInteractable nearest = FindNearestInteractable(out _);

            if (currentInteractable != nearest)
            {
                currentInteractable = nearest;
                currentUI = currentUIInstance.GetComponent<WorldInteractUI>();

                currentUIInstance.SetActive(true);
                currentUI.Setup(currentInteractable);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.gameObject.CompareTag("Pickable"))
            {
                InteractableItem interactable = hit.gameObject.GetComponentInParent<InteractableItem>();
                if (interactable != null) interactable.Interact();

                if (currentInteractable == interactable.GetComponent<IInteractable>())
                    ClearInteractable();
            }
        }
    }
}
