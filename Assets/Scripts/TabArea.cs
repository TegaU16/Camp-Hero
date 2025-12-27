using UnityEngine;

public class TabArea : MonoBehaviour
{
    public Tab[] tabs;

    void Start()
    {
        if (tabs.Length > 0)
            SelectTab(tabs[0]);
    }

    public void SelectTab(Tab selectedTab)
    {
        foreach (Tab tab in tabs)
        {
            if (tab == selectedTab)
            {
                tab.Select();
                continue;
            }

            tab.Deselect();
        }
    }
}
