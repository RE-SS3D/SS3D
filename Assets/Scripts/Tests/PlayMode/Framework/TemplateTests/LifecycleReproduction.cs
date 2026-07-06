using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using System.Collections;
using UnityEngine;

namespace SS3D.Tests
{
    /// <summary>
    /// Shared lifecycle test logic for host, client, and dedicated-server fixtures.
    /// </summary>
    public static class LifecycleReproduction
    {
        public static IEnumerator RapidStartThenStopEventuallyReachesStopped()
        {
            ServerHelpers.ChangeRoundState(true);
            ServerHelpers.ChangeRoundState(false);

            yield return RoundLifecycleTestHelpers.WaitUntilStopped(45f);

            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            Assert.AreEqual(0, entitySystem.SpawnedPlayers.Count, "All player entities should be despawned after the round stops.");
        }

        public static IEnumerator DoubleStartCanBeStoppedCleanly()
        {
            ServerHelpers.ChangeRoundState(true);
            ServerHelpers.ChangeRoundState(true);

            yield return new WaitForSeconds(8f);
            ServerHelpers.ChangeRoundState(false);

            yield return RoundLifecycleTestHelpers.WaitUntilStopped(30f);

            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            Assert.AreEqual(0, entitySystem.SpawnedPlayers.Count, "All player entities should be despawned after stopping a doubly-started round.");
        }

        public static IEnumerator RoundCanRestartAfterRapidStartStop()
        {
            ServerHelpers.ChangeRoundState(true);
            ServerHelpers.ChangeRoundState(false);

            yield return RoundLifecycleTestHelpers.WaitUntilStopped(45f);
            yield return new WaitForSeconds(2f);

            ServerHelpers.SetPlayerReadiness("john", true);
            ServerHelpers.ChangeRoundState(true);

            yield return RoundLifecycleTestHelpers.WaitForRoundState(RoundState.Ongoing, 20f);
        }

        public static IEnumerator LateEmbarkRejectedDuringEndingState()
        {
            ServerHelpers.SetPlayerReadiness("john", true);
            ServerHelpers.ChangeRoundState(true);
            yield return RoundLifecycleTestHelpers.WaitForRoundState(RoundState.Ongoing, 20f);

            ServerHelpers.ChangeRoundState(false);
            yield return RoundLifecycleTestHelpers.WaitForRoundState(RoundState.Ending, 10f);

            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            Player player = playerSystem.GetPlayer("john");

            Assert.IsFalse(entitySystem.IsPlayerSpawned(player), "Host should not be spawned before attempting a late embark.");

            ServerHelpers.SpawnLatePlayer("john");
            yield return new WaitForSeconds(2f);

            Assert.IsFalse(
                entitySystem.IsPlayerSpawned(player),
                "Players should not be able to embark while the round is in Ending state.");
        }

        public static IEnumerator GamemodeEndRoundStopsActiveRound()
        {
            ServerHelpers.ChangeRoundState(true);
            yield return RoundLifecycleTestHelpers.WaitForRoundState(RoundState.Ongoing, 20f);

            SubSystems.Get<GamemodeSubSystem>().EndRound();

            yield return RoundLifecycleTestHelpers.WaitUntilStopped(20f);
            Assert.AreEqual(RoundState.Stopped, RoundLifecycleTestHelpers.CurrentState);
        }
    }
}
