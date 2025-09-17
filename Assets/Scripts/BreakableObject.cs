using System.Collections;
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
    private int maxHealth;
    public int baseHealth;
    private int health;
    public int expDropped;
    public Drop[] drops;
    public ObjectType objectType;
    public EntityType entityType;
    public int objectLevel;

    private bool isDestroyed = false;

    [HideInInspector] public VoxelChunk owningChunk;
    [HideInInspector] public int savedObjectIndex = -1;

    [SerializeField] private Transform torsoBone;

    private void OnEnable()
    {
        DifficultyManager.Instance.OnDifficultyChanged += HandleDifficultyChange;
        ResetObject();
    }

    private void OnDisable()
    {
        DifficultyManager.Instance.OnDifficultyChanged -= HandleDifficultyChange;
    }

    private void HandleDifficultyChange(Difficulty newDifficulty)
    {
        ApplyDifficultyScaling();
    }

    private void ApplyDifficultyScaling(bool reset = false)
    {
        float healthMultiplier = DifficultyManager.Instance.GetHealthMultiplier(entityType);

        float healthPercent = maxHealth > 0 ? (float)health / maxHealth : 1f;

        maxHealth = (int)(baseHealth * healthMultiplier);
        health = reset ? maxHealth : (int)(maxHealth * healthPercent);
    }

    public void TakeDamage(int damage, bool crit, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDestroyed) return;

        if (health > 0) ShowDamagePopup(damage, crit, hitPoint);

        health -= damage;
        health = Mathf.Max(health, 0);

        if (health == 0)
        {
            if (TryGetComponent(out Enemy enemy))
            {
                enemy.Die();
                return;
            }

            if (TryGetComponent(out Animal animal))
            {
                animal.Die();
                return;
            }

            DestroyObject();
        }
        else
        {
            HitEffectManager hitEffectManager = FindFirstObjectByType<HitEffectManager>();
            if (hitEffectManager != null)
            {
                hitEffectManager.SpawnSparks(hitPoint, hitNormal);
            }

            if (entityType == EntityType.Static)
                StartCoroutine(BounceObject(transform));
        }
    }

    private void ShowDamagePopup(int damage, bool crit, Vector3 hitPoint)
    {
        GameObject popupInstance = DamagePopupPool.Instance.GetPopup();

        Vector3 spawnPosition = hitPoint + Vector3.up * Random.Range(0.2f, 0.5f);
        popupInstance.transform.position = spawnPosition;

        // Always face camera
        popupInstance.transform.LookAt(Camera.main.transform);
        popupInstance.transform.Rotate(0, 180, 0);

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

                dropAmount = Mathf.Clamp(dropAmount, itemDrop.minValue, itemDrop.maxValue);

                itemDrop.drop.SpawnObject(pos, dropAmount, torsoBone);
            }
        }

        LevelManager.Instance.AddExp(expDropped);
        
        if (entityType == EntityType.Static)
        {
            if (owningChunk != null && savedObjectIndex >= 0 && savedObjectIndex < owningChunk.savedObjects.Count)
            {
                owningChunk.savedObjects.RemoveAt(savedObjectIndex);
                owningChunk.isDirty = true;
            }

            GameManager.Instance.voxelGrid.MarkAreaOccupied(gameObject, false);
            Destroy(gameObject);
        }
    }

    IEnumerator BounceObject(Transform objTransform)
    {
        Vector3 originalScale = objTransform.localScale;
        Vector3 shrunkenScale = originalScale * 0.95f; // 95% size

        float duration = 0.1f; // shrink duration
        float elapsed = 0f;

        // Shrink
        while (elapsed < duration)
        {
            objTransform.localScale = Vector3.Lerp(originalScale, shrunkenScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        objTransform.localScale = shrunkenScale;

        // Expand back
        elapsed = 0f;
        while (elapsed < duration)
        {
            objTransform.localScale = Vector3.Lerp(shrunkenScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        objTransform.localScale = originalScale;
    }

    public void ResetObject()
    {
        isDestroyed = false;
        ApplyDifficultyScaling(true);
    }

    public int GetHealth() => health;

    public void SetHealth(int currentHealth) => health = currentHealth;
}
