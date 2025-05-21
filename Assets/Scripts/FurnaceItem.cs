using UnityEngine;
using UnityEngine.UI;

public class FurnaceItem : MonoBehaviour
{
    [HideInInspector] public SmeltingRecipe recipe;

    public Image itemImage;
    public GameObject requirementPrefab;

    [HideInInspector] public Image requirementBackground;

    public void SetRequirements()
    {
        FurnaceManager.Instance.SetSelectedFurnaceItem(this);
    }
}
