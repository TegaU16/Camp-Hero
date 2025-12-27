using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public GameObject[] menus;

    public void NavigateTo(GameObject destination)
    {
        if (destination != null)
            destination.SetActive(true);

        foreach (GameObject menu in menus)
        {
            if (menu != null && menu != destination)
                menu.SetActive(false);
        }
    }

    // Called by quit button
    public void QuitGame() => Application.Quit();
}
