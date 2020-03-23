using SS3D.Engine.Inventory;
using Mirror;
using UnityEngine;

namespace SS3D.Content.Items.Functional.Generic.Crayons
{
    /// <summary>
    /// Component to handle items that can be used to paint with.
    /// Should be attached to such items.
    /// </summary>
    public class Painter : NetworkBehaviour, IItemWithSupply
    {
        [SerializeField] private PainterProperties propertiesPrefab = null;

        public PainterProperties Properties { get; private set; } = null;

        private void Start()
        {
            Properties = Instantiate(propertiesPrefab);
            Properties.name = propertiesPrefab.name;
        }

        public int GetSupplyDrainRate()
        {
            return Properties.ItemSupply.DrainRate;
        }

        public void ChangeSupply(int amount)
        {
            int newValue = Mathf.Clamp(Properties.ItemSupply.CurrentSupply + amount, 0, Properties.ItemSupply.MaxSupply);
            Properties.ItemSupply = Properties.ItemSupply.WithNewSupplyValue(newValue);
        }

        public float GetRemainingSupplyPercentage()
        {
            return Mathf.Round(Properties.ItemSupply.CurrentSupply * 100 / Properties.ItemSupply.MaxSupply) / 100;
        }
    }
}