using UnityEngine;
using UnityEngine.UI;

namespace Game.Smelting
{
    public class FurnaceItem : MonoBehaviour
    {
        [HideInInspector] public SmeltingRecipe recipe;

        public Image itemImage;

        public void SetRequirements()
        {
            FurnaceManager.Instance.SetSelectedFurnaceItem(this);
        }
    }
}
