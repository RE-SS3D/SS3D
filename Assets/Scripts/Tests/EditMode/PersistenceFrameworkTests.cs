using NUnit.Framework;
using SS3D.Data.Persistence;
using SS3D.Systems.Persistence;
using SS3D.Systems.Tile;
using System.Linq;
using UnityEngine;

namespace EditorTests
{
    public class PersistenceFrameworkTests
    {
        [Test]
        public void LegacyTileMapMigrator_WrapsFlatJsonIntoEnvelopeChunks()
        {
            var legacy = new SavedTileMap
            {
                mapName = "LegacyStation",
                savedChunkList = new SavedTileChunk[0],
                savedItemList = new SavedPlacedItemObject[0],
                savedAreas = new[]
                {
                    new SavedAreaRecord
                    {
                        id = 1,
                        displayName = "Engineering",
                        parentTag = "eng",
                        apcWorldPosition = new Vector3(2, 0, 2),
                        lightingSwitchOn = false,
                    },
                },
            };

            string legacyJson = JsonUtility.ToJson(legacy);
            Assert.IsTrue(LegacyTileMapMigrator.TryWrapLegacyJson(legacyJson, "LegacyStation", out PersistenceEnvelope envelope));
            Assert.AreEqual(PersistenceEnvelope.StationTemplateType, envelope.envelopeType);
            Assert.AreEqual(2, envelope.chunks.Count);
            Assert.AreEqual(TileMapPersistenceContributor.ContributorIdValue, envelope.chunks[0].contributorId);
            Assert.AreEqual(AreaPersistenceContributor.ContributorIdValue, envelope.chunks[1].contributorId);

            SavedTileMap tilePayload = JsonUtility.FromJson<SavedTileMap>(envelope.chunks[0].payloadJson);
            SavedAreaChunkPayload areaPayload = JsonUtility.FromJson<SavedAreaChunkPayload>(envelope.chunks[1].payloadJson);

            Assert.IsNull(tilePayload.savedAreas);
            Assert.AreEqual("Engineering", areaPayload.records[0].displayName);
            Assert.IsFalse(areaPayload.records[0].lightingSwitchOn);
        }

        [Test]
        public void PersistenceEnvelope_RoundTripsThroughJsonUtility()
        {
            var envelope = new PersistenceEnvelope
            {
                schemaVersion = PersistenceEnvelope.CurrentSchemaVersion,
                envelopeType = PersistenceEnvelope.StationTemplateType,
                createdAt = "2026-07-14T12:00:00Z",
                gameVersion = "test",
            };

            envelope.chunks.Add(new PersistenceChunk
            {
                contributorId = TileMapPersistenceContributor.ContributorIdValue,
                payloadJson = JsonUtility.ToJson(new SavedTileMap { mapName = "RoundTrip" }),
            });

            string json = JsonUtility.ToJson(envelope);
            PersistenceEnvelope loaded = JsonUtility.FromJson<PersistenceEnvelope>(json);

            Assert.AreEqual(PersistenceEnvelope.CurrentSchemaVersion, loaded.schemaVersion);
            Assert.AreEqual(1, loaded.chunks.Count);
            Assert.AreEqual("RoundTrip", JsonUtility.FromJson<SavedTileMap>(loaded.chunks[0].payloadJson).mapName);
        }

        [Test]
        public void ContributorLoadOrder_TilemapLoadsBeforeAreas()
        {
            var contributors = new IPersistenceContributor[]
            {
                new AreaPersistenceContributor(() => null, () => null),
                new TileMapPersistenceContributor(() => null),
            };

            int[] order = contributors
                .OrderBy(contributor => contributor.LoadOrder)
                .Select(contributor => contributor.LoadOrder)
                .ToArray();

            Assert.AreEqual(new[] { 0, 100 }, order);
        }
    }
}
