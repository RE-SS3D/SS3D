using Inventory;
using Mirror;
using UnityEngine;

namespace ItemComponents
{
    /// <summary>
    /// Component to handle items that can be used to paint with.
    /// Should be attached to such items.
    /// </summary>
    public class Painter : NetworkBehaviour, IItemWithSupply
    {
        [SerializeField] private PainterProperties propertiesPrefab = null;
        private PainterProperties properties = null;

        public PainterProperties Properties => properties;

        private void Start()
        {
            properties = Instantiate(propertiesPrefab);
            properties.name = propertiesPrefab.name;
        }

        public int GetSupplyDrainRate()
        {
            return properties.ItemSupply.DrainRate;
        }

        public void ChangeSupply(int amount)
        {
            int newValue = Mathf.Clamp(properties.ItemSupply.CurrentSupply + amount, 0, properties.ItemSupply.MaxSupply);
            properties.ItemSupply = properties.ItemSupply.WithNewSupplyValue(newValue);
        }

        public float GetRemainingSupplyPercentage()
        {
            return Mathf.Round(properties.ItemSupply.CurrentSupply * 100 / properties.ItemSupply.MaxSupply) / 100;
        }
    }
}