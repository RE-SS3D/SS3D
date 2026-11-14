using NUnit.Framework;
using SS3D.Systems.Persistence;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Commands;
using SS3D.Systems.Tile.SpawnPoints;
using UnityEngine;

namespace EditorTests
{
    public class SpawnPointAuthoringTests
    {
        [Test]
        public void SpawnPointRegistry_PlaceReplaceAndClear_RoundTripsPerTile()
        {
            var registry = new SpawnPointRegistry();
            var assistant = SpawnPointRecord.ForJob("Assistant", new Vector3(1f, 0f, 2f), Direction.East);
            Assert.IsTrue(registry.TryPlace(assistant));
            Assert.AreEqual(1, registry.Count);
            Assert.IsTrue(registry.TryGetAt(new Vector3(1f, 0f, 2f), out SpawnPointRecord loaded));
            Assert.AreEqual(SpawnPointKind.Job, loaded.Kind);
            Assert.AreEqual("Assistant", loaded.JobName);
            Assert.AreEqual(Direction.East, loaded.Direction);

            var security = SpawnPointRecord.ForJob("Security", new Vector3(1.2f, 0f, 2.1f), Direction.North);
            Assert.IsTrue(registry.TryPlace(security));
            Assert.AreEqual(1, registry.Count);
            Assert.IsTrue(registry.TryGetAt(new Vector3(1f, 0f, 2f), out SpawnPointRecord replaced));
            Assert.AreEqual("Security", replaced.JobName);

            Assert.IsTrue(registry.TryClearAt(new Vector3(1f, 0f, 2f), out SpawnPointRecord cleared));
            Assert.AreEqual("Security", cleared.JobName);
            Assert.AreEqual(0, registry.Count);
        }

        [Test]
        public void SavedSpawnPointChunk_JsonRoundTrip_PreservesJobAndAntag()
        {
            var payload = new SavedSpawnPointChunkPayload
            {
                records = new[]
                {
                    SavedSpawnPointRecord.FromRecord(
                        SpawnPointRecord.ForJob("Assistant", new Vector3(3f, 0f, 4f), Direction.South)),
                    SavedSpawnPointRecord.FromRecord(
                        SpawnPointRecord.ForAntagonist(
                            AntagonistSpawnCategory.NuclearOperatives,
                            new Vector3(5f, 0f, 6f),
                            Direction.West)),
                },
            };

            string json = JsonUtility.ToJson(payload);
            SavedSpawnPointChunkPayload deserialized = JsonUtility.FromJson<SavedSpawnPointChunkPayload>(json);
            Assert.AreEqual(2, deserialized.records.Length);

            var registry = new SpawnPointRegistry();
            registry.ReplaceAll(new[]
            {
                deserialized.records[0].ToRecord(),
                deserialized.records[1].ToRecord(),
            });

            Assert.AreEqual(2, registry.Count);
            Assert.IsTrue(registry.TryGetAt(new Vector3(3f, 0f, 4f), out SpawnPointRecord job));
            Assert.AreEqual("Assistant", job.JobName);
            Assert.IsTrue(registry.TryGetAt(new Vector3(5f, 0f, 6f), out SpawnPointRecord antag));
            Assert.AreEqual(SpawnPointKind.Antagonist, antag.Kind);
            Assert.AreEqual(AntagonistSpawnCategory.NuclearOperatives, antag.AntagonistCategory);
        }

