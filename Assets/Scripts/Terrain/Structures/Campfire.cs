using System.Collections;
using System.Collections.Generic;
using Game.AI.Enemies;
using Game.Inventory;
using Game.Players;
using Game.Saving;
using Game.Upgrades;
using UnityEngine;

namespace Game.Terrain.Structures
{
    public enum GemColor
    {
        Blue,
        Green,
        Red,
        Yellow
    }

    [RequireComponent(typeof(SphereCollider))]
    public class Campfire : MonoBehaviour, IInteractable, ISaveableObject
    {
        public Health health;
        public AudioClip fireSound;

        [Header("Gem References")]
        public GameObject blueGem;
        public GameObject greenGem;
        public GameObject redGem;
        public GameObject yellowGem;

        private readonly List<CampfireUpgradeEffect> activeUpgrades = new();
        private readonly Dictionary<(CampfireUpgradeEffect, MonoBehaviour), Coroutine> activeCoroutines = new();

        private SphereCollider auraCollider;
        private readonly List<MonoBehaviour> charactersInRange = new();

        private readonly List<GemColor> unlockedGems = new();
        private Dictionary<GemColor, GameObject> gemObjects;

        private void Awake()
        {
            gemObjects = new Dictionary<GemColor, GameObject>
        {
            { GemColor.Blue, blueGem },
            { GemColor.Green, greenGem },
            { GemColor.Red, redGem },
            { GemColor.Yellow, yellowGem }
        };

            auraCollider = GetComponent<SphereCollider>();
            if (auraCollider == null)
                auraCollider = gameObject.AddComponent<SphereCollider>();
            auraCollider.isTrigger = true;

            foreach (CampfireUpgradeEffect upg in activeUpgrades)
                auraCollider.radius = upg.effectRadius > 0f ? upg.effectRadius : 0f;
        }

        private void Start()
        {
            // find any players already inside the collider (use Physics.OverlapSphere)
            Collider[] cols = new Collider[20];
            int colliderCount = Physics.OverlapSphereNonAlloc(transform.position, auraCollider.radius, cols);

            for (int i = 0; i < colliderCount; i++)
            {
                Collider col = cols[i];
                if (!col.TryGetComponent(out CharacterController _)) continue;

                if (col.TryGetComponent(out MonoBehaviour script))
                {
                    if (script is not Player && script is not Enemy) continue;

                    if (!charactersInRange.Contains(script))
                        charactersInRange.Add(script);

                    foreach (CampfireUpgradeEffect upgrade in activeUpgrades)
                    {
                        if (Vector3.Distance(transform.position, script.transform.position) <= upgrade.effectRadius)
                        {
                            if (script is Player player)
                                upgrade.OnPlayerEnterRange(this, player);
                            else if (script is Enemy enemy)
                                upgrade.OnEnemyEnterRange(this, enemy);
                        }

                    }
                }
            }

            AudioManager.Instance.PlaySFX(fireSound);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out MonoBehaviour script)) return;
            if (script is not Player && script is not Enemy) return;

            // track players so exit is reliable
            if (!charactersInRange.Contains(script))
                charactersInRange.Add(script);

            // For each upgrade, check per-upgrade radius and call Enter if inside
            foreach (CampfireUpgradeEffect upgrade in activeUpgrades)
            {
                if (Vector3.Distance(transform.position, script.transform.position) <= upgrade.effectRadius)
                {
                    if (script is Player player)
                        upgrade.OnPlayerEnterRange(this, player);
                    else if (script is Enemy enemy)
                        upgrade.OnEnemyEnterRange(this, enemy);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out MonoBehaviour script)) return;
            if (script is not Player && script is not Enemy) return;

            charactersInRange.Remove(script);

            // call exit for all upgrades that previously applied (we don't track per-upgrade state here,
            // so just call OnPlayerExitRange if player is outside that upgrade's aura)
            foreach (CampfireUpgradeEffect upgrade in activeUpgrades)
            {
                if (Vector3.Distance(transform.position, script.transform.position) <= upgrade.effectRadius)
                {
                    if (script is Player player)
                        upgrade.OnPlayerExitRange(this, player);
                    else if (script is Enemy enemy)
                        upgrade.OnEnemyExitRange(this, enemy);
                }
            }
        }

        public void StartUpgradeCoroutine(CampfireUpgradeEffect upgrade, MonoBehaviour script, IEnumerator routine)
        {
            (CampfireUpgradeEffect, MonoBehaviour) key = (upgrade, script);
            if (activeCoroutines.ContainsKey(key)) return;

            Coroutine coroutine = StartCoroutine(routine);
            activeCoroutines[key] = coroutine;
        }

        public void StopUpgradeCoroutine(CampfireUpgradeEffect upgrade, MonoBehaviour script)
        {
            (CampfireUpgradeEffect, MonoBehaviour) key = (upgrade, script);
            if (!activeCoroutines.TryGetValue(key, out Coroutine coroutine)) return;

            StopCoroutine(coroutine);
            activeCoroutines.Remove(key);
        }

        public void UnlockGem(GemColor color)
        {
            if (!unlockedGems.Contains(color))
            {
                unlockedGems.Add(color);
                UpdateGemVisibility();
            }
        }

        public void RemoveGem(GemColor color)
        {
            if (unlockedGems.Remove(color))
                UpdateGemVisibility();
        }

        private void UpdateGemVisibility()
        {
            foreach (KeyValuePair<GemColor, GameObject> pair in gemObjects)
                pair.Value.SetActive(unlockedGems.Contains(pair.Key));
        }

        public void Interact()
        {
            InventoryManager.Instance.campfireMenuUI.SetActive(true);
            CameraControlToggle.Instance.SetCameraControl(false);
        }

        public string GetInteractText() => "Open Campfire Menu";

        public Transform GetTransform() => transform;

        public void Die()
        {
            GameManager.Instance.GameOver(win: false);
        }

        public string SaveState()
        {
            CampfireSaveData data = new()
            {
                currentHealth = health.GetHealth(),
                unlockedGems = new List<GemColor>(unlockedGems)
            };

            return JsonUtility.ToJson(data);
        }

        public void LoadState(string json)
        {
            CampfireSaveData data = JsonUtility.FromJson<CampfireSaveData>(json);
            GameManager.Instance.SetCampfire(gameObject, data);

            if (data == null)
            {
                health.SetHealth(health.maxHealth);
                UpdateGemVisibility();
            }
            else
            {
                health.SetHealth(data.currentHealth);

                unlockedGems.Clear();
                foreach (GemColor color in data.unlockedGems)
                    unlockedGems.Add(color);

                UpdateGemVisibility();
            }
        }
    }
}
