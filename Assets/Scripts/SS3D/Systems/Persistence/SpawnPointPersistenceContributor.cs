using SS3D.Data.Persistence;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.SpawnPoints;
using System;
using UnityEngine;

namespace SS3D.Systems.Persistence
{
    public sealed class SpawnPointPersistenceContributor : IPersistenceContributor
    {
        public const string ContributorIdValue = "spawn-points";

        private readonly Func<TileSubSystem> _tileSubSystemProvider;

        public SpawnPointPersistenceContributor(Func<TileSubSystem> tileSubSystemProvider)
        {
            _tileSubSystemProvider = tileSubSystemProvider;
        }

        public string ContributorId => ContributorIdValue;

        public PersistenceLayer Layer => PersistenceLayer.StationTemplate;

        public int LoadOrder => 110;

        public object DeserializePayload(string payloadJson) => JsonUtility.FromJson<SavedSpawnPointChunkPayload>(payloadJson);

        public object Capture()
        {
            TileSubSystem tileSubSystem = _tileSubSystemProvider();
            SpawnPointRegistry registry = tileSubSystem?.SpawnPoints;
            if (registry == null || registry.Count == 0)
            {
                return new SavedSpawnPointChunkPayload
                {
                    records = Array.Empty<SavedSpawnPointRecord>(),
                };
            }

            var saved = new SavedSpawnPointRecord[registry.Count];
            for (int i = 0; i < registry.Count; i++)
                saved[i] = SavedSpawnPointRecord.FromRecord(registry.Records[i]);

            return new SavedSpawnPointChunkPayload { records = saved };
        }

        public void Restore(object data, PersistenceContext context)
        {
            SavedSpawnPointRecord[] records = data switch
            {
                SavedSpawnPointChunkPayload payload => payload.records,
                SavedSpawnPointRecord[] direct => direct,
                _ => null,
            };

            TileSubSystem tileSubSystem = _tileSubSystemProvider();
            if (tileSubSystem?.SpawnPoints == null)
                return;

            if (records == null || records.Length == 0)
            {
                tileSubSystem.SpawnPoints.Clear();
                return;
            }

            var runtime = new SpawnPointRecord[records.Length];
            for (int i = 0; i < records.Length; i++)
                runtime[i] = records[i].ToRecord();

            tileSubSystem.SpawnPoints.ReplaceAll(runtime);
        }
    }
}
