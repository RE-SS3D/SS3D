using SS3D.Core;
using SS3D.Core.Behaviours;

namespace SS3D.Systems.Atmospherics
{
    public class AtmosBlocker : NetworkActor
    {
        public override void OnStartServer()
        {
            base.OnStartServer();
            Subsystems.Get<AtmosEnvironmentSubSystem>().ChangeState(Position, AtmosState.Blocked);
        }
    }
}
