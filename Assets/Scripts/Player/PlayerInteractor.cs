using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactableMask;
    public GameObject worldUIIndicatorPrefab;
    private GameObject currentUIInstance;
    private WorldInteractUI currentUI;

    private IInteractable currentInteractable;

    void Update()
    {
        if (GameManager.Instance.isPaused) return;

        if (!InventoryManager.Instance.IsExtensionOpen())
        {
            Collider[] hits = new Collider[20];
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactionRange, hits, interactableMask);
            float closest = float.MaxValue;
            IInteractable nearest = null;

            if (hitCount == hits.Length)
            {
                // Resize the array if it's full
                Collider[] expandedArray = new Collider[hitCount * 2];  // Double the size
                hitCount = Physics.OverlapSphereNonAlloc(transform.position, interactionRange, expandedArray, interactableMask);
                hits = expandedArray;  // Assign the expanded array
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hits[i];
                IInteractable interactable = hit.gameObject.GetComponentInParent<IInteractable>();

                if (interactable != null)
                {
                    float dist = Vector3.Distance(transform.position, interactable.GetTransform().position);

                    if (dist < closest)
                    {
                        closest = dist;
                        nearest = interactable;
                    }
                }
            }

            if (nearest != null)
            {
                if (currentInteractable != nearest)
                {
                    currentInteractable = nearest;

                    if (currentUIInstance == null)
                    {
                        currentUIInstance = Instantiate(worldUIIndicatorPrefab);
                        currentUI = currentUIInstance.GetComponent<WorldInteractUI>();
                    }

                    currentUIInstance.SetActive(true);
                    currentUI.Setup(currentInteractable.GetInteractText(), currentInteractable.GetTransform());
                }

                if (Input.GetKeyDown(interactKey))
                {
                    currentUIInstance.SetActive(false);
                    currentInteractable.Interact();
                    currentInteractable = null;
                }
            }
            else
            {
                currentInteractable = null;

                if (currentUIInstance != null)
                {
                    currentUIInstance.SetActive(false);
                }
            }
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Pickable"))
        {
            InteractableItem interactable = hit.gameObject.GetComponentInParent<InteractableItem>();
            interactable.Interact();
        }
    }
}
