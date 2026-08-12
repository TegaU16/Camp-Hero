using Game.Inventory;

namespace Game.Registries
{
    public class ItemRegistry : BaseRegistry<Item, string>
    {
        public static ItemRegistry Instance;

        private new void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            base.Awake();
        }

        protected override string GetKey(Item entry) => entry.itemName;
    }
}
