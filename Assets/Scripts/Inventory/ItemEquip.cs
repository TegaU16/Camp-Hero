using System.Collections;
using System.Collections.Generic;
using Game.Players;
using TMPro;
using UnityEngine;

namespace Game.Inventory
{
    public class ItemEquip : MonoBehaviour
    {
        public static ItemEquip Instance;

        private Player player;
        private Item equippedItem;
        private GameObject equippedItemObj;

        public GameObject HeldItem => equippedItemObj;

        [SerializeField] private TextMeshProUGUI itemText;
        [SerializeField] private float itemTextDisplayTime;
        [SerializeField] private float itemTextFadeInTime;
        [SerializeField] private float itemTextFadeOutTime;
        private Coroutine activeItemDisplayCoroutine;

        private ToolAttribute ToolAttribute => equippedItem.toolAttribute;

        private IEnumerable<(float, MultiplierStat)> AllMultipliers
        {
            get
            {
                yield return (ToolAttribute.damageMult, player.damageMultiplier);
                yield return (ToolAttribute.attackSpeedMult, player.attackSpeedMultiplier);
                yield return (ToolAttribute.knockbackForceMult, player.knockbackForceMultiplier);
                yield return (ToolAttribute.poiseDamageMult, player.poiseDamageMultiplier);
                yield return (ToolAttribute.critChanceMult, player.critChanceMultiplier);
                yield return (ToolAttribute.critFactorMult, player.critFactorMultiplier);
                yield return (ToolAttribute.resourceDropMult, player.resourceDropMultiplier);
            }
        }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            StartCoroutine(SetPlayer());
        }

        public void EquipItem(Item item)
        {
            if (player == null) return;

            if (equippedItemObj != null)
            {
                if (equippedItem != null && ToolAttribute != null)
                {
                    foreach ((_, MultiplierStat multiplierStat) in AllMultipliers)
                        Utility.RemoveMultiplierSource(equippedItemObj, multiplierStat);
                }

                Destroy(equippedItemObj);
            }
            
            if (item != null && equippedItem != item)
            {
                if (activeItemDisplayCoroutine != null)
                {
                    StopCoroutine(activeItemDisplayCoroutine);
                    activeItemDisplayCoroutine = null;
                }

                itemText.gameObject.SetActive(false);
                itemText.text = item.itemName;

                activeItemDisplayCoroutine = StartCoroutine(UIManager.Instance.DisplayTextRoutine(
                    itemText,
                    itemTextDisplayTime,
                    itemTextFadeInTime,
                    itemTextFadeOutTime)
                );
            }

            if (item == null || item.equippedPrefab == null)
            {
                equippedItem = null;
                return;
            }

            equippedItem = item;
            equippedItemObj = Instantiate(item.equippedPrefab);
            equippedItemObj.transform.SetParent(player.itemHolder, worldPositionStays: false); // keep prefab offsets

            if (ToolAttribute == null) return;

            foreach ((float attributeMult, MultiplierStat multiplierStat) in AllMultipliers)
                Utility.SetMultiplierSource(equippedItemObj, attributeMult, multiplierStat);
        }

        private IEnumerator SetPlayer()
        {
            while (GameManager.Instance.playerInstance == null)
                yield return null;

            player = GameManager.Instance.playerInstance.GetComponent<Player>();
        }
    }
}
