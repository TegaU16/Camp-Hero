using UnityEngine;

public class GemAltar : MonoBehaviour, IInteractable
{
    public string altarID;
    public Item requiredKey;
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public GameObject blessingAltarPrefab;

    private bool bossDefeated = false;
    private bool isActivated = false;

    private void Start()
    {
        LoadAltarState();
    }

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

    public void SaveAltarState()
    {
        GemAltarSaveData data = new()
        {
            bossDefeated = bossDefeated,
            isActivated = isActivated
        };

        SaveSystem.SaveGemAltarState(GameManager.Instance.currentWorldName, altarID, data);
    }

    private void LoadAltarState()
    {
        GemAltarSaveData data = SaveSystem.LoadGemAltarState(GameManager.Instance.currentWorldName, altarID);
        if (data != null)
        {
            bossDefeated = data.bossDefeated;
            isActivated = data.isActivated;
        }
    }
}
