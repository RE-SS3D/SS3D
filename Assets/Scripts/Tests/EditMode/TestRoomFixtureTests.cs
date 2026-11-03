using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SS3D.Data.Persistence;
using SS3D.Systems.Roles;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.SpawnPoints;
using SS3D.Systems.Tile.Validation;
using UnityEditor;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// The scenario test room (<c>Testing/fixtures/stations/testroom.json</c>, harness rework D2) passes Map
    /// Check for Extended and still has what the scenarios rely on: a Security-access door between two
    /// rooms, each with its own area, the side one tagged Security.
    /// </summary>
    public sealed class TestRoomFixtureTests
    {
        private const string FixturePath = "Testing/fixtures/stations/testroom.json";
        private const string StartingRolesPath = "Assets/Content/Data/Roles/StartingRoles.asset";

        private PersistenceEnvelope _envelope;
        private SavedTileMap _map;

        [OneTimeSetUp]
        public void Load()
        {
            Assert.IsTrue(File.Exists(FixturePath), $"{FixturePath} exists");
            _envelope = JsonUtility.FromJson<PersistenceEnvelope>(File.ReadAllText(FixturePath));
            _map = JsonUtility.FromJson<SavedTileMap>(Payload("tilemap"));
        }

        [Test]
        public void PassesMapCheckForExtended()
        {
            RolesAvailable roles = AssetDatabase.LoadAssetAtPath<RolesAvailable>(StartingRolesPath);
            Assert.IsNotNull(roles, StartingRolesPath);
            // Crew jobs only, as MapValidationService gets them from RoleSubSystem.Jobs: antagonist roles
            // (Operative) spawn on antagonist markers and are no job-spawn requirement.
            string[] jobs = roles.Roles.Where(role => role.Data != null && !role.Data.IsAntagonist)
                .Select(role => role.Data.Name).ToArray();

            MapValidationReport report = MapValidator.Validate("Extended", jobs, SpawnPoints().ToList(), false, false);

            Assert.IsTrue(report.CanStart, report.Describe("testroom", "Extended", saved: true));
        }

        [Test]
        public void EverySpawnStandsOnPlenum()
        {
            HashSet<Vector2Int> plenum = Cells().Where(cell => cell.names.Contains("Plenum")).Select(cell => cell.position).ToHashSet();
            foreach (SpawnPointRecord spawn in SpawnPoints())
                Assert.IsTrue(plenum.Contains(SpawnPointRegistry.TileKey(spawn.Position)), $"Spawn {spawn.JobName} at {spawn.Position} has a floor");
        }

        [Test]
        public void HasOneSecurityDoorAndTwoAreas()
        {
            Assert.AreEqual(1, Cells().Count(cell => cell.names.Contains("SecurityAirlock")), "one SecurityAirlock");
            string areas = Payload("areas");
            StringAssert.Contains("\"id\":2", areas, "a second area for the side room");
            StringAssert.Contains("\"parentTag\":\"Security\"", areas, "the side room is a Security area, so the door denies non-Security IDs");
        }

        private string Payload(string contributor) =>
            _envelope.chunks.Single(chunk => chunk.contributorId == contributor).payloadJson;

        private IEnumerable<SpawnPointRecord> SpawnPoints() =>
            JsonUtility.FromJson<SavedSpawnPointChunkPayload>(Payload("spawn-points")).records.Select(record => record.ToRecord());

        private IEnumerable<(Vector2Int position, HashSet<string> names)> Cells() =>
            _map.savedChunkList.SelectMany(chunk => chunk.savedTiles.Where(tile => tile != null).Select(tile => (
                new Vector2Int(Mathf.RoundToInt(chunk.originPosition.x) + tile.Location.x, Mathf.RoundToInt(chunk.originPosition.z) + tile.Location.y),
                tile.GetPlacedObjects().Select(placed => placed.tileObjectSOName).ToHashSet())));
    }
}
