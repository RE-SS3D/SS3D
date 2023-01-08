using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Representation of a mind, it is what "owns" an entity (player controllable).
    /// A mind is controlled by a Soul, a Soul can control multiple minds (not at the same time).
    /// </summary>
    public class Mind : NetworkActor
    {
        [SyncVar(OnChange = nameof(SyncSoul))] public Soul Soul;

        [Server]
        public void SetSoul(Soul soul)
        {
            Soul = soul;
        }

        public void SyncSoul(Soul oldSoul, Soul newSoul, bool asServer)
        {
            if (!IsServer && IsHost)
            {
                return;
            }
        }
    }
}