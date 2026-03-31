using UnityEngine;
using UnityEngine.UI;

namespace Game.Crafting
{
    [RequireComponent(typeof(Button))]
    public class CraftingTab : MonoBehaviour
    {
        [SerializeField] private CraftingCategory category;

        private void Start()
        {
            Button button = GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                CraftingUI.Instance.SetSelectedCategory(category, button);
                CraftingUI.Instance.FilterByCategory(category, button);
            });
        }
    }
}