        [Test]
        public void PlaceAndClearSpawnPointCommands_AreInvertible()
        {
            var registry = new SpawnPointRegistry();
            var ctx = new MapEditorCommandContext(null, null, null)
            {
                SpawnPoints = registry,
            };

            SpawnPointRecord record = SpawnPointRecord.ForJob("Security", new Vector3(2f, 0f, 2f), Direction.North);
            var place = new PlaceSpawnPointCommand(record, hadPrevious: false, previous: default);
            place.Apply(ctx);
            Assert.AreEqual(1, registry.Count);

            place.Revert(ctx);
            Assert.AreEqual(0, registry.Count);

            place.Apply(ctx);
            bool had = registry.TryGetAt(record.Position, out SpawnPointRecord previous);
            var clear = new ClearSpawnPointCommand(record.Position, had, previous);
            clear.Apply(ctx);
            Assert.AreEqual(0, registry.Count);

            clear.Revert(ctx);
            Assert.AreEqual(1, registry.Count);
            Assert.IsTrue(registry.TryGetAt(record.Position, out SpawnPointRecord restored));
            Assert.AreEqual("Security", restored.JobName);
        }

        [Test]
        public void MapEditorSpawnCatalog_EncodesAndDecodesJobAndAntagKeys()
        {
            string jobKey = MapEditorSpawnCatalog.EncodeJob("Assistant");
            Assert.IsTrue(MapEditorSpawnCatalog.TryDecode(
                jobKey, out SpawnPointKind jobKind, out string jobName, out _));
            Assert.AreEqual(SpawnPointKind.Job, jobKind);
            Assert.AreEqual("Assistant", jobName);

            string antagKey = MapEditorSpawnCatalog.EncodeAntagonist(AntagonistSpawnCategory.Traitor);
            Assert.IsTrue(MapEditorSpawnCatalog.TryDecode(
                antagKey, out SpawnPointKind antagKind, out _, out AntagonistSpawnCategory category));
            Assert.AreEqual(SpawnPointKind.Antagonist, antagKind);
            Assert.AreEqual(AntagonistSpawnCategory.Traitor, category);
        }

        [Test]
        public void ContributorLoadOrder_SpawnPointsLoadAfterAreas()
        {
            var spawn = new SpawnPointPersistenceContributor(() => null);
            var areas = new AreaPersistenceContributor(() => null, () => null);
            Assert.Greater(spawn.LoadOrder, areas.LoadOrder);
            Assert.AreEqual("spawn-points", spawn.ContributorId);
        }

        // Bug report 3: a Doctor spawned on the first marker (Security) instead of a Doctor marker.
        [Test]
        public void TryGetJobSpawn_PicksTheJobsMarker_NotTheFirstMarker()
        {
            var registry = new SpawnPointRegistry();
            registry.TryPlace(SpawnPointRecord.ForJob("Security", new Vector3(-25f, 0f, 6f), Direction.North));
            registry.TryPlace(SpawnPointRecord.ForAntagonist(
                AntagonistSpawnCategory.NuclearOperatives, new Vector3(-8f, 0f, -59f), Direction.North));
            registry.TryPlace(SpawnPointRecord.ForJob("Doctor", new Vector3(21f, 0f, -6f), Direction.North));
            registry.TryPlace(SpawnPointRecord.ForJob("Doctor", new Vector3(21f, 0f, -3f), Direction.North));

            Assert.IsTrue(registry.TryGetJobSpawn("Doctor", 0, out Vector3 first));
            Assert.AreEqual(new Vector3(21f, 0f, -6f), first);
            Assert.IsTrue(registry.TryGetJobSpawn("Doctor", 1, out Vector3 second));
            Assert.AreEqual(new Vector3(21f, 0f, -3f), second);
            Assert.IsTrue(registry.TryGetJobSpawn("Doctor", 2, out Vector3 wrapped));
            Assert.AreEqual(first, wrapped);
        }

        [Test]
        public void TryGetJobSpawn_JobWithoutMarker_ReturnsFalse()
        {
            var registry = new SpawnPointRegistry();
            registry.TryPlace(SpawnPointRecord.ForJob("Security", new Vector3(-25f, 0f, 6f), Direction.North));

            Assert.IsFalse(registry.TryGetJobSpawn("Doctor", 0, out _));
            Assert.IsFalse(registry.TryGetJobSpawn(null, 0, out _));
        }
    }
}
