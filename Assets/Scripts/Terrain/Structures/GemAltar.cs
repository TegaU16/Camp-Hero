using Game.Inventory;
using Game.Saving;
using UnityEngine;

namespace Game.Terrain.Structures
{
    public class GemAltar : MonoBehaviour, IInteractable, ISaveableObject
    {
        public Item requiredKey;
        public GameObject bossPrefab;
        public Transform bossSpawnPoint;
        public GameObject blessingAltarPrefab;

        private bool bossDefeated = false;
        private bool isActivated = false;

        public Sprite gemAltarIcon;
        public Sprite ObjectIcon => gemAltarIcon;

        public void Interact()
        {
            if (isActivated || bossDefeated) return;
            if (!InventoryManager.Instance.HasItem(requiredKey)) return;

            isActivated = true;
            InventoryManager.Instance.ConsumeItem(requiredKey);
            SpawnBoss();
        }

        private void SpawnBoss()
        {
            GameObject boss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);
            BossController bossScript = boss.GetComponent<BossController>();
            bossScript.OnBossDefeated += OnBossDefeated;

            BossHealthBarManager.Instance.SpawnBossHealthBar(boss);
        }

        private void OnBossDefeated()
        {
            bossDefeated = true;
            TransformIntoBlessingAltar();
        }

        private void TransformIntoBlessingAltar()
        {
            Instantiate(blessingAltarPrefab, transform.position, transform.rotation);
            Destroy(gameObject);
        }

        public string GetInteractText() => "Summon Gem Guardian";

        public Transform GetTransform() => transform;

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
}
