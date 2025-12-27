using UnityEngine;
using UnityEngine.UI;

namespace Game.Smelting
{
    public class FurnaceItem : MonoBehaviour
    {
        [HideInInspector] public SmeltingRecipe recipe;

        public Image itemImage;

        // Called by attached button
        public void SetRequirements() => FurnaceManager.Instance.furnaceUI.SetSelectedFurnaceItem(this);
    }
}
