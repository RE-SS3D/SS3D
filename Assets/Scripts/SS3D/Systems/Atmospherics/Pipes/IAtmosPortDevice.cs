using SS3D.Systems.Tile;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Server-side atmos port ticked after turf diffusion and pipe bulk simulation.
    /// </summary>
    public interface IAtmosPortDevice : IAtmosPipeElement
    {
        bool IsEnabled { get; }

        void ServerTick(AtmosPipeSimulation pipeSimulation, AtmosSimulation turfSimulation, float deltaTime);
    }
}
