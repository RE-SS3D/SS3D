using SS3D.Core;
using SS3D.Data.Persistence;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using System;
using UnityEngine;

namespace SS3D.Systems.Persistence
{
    public sealed class TileMapPersistenceContributor : IPersistenceContributor
    {
        public const string ContributorIdValue = "tilemap";

        private readonly Func<TileSubSystem> _tileSubSystemProvider;

        public TileMapPersistenceContributor(Func<TileSubSystem> tileSubSystemProvider)
        {
            _tileSubSystemProvider = tileSubSystemProvider;
        }

        public string ContributorId => ContributorIdValue;

        public PersistenceLayer Layer => PersistenceLayer.StationTemplate;

        public int LoadOrder => 0;

        public object Capture()
        {
            TileSubSystem tileSubSystem = _tileSubSystemProvider();
            SavedTileMap saved = tileSubSystem.CurrentMap.Save();
            saved.savedAreas = null;
            return saved;
        }

        public void Restore(object data, PersistenceContext context)
        {
            if (data is not SavedTileMap savedTileMap)
            {
                return;
            }

            TileSubSystem tileSubSystem = _tileSubSystemProvider();
            tileSubSystem.CurrentMap.Load(savedTileMap, invokeMapLoadedEvent: false);
        }
    }
}
