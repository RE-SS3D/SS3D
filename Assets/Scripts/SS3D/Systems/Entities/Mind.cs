using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Representation of a mind, it is what "owns" an entity (player controllable).
    /// A mind is controlled by a Player, a Player can control multiple minds (not at the same time).
    /// </summary>
    public class Mind : NetworkActor
    {
        [SyncVar(OnChange = nameof(SyncPlayer))]
        public Player Player;

        [SyncVar]
        public Entity Entity;

        public static Mind Empty { get; private set; }

        [Server]
        public void SetPlayer(Player player)
        {
            this.Player = player;
        }

        public void SyncPlayer(Player oldPlayer, Player newPlayer, bool asServer)
        {
            if (!IsServer && IsHost)
            {
                return;
            }

            name = $"Mind - {newPlayer.Ckey}";
        }

        protected override void OnStart()
        {
            base.OnStart();

            Empty = Subsystems.Get<MindSystem>().EmptyMind;
        }
    }
}