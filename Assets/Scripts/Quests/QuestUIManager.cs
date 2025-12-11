using UnityEngine;
using System.Collections.Generic;

namespace Game.Quests
{
    public class QuestUIManager : MonoBehaviour
    {
        public static QuestUIManager Instance;

        [Header("UI Setup")]
        public Transform questListContainer;  // parent object (Vertical Layout Group)
        public GameObject questUIPrefab;

        private readonly Dictionary<string, QuestUIElement> uiElements = new();

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (QuestManager.Instance == null) return;

            List<Quest> quests = QuestManager.Instance.GetActiveQuests();

            // Create missing UI elements
            foreach (Quest quest in quests)
            {
                if (!uiElements.ContainsKey(quest.questID))
                {
                    GameObject obj = Instantiate(questUIPrefab, questListContainer);
                    QuestUIElement element = obj.GetComponent<QuestUIElement>();
                    element.Init(quest);
                    uiElements.Add(quest.questID, element);
                }

                // Update existing
                uiElements[quest.questID].Refresh();
            }
        }
    }
}
