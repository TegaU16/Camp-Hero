using UnityEngine;

public class GemAltar : MonoBehaviour, IInteractable, ISaveableObject
{
    public string altarID;
    public Item requiredKey;
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public GameObject blessingAltarPrefab;

    private bool bossDefeated = false;
    private bool isActivated = false;

    public void Interact()
    {
        if (isActivated || bossDefeated)
            return;

        if (!InventoryManager.Instance.HasItem(requiredKey))
        {
            return;
        }

        isActivated = true;
        InventoryManager.Instance.ConsumeItem(requiredKey);
        SpawnBoss();
    }

    void SpawnBoss()
    {
        GameObject boss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);
        BossController bossScript = boss.GetComponent<BossController>();
        bossScript.OnBossDefeated += OnBossDefeated;
    }

    void OnBossDefeated()
    {
        bossDefeated = true;
        TransformIntoBlessingAltar();
    }

    void TransformIntoBlessingAltar()
    {
        Instantiate(blessingAltarPrefab, transform.position, transform.rotation);
        Destroy(gameObject); // Remove this altar
    }

    public string GetInteractText()
    {
        return "Summon Gem Guardian";
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public string SaveState()
    {
        GemAltarSaveData data = new()
        {
            bossDefeated = bossDefeated,
            isActivated = isActivated
        };

        return JsonUtility.ToJson(data);
    }

    public void LoadState(string json)
    {
        GemAltarSaveData data = JsonUtility.FromJson<GemAltarSaveData>(json);
        if (data != null)
        {
            bossDefeated = data.bossDefeated;
            isActivated = data.isActivated;
        }
        else
        {
            bossDefeated = false;
            isActivated = false;
        }
    }
}
