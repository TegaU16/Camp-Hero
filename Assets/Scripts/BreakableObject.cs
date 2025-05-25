using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [System.Serializable]
    public struct Drop
    {
        public ItemDrop drop;
        public int minValue;
        public int maxValue;
        [Range(0f, 1f)] public float dropChance;
    }

    public enum ObjectType
    {
        Wood,
        Stone,
        Flesh,
        None
    }

    public GameObject damagePopupPrefab;
    private float maxHealth;
    public float health;
    public int expDropped;
    public Drop[] drops;
    public ObjectType type;
    public int objectLevel;
    public bool isOrganism = false;

    private bool isDestroyed = false;

    [HideInInspector] public VoxelChunk owningChunk;
    [HideInInspector] public int savedObjectIndex = -1;

    private void Start()
    {
        maxHealth = health;
    }

    public void TakeDamage(int damage, bool crit)
    {
        if (isDestroyed) return;

        ShowDamagePopup(damage, crit);

        if (health <= damage)
        {
            // Tell the Enemy (if there is one) to die first
            if (TryGetComponent(out Enemy enemy))
            {
                enemy.Die();
                isDestroyed = true;
                return; // Enemy will handle deactivation
            }

            DestroyObject();
        }
        else
        {
            health -= damage;
            HitEffectManager hitEffectManager = FindFirstObjectByType<HitEffectManager>();
            if (hitEffectManager != null)
            {
                hitEffectManager.ApplyHitEffect(gameObject);
            }
        }
    }

    private void ShowDamagePopup(int damage, bool crit)
    {
        GameObject popupInstance = DamagePopupPool.Instance.GetPopup();

        Vector3 spawnPosition = transform.position + new Vector3(0, 2f, 0);
        popupInstance.transform.position = spawnPosition;
        popupInstance.transform.LookAt(Camera.main.transform);
        popupInstance.transform.Rotate(0, 180, 0); // Flip if needed

        if (popupInstance.TryGetComponent(out DamagePopup popup))
        {
            popup.Setup(damage, crit);
        }
    }

    public void DestroyObject()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        foreach (Drop itemDrop in drops)
        {
            if (Random.value <= itemDrop.dropChance)
            {
                Vector3 pos = gameObject.transform.position + new Vector3(0f, 2f, 0f);
                int dropAmount = Random.Range(itemDrop.minValue, itemDrop.maxValue + 1);

                // Clamp the value to ensure it's within the range even if something unexpected happens
                dropAmount = Mathf.Clamp(dropAmount, itemDrop.minValue, itemDrop.maxValue);

                itemDrop.drop.SpawnObject(pos, dropAmount);
            }
        }

        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.AddExp(expDropped);
        }

        if (!isOrganism)
        {
            if (owningChunk != null && savedObjectIndex >= 0 && savedObjectIndex < owningChunk.savedObjects.Count)
            {
                var saved = owningChunk.savedObjects[savedObjectIndex];
                if (saved.position == transform.position)
                {
                    owningChunk.savedObjects.RemoveAt(savedObjectIndex);
                }
            }

            GameManager.Instance.voxelGrid.MarkAreaOccupied(gameObject);
            Destroy(gameObject);
        }
    }

    public void ResetObject()
    {
        isDestroyed = false;
        health = maxHealth;
    }
}
