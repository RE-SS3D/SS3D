using SS3D.Core;
using SS3D.Core.Behaviours;
using System.Electricity;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Put this script on anything that need to be part of an electric circuit.
    /// This script add the game object it's on to an electric circuit.
    /// </summary>
    public class BasicElectricDevice : NetworkActor, IElectricDevice
    {
        public PlacedTileObject TileObject => gameObject.GetComponent<PlacedTileObject>();

        public override void OnStartServer()
        {
            base.OnStartServer();

            ElectricitySystem electricitySystem = Subsystems.Get<ElectricitySystem>();
            if (electricitySystem.IsSetUp)
                electricitySystem.AddElectricalElement(this);
            else
                electricitySystem.OnSystemSetUp += OnElectricitySystemSetup;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            ElectricitySystem electricitySystem = Subsystems.Get<ElectricitySystem>();
            electricitySystem.RemoveElectricalElement(this);
        }

        private void OnElectricitySystemSetup()
        {
            ElectricitySystem electricitySystem = Subsystems.Get<ElectricitySystem>();
            electricitySystem.AddElectricalElement(this);
        }
    }
}
