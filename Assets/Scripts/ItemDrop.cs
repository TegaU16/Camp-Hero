using UnityEngine;

[CreateAssetMenu(menuName = "Item/Item Drop")]
public class ItemDrop : ScriptableObject
{
    public GameObject itemPrefab;
    public Item item;

    public void SpawnObject(Vector3 position, int itemCount)
    {
        if (itemPrefab != null)
        {
            GameObject instance = Instantiate(itemPrefab, position, itemPrefab.transform.rotation);
            
            if (instance.TryGetComponent<InteractableItem>(out var interactable))
            {
                interactable.item = item;
                interactable.itemCount = itemCount;
            }
        }
    }
}

