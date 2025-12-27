using UnityEngine;
using UnityEngine.UI;

namespace Game.Smelting
{
    [RequireComponent(typeof(Button))]
    public class SmeltingTab : MonoBehaviour
    {
        public SmeltingCategory category;

        private void Start()
        {
            Button button = GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                FurnaceManager.Instance.furnaceUI.FilterByCategory(category, button);
            });
        }
    }
}
