using SS3D.Core;
using SS3D.Core.Behaviours;

namespace SS3D.Systems.Atmospherics.AtmosRework.Machinery
{
    public abstract class BasicAtmosDevice : NetworkActor, IAtmosDevice
    {
        public override void OnStartServer()
        {
            base.OnStartServer();
            if (Subsystems.Get<PipeSubSystem>().IsSetUp)
            {
                Subsystems.Get<PipeSubSystem>().RegisterAtmosDevice(this);
            }
            else
            {
                Subsystems.Get<PipeSubSystem>().OnSystemSetUp += () => Subsystems.Get<PipeSubSystem>().RegisterAtmosDevice(this);
            }
        }

        public abstract void StepAtmos(float dt);

        protected void OnDestroy()
        {
            Subsystems.Get<PipeSubSystem>().RemoveAtmosDevice(this);
        }
    }
}
