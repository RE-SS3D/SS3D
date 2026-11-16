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
        public Player player;

        [SyncVar]
        public Entity Entity;

        /// <summary>In-character name from the player's lobby character; empty if none was sent.</summary>
        [SyncVar]
        public string CharacterName;

        /// <summary>The name shown in-world: the character name, else the ckey.</summary>
        public string DisplayName => !string.IsNullOrEmpty(CharacterName) ? CharacterName : player?.Ckey;

        public static Mind Empty { get; private set; }

        protected override void OnStart()
        {
            base.OnStart();

            Empty = SubSystems.Get<MindSubSystem>().EmptyMind;
        }

        [Server]
        public void SetPlayer(Player player)
        {
            this.player = player;
        }

        public void SyncPlayer(Player oldPlayer, Player newPlayer, bool asServer)
        {
            if (!IsServer && IsHost)
            {
                return;
            }

            name = $"Mind - {newPlayer.Ckey}";
        }
    }
}