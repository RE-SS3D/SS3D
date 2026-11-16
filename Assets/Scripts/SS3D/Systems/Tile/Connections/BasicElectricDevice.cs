using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Electricity;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Put this script on anything that need to be part of an electric circuit.
    /// This script add the game object it's on to an electric circuit.
    /// </summary>
    public class BasicElectricDevice : NetworkActor, IElectricDevice
    {
        // Destroyed Unity objects throw on .gameObject; callers (area/APC power) null-check TileObject.
        public PlacedTileObject TileObject => this ? GetComponent<PlacedTileObject>() : null;

        public override void OnStartServer()
        {
            base.OnStartServer();

            ElectricitySubSystem electricitySystem = SubSystems.Get<ElectricitySubSystem>();
            if (electricitySystem.IsReady)
                electricitySystem.AddElectricalElement(this);
            else
                electricitySystem.WhenReady += OnElectricitySystemSetup;
        }

        // Unregister on despawn: FishNet clears IsServer before Unity's OnDestroy, so a server-gated
        // OnDestroyed never ran and removed devices lingered until the next graph rebuild pruned them.
        public override void OnStopServer()
        {
            if (SubSystems.TryGet(out ElectricitySubSystem electricitySystem))
            {
                electricitySystem.WhenReady -= OnElectricitySystemSetup;
                electricitySystem.RemoveElectricalElement(this);
            }

            base.OnStopServer();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            // Tile icon previews DestroyImmediate prefabs that never started as server objects.
            if (SubSystems.TryGet(out ElectricitySubSystem electricitySystem))
                electricitySystem.WhenReady -= OnElectricitySystemSetup;
        }

        private void OnElectricitySystemSetup()
        {
            ElectricitySubSystem electricitySystem = SubSystems.Get<ElectricitySubSystem>();
            electricitySystem.WhenReady -= OnElectricitySystemSetup;
            electricitySystem.AddElectricalElement(this);
        }
    }
}
