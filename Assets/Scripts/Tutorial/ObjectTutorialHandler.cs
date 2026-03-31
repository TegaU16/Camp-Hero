using UnityEngine;

namespace Game.Tutorial
{
    public class ObjectTutorialHandler : MonoBehaviour
    {
        [SerializeField] private GameObject highlightPrefab;
        [SerializeField] private Canvas worldCanvas;

        private ObjectHighlightTutorial activeHighlight;

        private void OnEnable()
        {
            TutorialEventBus.OnTutorialTriggered += OnTutorialTriggered;
            TutorialEventBus.OnTutorialCompleted += OnTutorialEnded;
            TutorialEventBus.OnTutorialTimedOut += OnTutorialEnded;
        }

        private void OnDisable()
        {
            TutorialEventBus.OnTutorialTriggered -= OnTutorialTriggered;
            TutorialEventBus.OnTutorialCompleted -= OnTutorialEnded;
            TutorialEventBus.OnTutorialTimedOut -= OnTutorialEnded;
        }

        private void OnTutorialTriggered(TutorialData data)
        {
            if (data.type != TutorialType.HighlightObject) return;
            if (data.objectHighlightData == null) return;

            if (activeHighlight != null)
                Destroy(activeHighlight.gameObject);

            GameObject instance = Instantiate(highlightPrefab, worldCanvas.transform);
            activeHighlight = instance.GetComponent<ObjectHighlightTutorial>();

            activeHighlight.Initialize(data.objectHighlightData);
        }

        private void OnTutorialEnded(TutorialData data)
        {
            if (activeHighlight != null)
                Destroy(activeHighlight.gameObject);
        }
    }
}
