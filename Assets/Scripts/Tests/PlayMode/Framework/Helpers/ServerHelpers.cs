using FishNet;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SS3D.Tests
{
    /// <summary>
    /// This class is simply a container for helper methods for the Server, for use
    /// in UnityTests. Only valid for single-process (Host or in-Editor DedicatedServer)
    /// PlayMode tests - the stub broadcast methods it calls are Editor-only and don't compile
    /// into a real built player/server. For real multi-process coverage, see
    /// Testing/multiplayer/ (Documents/architecture/systems/networking-session-testing.md).
    /// </summary>
    public static class ServerHelpers
    {
        /// <summary>Ckey of the local player in single-process PlayMode tests (see PlayModeTest.SetApplicationSettings).</summary>
        public const string LocalCkey = "john";

        /// <summary>
        /// Round-end summary beat in PlayMode tests: the 3 s Ending delay rounds had before the 30 s summary,
        /// so tests that stop a round for set-up or teardown don't each wait 30 s (testing philosophy D7).
        /// </summary>
        public const int TestEndBeatSeconds = 3;

        /// <summary>Shortens the round-end beat to <see cref="TestEndBeatSeconds"/> for this server session. No-op off the server.</summary>
        public static void UseTestEndBeat()
        {
            if (InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started
                && SubSystems.TryGet(out RoundSubSystem rounds))
            {
                rounds.SetEndBeatSecondsForTesting(TestEndBeatSeconds);
            }
        }

        public static RoundState CurrentRoundState => SubSystems.Get<RoundSubSystem>().CurrentRoundState;

        /// <summary>Polls the server's round state (no event observer needed).</summary>
        public static IEnumerator WaitForRoundState(RoundState target, float timeout = 30f)
        {
            float startTime = Time.time;
            while (CurrentRoundState != target)
            {
                Assert.IsTrue(Time.time - startTime < timeout, $"Expected round state {target} but was {CurrentRoundState} after {timeout} seconds.");
                yield return null;
            }
        }

        public static bool IsPlayerSpawned(string ckey)
        {
            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(ckey);
            return player != null && SubSystems.Get<EntitySubSystem>().IsPlayerSpawned(player);
        }

        /// <summary>Joins the Ongoing round as <paramref name="ckey"/> unless already spawned, and waits for the body.</summary>
        public static IEnumerator EmbarkPlayer(string ckey, float timeout = 15f)
        {
            yield return WaitForRoundState(RoundState.Ongoing, timeout);
            if (!IsPlayerSpawned(ckey))
            {
                SpawnLatePlayer(ckey);
            }

            float startTime = Time.time;
            while (!IsPlayerSpawned(ckey))
            {
                Assert.IsTrue(Time.time - startTime < timeout, $"{ckey} was not spawned within {timeout} seconds of embarking.");
                yield return null;
            }
        }

        /// <summary>Stops a running or starting round and waits until it is Stopped. Safe when no round runs.</summary>
        public static IEnumerator StopRound(float timeout = 30f)
        {
            if (!SubSystems.TryGet(out RoundSubSystem rounds) || rounds.CurrentRoundState == RoundState.Stopped)
            {
                yield break;
            }

            ChangeRoundState(false);
            yield return WaitForRoundState(RoundState.Stopped, timeout);
        }

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
