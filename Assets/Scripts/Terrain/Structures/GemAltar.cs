using Game.Inventory;
using Game.Saving;
using UnityEngine;

namespace Game.Terrain.Structures
{
    public class GemAltar : MonoBehaviour, IInteractable, ISaveableObject
    {
        [SerializeField] private Item requiredKey;
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private GameObject blessingAltarPrefab;

        [Header("Boss Spawn")]
        [SerializeField] private float spawnRadius = 3f;
        [SerializeField] private int maxSpawnAttempts = 10;

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
            Vector3 spawnPosition = GetValidSpawnPosition();

            GameObject boss = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);

            BossController bossScript = boss.GetComponent<BossController>();
            bossScript.OnBossDefeated += OnBossDefeated;

            BossHealthBarManager.Instance.SpawnBossHealthBar(boss);
        }

        private Vector3 GetValidSpawnPosition()
        {
            Vector3 center = transform.position;

            for (int i = 0; i < maxSpawnAttempts; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;

                Vector3 candidatePosition = center + new Vector3(randomCircle.x, 0f, randomCircle.y);
                Vector3Int posInt = Utility.WorldToVoxelCoord(candidatePosition);

                candidatePosition.y = Utility.GetHeightAt(posInt.x, posInt.z);

                bool occupied = TerrainGenerator.Instance.IsOccupied(posInt);

                if (!occupied) return candidatePosition;
            }

            // Fallback if no valid position found
            return center;
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
