using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Systems.Atmospherics
{
    public sealed partial class AtmosSubSystem
    {
        private void TickWind()
        {
            foreach (Ragdoll body in Ragdoll.WindBodies)
            {
                if (body == null || !body.IsServer) continue;
                body.ServerApplyWind(AtmosWind.SampleAt(_simulation, body.WindSamplePosition));
            }
        }
    }
}
