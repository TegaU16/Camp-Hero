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
        if (InventoryManager.Instance.IsExtensionOpen()) return;

        if (currentInteractable is Object unityObj && unityObj == null)
        {
            ClearInteractable();
            return;
        }

        IInteractable nearest = FindNearestInteractable(out float dist);

        bool validNearest = nearest != null && dist <= interactionRange;

        if (validNearest)
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

            if (interactable is Object unityObj && unityObj == null) continue;

            float dist = Vector3.Distance(transform.position, interactable.GetTransform().position);
            if (dist < closest)
            {
                closest = dist;
                nearest = interactable;
            }
        }

        return nearest;
    }

    private void ClearInteractable()
    {
        currentInteractable = null;
        if (currentUIInstance != null)
            currentUIInstance.SetActive(false);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Pickable"))
        {
            InteractableItem interactable = hit.gameObject.GetComponentInParent<InteractableItem>();
            interactable.Interact();

            if (currentInteractable == interactable.GetComponent<IInteractable>())
                ClearInteractable();
        }
    }
}
