using SS3D.Engine.Inventory;
using Mirror;
using UnityEngine;

namespace SS3D.Content.Items.Functional.Generic.PowerCells
{
    /// <summary>
    /// Handles powercells
    /// </summary>
    public class PowerCell : NetworkBehaviour, IChargeable
    {
        [SerializeField] private PowerCellProperties propertiesPrefab = null;

        public PowerCellProperties Properties { get; private set; } = null;

        private void Start()
        {
            Properties = Instantiate(propertiesPrefab);
            Properties.name = propertiesPrefab.name;
        }

        public int GetChargeRate()
        {
            return Properties.PowerSupply.ChargeRate;
        }

        public void AddCharge(int amount)
        {
            int newValue = Mathf.Clamp(Properties.PowerSupply.Charge + amount, 0, Properties.PowerSupply.MaxCharge);
            Properties.PowerSupply = Properties.PowerSupply.WithCharge(newValue);
        }

        public float GetPowerPercentage()
        {
            return Mathf.Round(Properties.PowerSupply.Charge * 100 / Properties.PowerSupply.MaxCharge) / 100;
        }
    }
}