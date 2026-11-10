using System.Collections;
using System.IO;
using System.Linq;
using FishNet;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Persistence;
using SS3D.Systems.Entities;
using SS3D.Systems.Rounds;
using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: a saved map that fails the round-start check refuses a normal start, and Edit Map Anyway still starts a round on it (as Extended, with the body on the deck) so its spawns can be added in the Map Editor (creative-mode §8; a tester's map saved without spawns could never be opened again) · Not cheaper because: needs the real start sequence, template load, validation and spawn on a live host
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.MapEditor)]
    public class MapRepairScenarios : ScenarioFixture
    {
        private const string NoSpawnsStation = "scenario-no-spawns";

        private static string NoSpawnsFile() =>
            Paths.GetPath(GamePaths.Data, true) + PersistencePaths.StationTemplates + "/" + NoSpawnsStation + ".json";

        [UnityTearDown]
        public IEnumerator DeleteNoSpawnsStation()
        {
            if (File.Exists(NoSpawnsFile()))
                File.Delete(NoSpawnsFile());
            yield break;
        }

        [UnityTest]
        public IEnumerator MapWithoutSpawns_RefusesAStart_ThenEditMapAnywayStartsItAsExtended()
        {
            ChangesRoom = true;
            PersistenceEnvelope envelope = JsonUtility.FromJson<PersistenceEnvelope>(File.ReadAllText(Path.GetFullPath(RoomFixturePath)));
            PersistenceChunk spawns = envelope.chunks.Single(chunk => chunk.contributorId == "spawn-points");
            spawns.payloadJson = "{\"records\":[]}";
            File.WriteAllText(NoSpawnsFile(), JsonUtility.ToJson(envelope));

            yield return ServerHelpers.StopRound();
            RoundSubSystem rounds = SubSystems.Get<RoundSubSystem>();
            ServerSettings settings = rounds.SettingsForServer;
            settings.Map = NoSpawnsStation;
            settings.Mode = "Secret";
            rounds.ApplyServerSettingsForTesting(settings);
            ServerHelpers.SetPlayerReadiness(ServerHelpers.LocalCkey, true);

            Assert.IsFalse(rounds.ServerRoundAdminAction(RoundAdminAction.EditMapAnyway, InstanceFinder.ClientManager.Connection),
                "Edit Map Anyway is only offered once a start found the map unplayable");
            ServerHelpers.ChangeRoundState(true);
            yield return WaitFor(() => rounds.CurrentStationLoadState == StationLoadState.Unplayable,
                "the start loads the map and finds it unplayable", 30f);
            Assert.AreEqual(RoundState.Stopped, rounds.CurrentRoundState, "An unplayable map never reaches the countdown");

            Assert.IsTrue(rounds.ServerRoundAdminAction(RoundAdminAction.EditMapAnyway, InstanceFinder.ClientManager.Connection),
                "Edit Map Anyway is accepted for the unplayable map");
            yield return ServerHelpers.WaitForRoundState(RoundState.WarmingUp, 30f);
            Assert.IsTrue(rounds.IsExtendedRound, "The repair round runs as Extended, without the Nuke Ops it can't support");
            rounds.CmdRoundAdminAction(RoundAdminAction.ForceStart);
            yield return ServerHelpers.EmbarkPlayer(ServerHelpers.LocalCkey, 20f);

            Entity body = HostEntity;
            Assert.IsNotNull(body, "The admin gets a body to open the Map Editor with");
            TileSubSystem tiles = SubSystems.Get<TileSubSystem>();
            Assert.IsTrue(tiles.QueryService.TryGetOccupancy(tiles.QueryService.WorldToTile(body.transform.position, tiles.CurrentMap.MapId),
                out TileOccupancy under) && under.HasPlenum && !under.HasWall && !under.IsDoor,
                $"With no spawn markers the body starts on walkable deck, not at {body.transform.position}");
        }
    }
}
