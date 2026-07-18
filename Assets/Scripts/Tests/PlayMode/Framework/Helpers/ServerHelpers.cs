using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using System.Linq;

namespace SS3D.Tests
{
    /// <summary>
    /// This class is simply a container for helper methods for the Server, for use
    /// in UnityTests. Only valid for single-process (Host or in-Editor DedicatedServer)
    /// PlayMode tests - the stub broadcast methods it calls are Editor-only and don't compile
    /// into a real built player/server. For real multi-process coverage, see
    /// Testing/multiplayer/ (Documents/architecture/2026-07_multiplayer-test-harness.md).
    /// </summary>
    public static class ServerHelpers
    {
        /// <summary>
        /// Sets all players ready.
        /// </summary>
        public static void SetAllPlayersReady()
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            ReadyPlayersSubSystem readyPlayersSystem = SubSystems.Get<ReadyPlayersSubSystem>();
            ChangePlayerReadyMessage msg;

            foreach (Player player in playerSystem.OnlinePlayers)
            {
                msg = new ChangePlayerReadyMessage(player.Ckey, true);
                readyPlayersSystem.ChangePlayerReadyMessageStubBroadcast(player.LocalConnection, msg);
            }
        }

        public static void SetPlayerReadiness(string Ckey, bool readiness)
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            ReadyPlayersSubSystem readyPlayersSystem = SubSystems.Get<ReadyPlayersSubSystem>();
            Player player = playerSystem.OnlinePlayers.ToList().Find(soul => soul.Ckey == Ckey);
            ChangePlayerReadyMessage msg = new ChangePlayerReadyMessage(Ckey, readiness);
            readyPlayersSystem.ChangePlayerReadyMessageStubBroadcast(player.LocalConnection, msg);
        }

        /// <summary>
        /// Turn the round on or off.
        /// </summary>
        public static void ChangeRoundState(bool running)
        {
            RoundSubSystem roundSystem = SubSystems.Get<RoundSubSystem>();
            ChangeRoundStateMessage msg = new ChangeRoundStateMessage(running);
            roundSystem.ChangeRoundStateMessageStubBroadcast(msg);
        }

        public static void SpawnLatePlayer(string Ckey)
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();

            Player player = playerSystem.GetPlayer(Ckey);
            entitySystem.CmdSpawnLatePlayer(player);
        }
    }
}
