using System.Collections.Generic;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Tracks active vent/scrubber/pump devices for post-pipe-sim transfer ticks.
    /// </summary>
    public sealed class AtmosPortRegistry
    {
        private readonly List<IAtmosPortDevice> _ports = new();

        public void Register(IAtmosPortDevice port)
        {
            if (port != null && !_ports.Contains(port))
                _ports.Add(port);
        }

        public void Unregister(IAtmosPortDevice port)
        {
            if (port != null)
                _ports.Remove(port);
        }

        public void TickDevices(AtmosPipeSimulation pipeSimulation, AtmosSimulation turfSimulation, float deltaTime)
        {
            if (pipeSimulation == null || turfSimulation == null)
                return;

            for (int i = _ports.Count - 1; i >= 0; i--)
            {
                IAtmosPortDevice port = _ports[i];
                if (port == null)
                {
                    _ports.RemoveAt(i);
                    continue;
                }

                port.ServerTick(pipeSimulation, turfSimulation, deltaTime);
            }
        }
    }
}
