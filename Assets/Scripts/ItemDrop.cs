using UnityEngine;

[CreateAssetMenu(menuName = "Item/Item Drop")]
public class ItemDrop : ScriptableObject
{
    public GameObject itemPrefab;
    public Item item;

    public void SpawnObject(Vector3 position, int itemCount, Transform torsoBone)
    {
        if (itemPrefab != null)
        {
            GameObject spawnedObject;

            if (!torsoBone)
                spawnedObject = Instantiate(itemPrefab, position, itemPrefab.transform.rotation);
            else
                spawnedObject = Instantiate(itemPrefab, torsoBone.position, Quaternion.identity);

            if (spawnedObject.TryGetComponent(out InteractableItem interactable))
            {
                interactable.item = item;
                interactable.itemCount = itemCount;
            }
        }
    }
}

