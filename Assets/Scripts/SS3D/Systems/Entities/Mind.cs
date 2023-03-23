using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Representation of a mind, it is what "owns" an entity (player controllable).
    /// A mind is controlled by a Soul, a Soul can control multiple minds (not at the same time).
    /// </summary>
    public class Mind : NetworkActor
    {
        [SyncVar(OnChange = nameof(SyncSoul))]
        public Soul Soul;

        [SyncVar]
        public Entity Entity;

        public static Mind Empty { get; private set; }

        protected override void OnStart()
        {
            base.OnStart();

            Empty = Subsystems.Get<MindSystem>().EmptyMind;
        }

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

            name = $"Mind - {newSoul.Ckey}";
        }
    }
}