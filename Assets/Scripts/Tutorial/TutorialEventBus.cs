using System;

namespace Game.Tutorial
{
    public static class TutorialEventBus
    {
        public static event Action<TutorialData> OnTutorialTriggered;
        public static event Action<TutorialData> OnTutorialCompleted;
        public static event Action<TutorialData> OnTutorialTimedOut;

        public static void TriggerTutorial(TutorialData data) => OnTutorialTriggered?.Invoke(data);

        public static void CompleteTutorial(TutorialData data) => OnTutorialCompleted?.Invoke(data);

        public static void TimeoutTutorial(TutorialData data) => OnTutorialTimedOut?.Invoke(data);
    }
}
