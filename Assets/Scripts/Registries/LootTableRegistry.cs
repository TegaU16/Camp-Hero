using Game.Storage;

namespace Game.Registries
{
    public class LootTableRegistry : BaseRegistry<LootTable, string>
    {
        public static LootTableRegistry Instance;

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

        protected override string GetKey(LootTable entry) => entry.name;
    }
}
