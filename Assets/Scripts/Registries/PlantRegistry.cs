using Game.Food;

namespace Game.Registries
{
    public class PlantRegistry : BaseRegistry<PlantData, string>
    {
        public static PlantRegistry Instance;

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

        protected override string GetKey(PlantData entry) => entry.plant.itemName;
    }
}
