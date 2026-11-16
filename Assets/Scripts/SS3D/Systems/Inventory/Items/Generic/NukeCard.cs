using SS3D.Core;

namespace SS3D.Systems.Inventory.Items.Generic
{
    /// <summary>
    /// The syndies love to use this to blow up the place
    /// </summary>
    public class NukeCard : Item
    {
        /// <summary>Server: registered while spawned, so the disk checks find every live disk, held or in a bag.</summary>
        public override void OnStartServer()
        {
            base.OnStartServer();
            SceneRegistry<NukeCard>.Register(this);
        }

        public override void OnStopServer()
        {
            SceneRegistry<NukeCard>.Unregister(this);
            base.OnStopServer();
        }
    }
}