using SS3D.Data.Persistence;
using SS3D.Systems.Tile;
using System;
using UnityEngine;

namespace SS3D.Systems.Persistence
{
    public static class LegacyTileMapMigrator
    {
        public static bool TryWrapLegacyJson(string json, string templateName, out PersistenceEnvelope envelope)
        {
            envelope = null;

            if (string.IsNullOrEmpty(json) || EnvelopePersistenceStore.IsEnvelopeJson(json))
            {
                return false;
            }

            SavedTileMap legacy = JsonUtility.FromJson<SavedTileMap>(json);
            if (legacy?.savedChunkList == null)
            {
                return false;
            }

            envelope = BuildEnvelope(legacy, templateName);
            return true;
        }

        public static PersistenceEnvelope BuildEnvelope(SavedTileMap legacy, string templateName)
        {
            if (string.IsNullOrWhiteSpace(legacy.mapName))
            {
                legacy.mapName = templateName;
            }

            SavedTileMap tilePayload = new()
            {
                mapName = legacy.mapName,
                savedChunkList = legacy.savedChunkList,
                savedItemList = legacy.savedItemList,
                savedAreas = null,
            };

            var envelope = new PersistenceEnvelope
            {
                schemaVersion = PersistenceEnvelope.CurrentSchemaVersion,
                envelopeType = PersistenceEnvelope.StationTemplateType,
                createdAt = DateTime.UtcNow.ToString("o"),
                gameVersion = Application.version,
            };

            envelope.chunks.Add(new PersistenceChunk
            {
                contributorId = TileMapPersistenceContributor.ContributorIdValue,
                payloadJson = JsonUtility.ToJson(tilePayload),
            });

            if (legacy.savedAreas is { Length: > 0 })
            {
                envelope.chunks.Add(new PersistenceChunk
                {
                    contributorId = AreaPersistenceContributor.ContributorIdValue,
                    payloadJson = JsonUtility.ToJson(new SavedAreaChunkPayload { records = legacy.savedAreas }),
                });
            }

            return envelope;
        }
    }
}
