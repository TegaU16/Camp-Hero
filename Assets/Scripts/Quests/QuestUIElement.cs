using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Quests
{
    public class QuestUIElement : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI progressText;
        public Image statusIcon;

        [Header("Status Sprites")]
        public Sprite incompleteSprite;
        public Sprite completeSprite;

        private Quest linkedQuest;

        public void Init(Quest quest)
        {
            linkedQuest = quest;
            Refresh();
        }

        public void Refresh()
        {
            if (linkedQuest == null) return;

            titleText.text = linkedQuest.title;
            progressText.text = $"{linkedQuest.currentCount}/{linkedQuest.requiredCount}";
            statusIcon.sprite = linkedQuest.isCompleted ? completeSprite : incompleteSprite;
        }
    }
}
