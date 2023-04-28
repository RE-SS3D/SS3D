using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Representation of a character, it is what "owns" an entity (player controllable).
    /// A character is controlled by a Player, a Player can control multiple characters (not at the same time).
    /// </summary>
    public class Character : NetworkActor
    {
        [SyncVar(OnChange = nameof(SyncPlayer))]
        public Player player;

        [SyncVar]
        public Entity Entity;

        public static Character Empty { get; private set; }

        protected override void OnStart()
        {
            base.OnStart();

            Empty = Subsystems.Get<CharacterSystem>().EmptyCharacter;
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

            name = $"Character - {newPlayer.Ckey}";
        }
    }
}