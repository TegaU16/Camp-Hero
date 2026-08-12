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
        private static readonly WaitForSeconds _waitForSeconds1 = new(1f);
        public Health health;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private int dayRegenAmount = 1500;

        /*[Header("Gem References")]
        [SerializeField] private GameObject blueGem;
        [SerializeField] private GameObject greenGem;
        [SerializeField] private GameObject redGem;
        [SerializeField] private GameObject yellowGem;*/

        private readonly List<CampfireUpgradeEffect> activeUpgrades = new();
        private readonly Dictionary<(CampfireUpgradeEffect, MonoBehaviour), Coroutine> activeCoroutines = new();

        private SphereCollider auraCollider;
        private readonly List<MonoBehaviour> charactersInRange = new();

        //private readonly List<GemColor> unlockedGems = new();
        //private Dictionary<GemColor, GameObject> gemObjects;

        [SerializeField] private Sprite campfireIcon;
        public Sprite ObjectIcon => campfireIcon;

        private float dayDuration;
        private float RegenRate => dayRegenAmount / dayDuration;

        private void Awake()
        {
            /*gemObjects = new Dictionary<GemColor, GameObject>
            {
                { GemColor.Blue, blueGem },
                { GemColor.Green, greenGem },
                { GemColor.Red, redGem },
                { GemColor.Yellow, yellowGem }
            };*/

            auraCollider = GetComponent<SphereCollider>();
            if (auraCollider == null)
                auraCollider = gameObject.AddComponent<SphereCollider>();

            auraCollider.isTrigger = true;

            foreach (CampfireUpgradeEffect upg in activeUpgrades)
                auraCollider.radius = upg.effectRadius > 0f ? upg.effectRadius : 0f;
        }

        private void Start()
        {
            dayDuration = DayNightCycle.Instance.dayDurationInSeconds;
            DayNightCycle.Instance.OnDayAdvanced += StartRegen;

            Collider[] cols = new Collider[20];
            int colliderCount = Physics.OverlapSphereNonAlloc(transform.position, auraCollider.radius, cols);

            for (int i = 0; i < colliderCount; i++)
            {
                Collider col = cols[i];
                if (!col.TryGetComponent(out CharacterController _)) continue;
                if (!col.TryGetComponent(out MonoBehaviour script)) continue;
                if (script is not Player && script is not Enemy) continue;

                if (!charactersInRange.Contains(script))
                    charactersInRange.Add(script);

                foreach (CampfireUpgradeEffect upgrade in activeUpgrades)
                {
                    if (Vector3.Distance(transform.position, script.transform.position) > upgrade.effectRadius) continue;

                    if (script is Player player)
                        upgrade.OnPlayerEnterRange(this, player);
                    else if (script is Enemy enemy)
                        upgrade.OnEnemyEnterRange(this, enemy);
                }
            }

            AudioManager.Instance.PlaySFX(fireSound, loop: true, position: transform.position);
        }

        private void OnDestroy()
        {
            DayNightCycle.Instance.OnDayAdvanced -= StartRegen;
        }

        private void StartRegen(int _)
        {
            StartCoroutine(RegenRoutine());
        }

        private IEnumerator RegenRoutine()
        {
            while (!health.IsFull)
            {
                if (DayNightCycle.Instance.IsNight()) yield break;

                health.AddHealth((int)RegenRate);
                yield return _waitForSeconds1;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out MonoBehaviour script)) return;
            if (script is not Player && script is not Enemy) return;

            if (!charactersInRange.Contains(script))
                charactersInRange.Add(script);

            foreach (CampfireUpgradeEffect upgrade in activeUpgrades)
            {
                if (Vector3.Distance(transform.position, script.transform.position) > upgrade.effectRadius) continue;

                if (script is Player player)
                    upgrade.OnPlayerEnterRange(this, player);
                else if (script is Enemy enemy)
                    upgrade.OnEnemyEnterRange(this, enemy);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out MonoBehaviour script)) return;
            if (script is not Player && script is not Enemy) return;

            charactersInRange.Remove(script);

            foreach (CampfireUpgradeEffect upgrade in activeUpgrades)
            {
                if (Vector3.Distance(transform.position, script.transform.position) > upgrade.effectRadius) continue;

                if (script is Player player)
                    upgrade.OnPlayerExitRange(this, player);
                else if (script is Enemy enemy)
                    upgrade.OnEnemyExitRange(this, enemy);
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

        /*public void UnlockGem(GemColor color)
        {
            if (unlockedGems.Contains(color)) return;

            unlockedGems.Add(color);
            UpdateGemVisibility();
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
        }*/

        public void Interact()
        {
            GameObject campfireMenu = InventoryManager.Instance.campfireMenuUI;
            if (campfireMenu == null) return;

            if (!campfireMenu.TryGetComponent(out RectTransform rectTransform)) return;
            if (!campfireMenu.TryGetComponent(out CanvasGroup canvasGroup)) return;

            UITween.DefaultOpenMenu(canvasGroup, rectTransform);

            CameraControlToggle.Instance.SetCameraControl(false);
        }

        public string GetInteractText()
        {
            IInteractable interactable = this;
            return $"Campfire\n{interactable.InteractKeyText}";
        }

        public Transform GetTransform() => transform;

        public void Die() => GameManager.Instance.GameOver(win: false);

        public string SaveState()
        {
            CampfireSaveData data = new()
            {
                currentHealth = health.GetHealth(),
                //unlockedGems = new List<GemColor>(unlockedGems)
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
                //UpdateGemVisibility();
                return;
            }

            health.SetHealth(data.currentHealth);

            /*unlockedGems.Clear();
            foreach (GemColor color in data.unlockedGems)
                unlockedGems.Add(color);

            UpdateGemVisibility();*/
        }
    }
}
