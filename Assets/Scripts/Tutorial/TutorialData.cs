using System;

namespace Game.Tutorial
{
    [Serializable]
    public class TutorialData
    {
        public string id;
        public string description;
        public TutorialType type;
        
        public float duration = 0f;
        public float cooldown = 10f;

        public CraftingRecipeHighlightTutorial craftingRecipeHighlightData;
        public SmeltingRecipeHighlightTutorial smeltingRecipeHighlightData;
        public ObjectHighlightTutorialData objectHighlightData;

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
