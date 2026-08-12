using Game.Inventory;

namespace Game.Registries
{
    public class ToolAttributeRegistry : BaseRegistry<ToolAttribute, string>
    {
        public static ToolAttributeRegistry Instance;

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

        protected override string GetKey(ToolAttribute entry) => entry.attributeID;
    }
}
