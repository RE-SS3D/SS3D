using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Systems.Atmospherics
{
    public sealed partial class AtmosSubSystem
    {
        /// <summary>Server only, and only while a simulation exists.</summary>
        public AtmosLooseBodies LooseBodies { get; private set; }

        private void TickWind()
        {
            foreach (Ragdoll body in Ragdoll.WindBodies)
            {
                if (body == null || !body.IsServer) continue;
                body.ServerApplyWind(AtmosWind.SampleAt(_simulation, body.WindSamplePosition));
            }

            LooseBodies?.Tick(_simulation);
        }

        private void FixedUpdate()
        {
            if (!SimulationPaused) LooseBodies?.ApplyForces();
        }
    }
}
