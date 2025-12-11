using System;

namespace Game.Tutorial
{
    [Serializable]
    public class TutorialData
    {
        public string id; // Unique tutorial ID
        public string description;
        public TutorialType type;     // Enum: Highlight, Popup, Hint, etc.
        public string targetTag;      // For highlight type
        public float duration = 0f;   // Auto-timeout after N seconds (0 = no timeout)
        public float cooldown = 10f;  // Minimum delay before this tutorial can re-trigger

        public Func<bool> triggerCondition;   // When to show tutorial
        public Func<bool> completionCondition; // Optional: When to auto-complete

        public CraftingRecipeHighlightTutorial craftingRecipeHighlightData;
        public SmeltingRecipeHighlightTutorial smeltingRecipeHighlightData;

        [NonSerialized] public bool isActive;
        [NonSerialized] public float lastTriggeredTime = -999f;
    }

    public enum TutorialType
    {
        HighlightObject,
        PopupMessage,
        OnscreenHint
    }
}
