using System.Collections.Generic;
using Game.Inventory;
using UnityEngine;

namespace Game.Quests
{
    [System.Serializable]
    public class Quest
    {
        public string questID;
        public string title;
        public bool isCompleted;

        public List<Item> requiredItems; // Example: "Wood", "Stone"
        public int requiredCount; // Example: need 5 of each item

        public int currentCount;

        private List<string> collectedItemNames;

        public Quest(string id, string title, List<Item> requiredItems, int requiredCount)
        {
            this.questID = id;
            this.title = title;
            this.requiredItems = requiredItems;
            this.requiredCount = requiredCount;
            this.isCompleted = false;
            this.currentCount = 0;

            this.collectedItemNames = new List<string>();
        }

        public void AddProgress(int amount = 1)
        {
            if (isCompleted) return;

            currentCount += amount;
            if (currentCount >= requiredCount)
                CompleteQuest();
        }

        public void OnItemCollected(Item item)
        {
            collectedItemNames ??= new List<string>();

            if (isCompleted) return;
            if (item == null) return;

            bool isRequired = requiredItems.Contains(item);
            if (!isRequired) return;

            // Only count the item once
            if (collectedItemNames.Contains(item.name)) return;

            collectedItemNames.Add(item.name);
            AddProgress();
        }

        public void CompleteQuest()
        {
            isCompleted = true;
            Debug.Log($"Quest '{title}' completed!");
        }
    }
}
