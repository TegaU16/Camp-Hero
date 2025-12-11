using System.Collections.Generic;
using System.Linq;
using Game.Registries;
using Game.Saving;
using UnityEngine;
using Worlds;

namespace Game.Quests
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance;

        private readonly List<Quest> activeQuests = new();
        public Quest starterQuest;
        public Quest trialQuest;
        [HideInInspector] public bool hasOpenedTrialMenu = false;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void AddQuest(Quest quest)
        {
            if (activeQuests.Exists(q => q.questID == quest.questID))
            {
                Debug.LogWarning($"Quest {quest.questID} already active!");
                return;
            }

            activeQuests.Add(quest);
            Debug.Log($"Quest added: {quest.title}");
        }

        public void CompleteQuest(string questID)
        {
            Quest quest = activeQuests.Find(q => q.questID == questID);
            if (quest != null && !quest.isCompleted)
                quest.CompleteQuest();
        }

        public void AddProgress(string questID, int amount = 1)
        {
            Quest quest = activeQuests.Find(q => q.questID == questID);
            quest?.AddProgress(amount);
        }

        private void ClearActiveQuests() => activeQuests.Clear();

        public List<Quest> GetActiveQuests() => activeQuests;

        public void SaveQuestData()
        {
            WorldQuestSaveData worldQuestSaveData = new();

            foreach (Quest quest in activeQuests)
            {
                if (quest != null)
                {
                    QuestSaveData questSaveData = new()
                    {
                        questID = quest.questID,
                        questTitle = quest.title,
                        isCompleted = quest.isCompleted,
                        requiredItems = quest.requiredItems.Where(x => x != null).Select(x => x.name).ToList(),
                        requiredCount = quest.requiredCount,
                        currentCount = quest.currentCount
                    };

                    worldQuestSaveData.questSaveDatas.Add(questSaveData);
                    worldQuestSaveData.hasOpenedTrialMenu = hasOpenedTrialMenu;
                }
            }

            SaveSystem.SaveQuestData(WorldSession.CurrentWorldName, worldQuestSaveData);
        }

        public WorldQuestSaveData LoadQuestData()
        {
            ClearActiveQuests();

            WorldQuestSaveData worldQuestSaveData = SaveSystem.LoadQuestData(WorldSession.CurrentWorldName);
            if (worldQuestSaveData != null)
            {
                foreach (QuestSaveData questSaveData in worldQuestSaveData.questSaveDatas)
                {
                    Quest quest = new(
                        questSaveData.questID,
                        questSaveData.questTitle,
                        ItemRegistry.GetItemsByName(questSaveData.requiredItems),
                        questSaveData.requiredCount
                        )
                    {
                        isCompleted = questSaveData.isCompleted,
                        currentCount = questSaveData.currentCount
                    };

                    AddQuest(quest);
                }

                hasOpenedTrialMenu = worldQuestSaveData.hasOpenedTrialMenu;
                return worldQuestSaveData;
            }
            else
            {
                List<QuestSaveData> defaultQuestList = new();
                QuestSaveData starterQuestData = new()
                {
                    questID = starterQuest.questID,
                    questTitle = starterQuest.title,
                    isCompleted = starterQuest.isCompleted,
                    requiredItems = starterQuest.requiredItems.Where(x => x != null).Select(x => x.itemName).ToList(),
                    requiredCount = starterQuest.requiredCount,
                    currentCount = starterQuest.currentCount
                };
                defaultQuestList.Add(starterQuestData);

                WorldQuestSaveData defaultWorldQuestSaveData = new()
                {
                    questSaveDatas = defaultQuestList
                };

                AddQuest(starterQuest);

                return defaultWorldQuestSaveData;
            }
        }
    }
}
