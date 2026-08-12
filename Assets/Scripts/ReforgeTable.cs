using System.Linq;
using Game.Inventory;
using Game.Saving;
using Game.Storage;
using UnityEngine;

namespace Game.Reforge
{
    public class ReforgeTable : StorageContainer, ISaveableObject, IInteractable
    {
        private enum SlotIndex
        {
            Tool,
            Material
        }

        private readonly InventorySlot[] inventorySlots = new InventorySlot[2];

        [HideInInspector] public InventorySlot toolSlot;
        [HideInInspector] public InventorySlot materialSlot;

        public Sprite reforgeTableIcon;
        public Sprite ObjectIcon => reforgeTableIcon;

        private int Tool => (int)SlotIndex.Tool;
        private int Material => (int)SlotIndex.Material;

        private Item ToolItem => GetItemFromSlot(Tool);
        private Item MaterialItem => GetItemFromSlot(Material);

        private void Awake()
        {
            items = new ItemData[2];
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            InventoryManager.Instance.OnInventoryItemChanged += SaveUI;

            inventorySlots[Tool] = toolSlot;
            inventorySlots[Material] = materialSlot;
        }

        // Update is called once per frame
        void Update()
        {
            if (!GameManager.Instance.IsGameActive) return;
            if (toolSlot == null || materialSlot == null) return;

            SaveUI();
        }

        public void LoadUI()
        {
            RefreshSlot(toolSlot, items[Tool]);
            RefreshSlot(materialSlot, items[Material]);
        }

        public void SaveUI(InventorySlot changedSlot = null)
        {
            if (changedSlot == toolSlot)
                SaveSlot(toolSlot, Tool);
            else if (changedSlot == materialSlot)
                SaveSlot(materialSlot, Material);
        }

        public void GiveToolAttribute()
        {
            if (ToolItem == null || MaterialItem == null) return;
            if (!ToolItem.itemsToPair.Contains(MaterialItem)) return;

            InventoryItem toolInvItem = toolSlot.GetComponentInChildren<InventoryItem>();
            if (toolInvItem == null) return;

            InventoryManager.Instance.SetAttributeForTool(toolInvItem.item, toolInvItem);

            items[Material].count--;
            if (items[Material].count <= 0)
                items[Material] = null;

            RefreshSlot(materialSlot, items[Material]);
        }

        public string SaveState()
        {
            ReforgeTableSaveData data = new()
            {
                tool = items[Tool],
                material = items[Material]
            };

            return JsonUtility.ToJson(data);
        }

        public void LoadState(string json)
        {
            ReforgeTableSaveData data = JsonUtility.FromJson<ReforgeTableSaveData>(json);
            if (data == null) return;

            items[Tool] = data.tool;
            items[Material] = data.material;
        }

        public void Interact()
        {
            if (!InventoryManager.Instance.reforgeMenuUI.TryGetComponent(out ReforgeTableUI reforgeTableUI)) return;

            reforgeTableUI.Open(this);
        }

        public string GetInteractText() => "Use Reforge Table\n<color=#27ef60>\"E\"</color>";

        public Transform GetTransform() => transform;
    }
}
