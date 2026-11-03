using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FishNet;
using FishNet.Object;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Persistence;
using SS3D.Networking;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace SS3D.Tests
{
    /// <summary>
    /// Base for tier 2 scenarios (2026-11_testing-philosophy D1–D4). The first scenario of a run boots a Play
    /// Mode host into the test room fixture with a body, and later fixtures reuse that boot; every scenario then gets a fresh body, and whatever
    /// items or entities it spawned are despawned afterwards. Logged errors fail the scenario
    /// (<c>LogAssert</c> stays at its default); declare intended ones with <c>LogAssert.Expect</c>.
    /// A scenario that changes the room sets <see cref="ChangesRoom"/>, and the next scenario reboots it.
    /// Every load checks the rooms hold standard station air (<see cref="ScenarioAtmos"/>); a scenario that
    /// changes the air without declaring it fails, and breathing CO₂ past a threshold triggers a reload.
    /// </summary>
    public abstract class ScenarioFixture : PlayModeTest
    {
        /// <summary>Checked-in station template the scenarios run in, relative to the project root.</summary>
        public const string RoomFixturePath = "Testing/fixtures/stations/testroom.json";

        /// <summary>Name the room is copied under for the run; deleted again afterwards.</summary>
        public const string RoomStation = "scenario-testroom";

        private static bool _roomReady;
        private bool _setUpDone;
        private readonly HashSet<int> _objectsBeforeScenario = new();
        private readonly Stopwatch _scenarioClock = new();

        /// <summary>The host's current body, fresh for each scenario.</summary>
        protected Entity Body { get; private set; }

        protected Player Host => SubSystems.Get<PlayerSubSystem>().GetPlayer(ServerHelpers.LocalCkey);

        /// <summary>The host's entity right now: <see cref="Body"/>, its ghost after a death, or a respawned body.</summary>
        protected Entity HostEntity => SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Host);

        /// <summary>Set in a scenario that leaves the room changed (atmos, doors); the next scenario reboots it.</summary>
        protected bool ChangesRoom { get; set; }

        /// <summary>Runs host console commands and returns their answers.</summary>
        protected ScenarioConsole Console { get; } = new();

        protected override bool UseMockUpInputs() => false;

        [UnitySetUp]
        public IEnumerator ScenarioSetUp()
        {
            ChangesRoom = false;
            Body = null;
            _setUpDone = false;
            if (!_roomReady || !RoundRunning())
            {
                Stopwatch boot = Stopwatch.StartNew();
                yield return BootIntoRoom();
                Debug.Log($"[Scenario] booted into {RoomStation} in {boot.Elapsed.TotalSeconds:F1}s");
                ScenarioAtmos.AssertStationAir("after loading the room");
                _roomReady = true;
            }

            Stopwatch setUp = Stopwatch.StartNew();
            yield return GiveFreshBody();
            Debug.Log($"[Scenario] fresh body in {setUp.Elapsed.TotalSeconds:F1}s");
            SnapshotSpawnedObjects();
            _setUpDone = true;
            _scenarioClock.Restart();
        }

        [UnityTearDown]
        public IEnumerator ScenarioTearDown()
        {
            if (!_setUpDone)
            {
                _roomReady = false; // set-up failed part-way; boot again next time
                yield break;
            }

            Debug.Log($"[Scenario] {TestContext.CurrentContext.Test.Name} took {_scenarioClock.Elapsed.TotalSeconds:F1}s");
            DespawnScenarioObjects();
            if (!ChangesRoom && ScenarioAtmos.CheckUnchanged(TestContext.CurrentContext.Test.Name))
            {
                Debug.Log($"[Scenario] CO₂ from breathing passed the threshold; reloading the room: {ScenarioAtmos.Read()}");
                ChangesRoom = true;
            }

            if (ChangesRoom)
            {
                yield return ServerHelpers.StopRound();
                _roomReady = false;
            }

            yield return null;
        }

        /// <summary>
        /// Leaves the host and round running, so the next scenario fixture in the run reuses the boot.
        /// The loaded round no longer needs the copied template; a reload copies it again.
        /// </summary>
        [OneTimeTearDown]
        public void ScenarioOneTimeTearDown() => DeleteRoomStation();

        /// <summary>
        /// Mid-scenario: stops the round, loads the room again in a new round and hands out a fresh body,
        /// as the next scenario would after <see cref="ChangesRoom"/>. Fails unless the air is back to station air.
        /// </summary>
        protected IEnumerator ReloadRoom()
        {
            DespawnScenarioObjects();
            yield return ServerHelpers.StopRound();
            yield return BootIntoRoom();
            ScenarioAtmos.AssertStationAir("after reloading the room");
            yield return GiveFreshBody();
            SnapshotSpawnedObjects();
        }

        private static bool RoundRunning() =>
            SubSystems.TryGet(out RoundSubSystem rounds) && rounds.CurrentRoundState == RoundState.Ongoing;

        private IEnumerator BootIntoRoom()
        {
            if (!SubSystems.TryGet(out RoundSubSystem _) || InstanceFinder.NetworkManager == null
                || !InstanceFinder.ServerManager.Started)
            {
                yield return LoadAndSetInLobby(NetworkType.Host);
            }
            else if (ServerHelpers.CurrentRoundState != RoundState.Stopped)
            {
                yield return ServerHelpers.StopRound();
            }

            CopyRoomStation();
            RoundSubSystem rounds = SubSystems.Get<RoundSubSystem>();
            ServerSettings settings = rounds.SettingsForServer;
            settings.Map = RoomStation;
            settings.Mode = "Extended";
            settings.RoundLengthMinutes = 120;
            settings.AutoStart = false;
            settings.AllowLateJoin = true;
            rounds.ApplyServerSettingsForTesting(settings);

            ServerHelpers.SetPlayerReadiness(ServerHelpers.LocalCkey, true);
            ServerHelpers.ChangeRoundState(true);
            yield return ServerHelpers.WaitForRoundState(RoundState.WarmingUp, 60f);
            rounds.CmdRoundAdminAction(RoundAdminAction.ForceStart);
            yield return ServerHelpers.EmbarkPlayer(ServerHelpers.LocalCkey, 20f);
        }

        /// <summary>Waits until <paramref name="condition"/> holds; fails after <paramref name="timeout"/> seconds.</summary>
        protected static IEnumerator WaitFor(System.Func<bool> condition, string what, float timeout = 5f)
        {
            float start = Time.time;
            while (!condition())
            {
                Assert.Less(Time.time - start, timeout, $"Within {timeout}s: {what}");
                yield return null;
            }
        }

        /// <summary>Waits <paramref name="seconds"/> of game time, for checks that something does not happen.</summary>
        protected static IEnumerator Hold(float seconds)
        {
            float start = Time.time;
            while (Time.time - start < seconds)
                yield return null;
        }

        /// <summary>
        /// Puts the body into critical state at once with one toxin-loaded health tick. No console command
        /// does this; scenarios that guard the atmos → health path use real gas instead.
        /// </summary>
        protected static void MakeCritical(Entity body)
        {
            HumanHealthController health = body.GetComponent<HumanHealthController>();
            health.TickHealth(toxinIntake: HealthConstants.CriticalToxinConcentration + 5f);
            Assert.AreEqual(HealthState.Critical, health.Snapshot.State, "A toxin overdose makes the body critical");
        }

        /// <summary>Fully heals the host's body through the <c>heal</c> console command.</summary>
        protected IEnumerator HealFully() =>
            Console.Expect($"heal {ServerHelpers.LocalCkey} all 999 999", "Player fully healed");

        /// <summary>Deletes whatever the body holds; the job loadout fills a hand (Security's toolbox).</summary>
        protected static void EmptyHands(Entity body)
        {
            foreach (Hand hand in body.GetComponentInParent<HumanInventory>().Hands.PlayerHands)
                hand.ItemInHand?.Delete();
        }

        /// <summary>Moves a body to <paramref name="position"/> past its CharacterController, as an admin teleport would.</summary>
        protected static void Teleport(Entity body, Vector3 position)
        {
            CharacterController controller = body.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            body.transform.position = position;
            if (controller != null)
                controller.enabled = true;
        }

        private IEnumerator GiveFreshBody()
        {
            // On a host, server-side lookups see a body at once, but the host's client spawns it a few
            // ticks later. Despawning it before then leaves the client processing a spawn (with nested
            // objects) for an object that is already gone, which FishNet logs as errors.
            Entity current = SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(Host);
            Assert.IsNotNull(current, "The host has a body to replace");
            yield return WaitForClientSpawn(current, "The host's client spawned its current body");

            Mind mind = SubSystems.Get<EntitySubSystem>().DespawnBodyForTesting(Host);
            Entity body = SubSystems.Get<EntitySubSystem>().SpawnBodyForTesting(Host, mind);
            Assert.IsNotNull(body, "The host got a fresh body");
            yield return WaitForClientSpawn(body, "The host's client spawned the fresh body");

            EmptyHands(body);
            yield return null;

            Body = body;
        }

        private static IEnumerator WaitForClientSpawn(Entity entity, string what)
        {
            float start = Time.time;
            while (!entity.NetworkObject.ClientInitialized)
            {
                Assert.Less(Time.time - start, 10f, what);
                yield return null;
            }
        }

        private void SnapshotSpawnedObjects()
        {
            _objectsBeforeScenario.Clear();
            foreach (int id in InstanceFinder.ServerManager.Objects.Spawned.Keys)
                _objectsBeforeScenario.Add(id);
        }

        /// <summary>
        /// Despawns items and entities that appeared during the scenario (D3). The host's current entity (body,
        /// ghost or respawned body) stays for the next swap; a corpse the scenario left behind goes.
        /// </summary>
        private void DespawnScenarioObjects()
        {
            Entity current = HostEntity;
            List<NetworkObject> spawned = InstanceFinder.ServerManager.Objects.Spawned
                .Where(pair => !_objectsBeforeScenario.Contains(pair.Key)).Select(pair => pair.Value).ToList();
            foreach (NetworkObject networkObject in spawned)
            {
                if (networkObject == null || !networkObject.IsSpawned)
                    continue;
                // A respawned body's loadout counts as spawned by the scenario too; some of it sits outside any container.
                if (networkObject.TryGetComponent(out Item item))
                {
                    if (item.Container != null)
                        item.Delete();
                    else
                        InstanceFinder.ServerManager.Despawn(networkObject);
                }
                else if (networkObject.TryGetComponent(out Entity entity) && entity != Body && entity != current)
                    InstanceFinder.ServerManager.Despawn(networkObject);
            }

            if (Body != null && Body != current && Body.IsSpawned)
                InstanceFinder.ServerManager.Despawn(Body.NetworkObject);
        }

        private static string RoomStationFile() =>
            Paths.GetPath(GamePaths.Data, true) + PersistencePaths.StationTemplates + "/" + RoomStation + ".json";

        private static void CopyRoomStation()
        {
            string source = Path.GetFullPath(RoomFixturePath);
            Assert.IsTrue(File.Exists(source), $"Test room fixture exists at {RoomFixturePath}");
            string target = RoomStationFile();
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source, target, overwrite: true);
        }

        private static void DeleteRoomStation()
        {
            string target = RoomStationFile();
            if (File.Exists(target))
                File.Delete(target);
        }
    }
}
