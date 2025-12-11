using UnityEngine;
using UnityEngine.UI;

namespace Game.Crafting
{
    [RequireComponent(typeof(Button))]
    public class CraftingTab : MonoBehaviour
    {
        public CraftingCategory category;

        private void Start()
        {
            Button button = GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                CraftingManager.Instance.FilterByCategory(category, button);
            });
        }
    }
}
