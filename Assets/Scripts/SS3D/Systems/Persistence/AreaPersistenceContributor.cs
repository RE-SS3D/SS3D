using SS3D.Core;
using SS3D.Data.Persistence;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using System;
using UnityEngine;

namespace SS3D.Systems.Persistence
{
    public sealed class AreaPersistenceContributor : IPersistenceContributor
    {
        public const string ContributorIdValue = "areas";

        private readonly Func<AreaSubSystem> _areaSubSystemProvider;
        private readonly Func<TileSubSystem> _tileSubSystemProvider;

        public AreaPersistenceContributor(
            Func<AreaSubSystem> areaSubSystemProvider,
            Func<TileSubSystem> tileSubSystemProvider)
        {
            _areaSubSystemProvider = areaSubSystemProvider;
            _tileSubSystemProvider = tileSubSystemProvider;
        }

        public string ContributorId => ContributorIdValue;

        public PersistenceLayer Layer => PersistenceLayer.StationTemplate;

        public int LoadOrder => 100;

        public object Capture()
        {
            AreaSubSystem areaSubSystem = _areaSubSystemProvider();
            return new SavedAreaChunkPayload
            {
                records = areaSubSystem.BuildSavedAreaRecords(),
            };
        }

        public void Restore(object data, PersistenceContext context)
        {
            SavedAreaRecord[] records = data switch
            {
                SavedAreaChunkPayload payload => payload.records,
                SavedAreaRecord[] directRecords => directRecords,
                _ => null,
            };

            if (records == null || records.Length == 0)
            {
                return;
            }

            TileSubSystem tileSubSystem = _tileSubSystemProvider();
            tileSubSystem.CurrentMap.SetLoadedAreaRecords(records);

            AreaSubSystem areaSubSystem = _areaSubSystemProvider();
            areaSubSystem.BeginTemplateRestore(records);
            areaSubSystem.RestoreFromSave(records);
        }
    }
}
