using Game.Inventory;

namespace Game.Smelting
{
    [System.Serializable]
    public class FuelComponent
    {
        public float maxFuel = 100f;
        public float currentFuel = 0f;
        public float fuelUseRate = 1.0f;

        public void AddFuel(Item item)
        {
            if (item == null || item.fuelValue <= 0) return;

            currentFuel += item.fuelValue;
            if (currentFuel > maxFuel)
                currentFuel = maxFuel;
        }

        public void Consume(float amount)
        {
            currentFuel -= amount;
            if (currentFuel < 0)
                currentFuel = 0;
        }

        public float GetFuelRatio()
        {
            float ratio = currentFuel / maxFuel;
            return ratio;
        }
    }
}
