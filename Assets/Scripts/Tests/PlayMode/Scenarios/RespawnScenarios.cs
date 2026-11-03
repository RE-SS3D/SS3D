using System.Collections;
using FishNet;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Rounds;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: death leaves a corpse and a ghost, the cooldown refuses an early respawn, and a respawn gives a controllable new body and removes only the previous corpse, twice in a row (death-cloning-respawn §6, EntitySubSystem.TryRespawn; the old respawn-cleanup pilot) · Not cheaper because: needs a live round, the ghost swap and the spawn path on a networked host
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Respawn)]
    public class RespawnScenarios : ScenarioFixture
    {
        private ServerSettings _settingsBefore;

        [UnitySetUp]
        public IEnumerator SaveSettings()
        {
            _settingsBefore = Rounds.SettingsForServer;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator RestoreSettings()
        {
            Rounds.ApplyServerSettingsForTesting(_settingsBefore);
            yield break;
        }

        [UnityTest]
        public IEnumerator Kill_Ghost_Respawn_NewBodyIsControllable()
        {
            for (int life = 1; life <= 2; life++)
            {
                Entity body = HostEntity;
                SetRespawnRules(cooldownSeconds: 30);
                yield return Console.Expect($"kill {ServerHelpers.LocalCkey}", "Player killed");
                yield return WaitFor(IsGhost, $"life {life}: the dead player becomes a ghost");

                Assert.IsFalse(SubSystems.Get<EntitySubSystem>().TryRespawn(InstanceFinder.ClientManager.Connection, out string refusal),
                    $"Life {life}: the cooldown refuses an early respawn");
                StringAssert.StartsWith("You can respawn in", refusal);
                Assert.IsTrue(body != null && body.IsSpawned, $"Life {life}: a refused respawn keeps the corpse");

                SetRespawnRules(cooldownSeconds: 0);
                Assert.IsTrue(SubSystems.Get<EntitySubSystem>().TryRespawn(InstanceFinder.ClientManager.Connection, out refusal),
                    $"Life {life}: respawn succeeds once the cooldown is gone ({refusal})");
                yield return WaitFor(() => HostEntity != null && !IsGhost() && HostEntity.NetworkObject.ClientInitialized,
                    $"life {life}: the host's client has the new body");
                yield return WaitFor(() => body == null, $"life {life}: respawn removes the previous corpse");
                Assert.AreEqual(1, Object.FindObjectsByType<Human>(FindObjectsSortMode.None).Length,
                    $"Life {life}: one living Human and no corpses");

                Entity fresh = HostEntity;
                Assert.IsTrue(fresh.IsOwner, $"Life {life}: the host owns the new body");
                EmptyHands(fresh);
                yield return null;
                yield return Console.Expect("spawninhand MedicalPatch", "spawned in hand");
                yield return Console.Run("spawninhand MedicalPatch");
                StringAssert.Contains("not empty", Console.LastAnswer, $"Life {life}: the new body's hand holds what it was given");
            }
        }

        private static RoundSubSystem Rounds => SubSystems.Get<RoundSubSystem>();

        private bool IsGhost() => HostEntity != null && HostEntity.GetComponent<HumanoidGhostController>() != null;

        private static void SetRespawnRules(int cooldownSeconds)
        {
            ServerSettings settings = Rounds.SettingsForServer;
            settings.RespawnEnabled = true;
            settings.RemoveCorpseOnRespawn = true;
            settings.RespawnCooldownSeconds = cooldownSeconds;
            Rounds.ApplyServerSettingsForTesting(settings);
        }
    }
}
