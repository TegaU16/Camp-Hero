using UnityEngine;
using UnityEngine.UI;

public class CompanionUIManager : MonoBehaviour
{
    public GameObject taskListContainer;
    public GameObject taskTogglePrefab; // A prefab with a Text and a Toggle

    public static CompanionUIManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void LoadCompanion(Companion companion)
    {
        foreach (Transform child in taskListContainer.transform)
        {
            Destroy(child.gameObject); // Clear old tasks
        }

        foreach (var task in companion.tasks)
        {
            var toggleObj = Instantiate(taskTogglePrefab, taskListContainer.transform);
            var toggle = toggleObj.GetComponentInChildren<Toggle>();
            var label = toggleObj.GetComponentInChildren<Text>();

            label.text = task.taskName;
            toggle.isOn = task.isEnabled;

            toggle.onValueChanged.AddListener((value) => {
                task.isEnabled = value;
            });
        }
    }
}
