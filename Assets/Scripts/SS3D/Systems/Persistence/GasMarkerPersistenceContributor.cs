using SS3D.Data.Persistence;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.GasMarkers;
using System;
using System.Linq;

namespace SS3D.Systems.Persistence
{
    public sealed class GasMarkerPersistenceContributor : IPersistenceContributor
    {
        public const string ContributorIdValue = "gas-markers";

        private readonly Func<TileSubSystem> _tileSubSystemProvider;

        public GasMarkerPersistenceContributor(Func<TileSubSystem> tileSubSystemProvider)
        {
            _tileSubSystemProvider = tileSubSystemProvider;
        }

        public string ContributorId => ContributorIdValue;

        public PersistenceLayer Layer => PersistenceLayer.StationTemplate;

        public int LoadOrder => 115;

        public object Capture() => new SavedGasMarkerChunkPayload
        {
            records = _tileSubSystemProvider()?.GasMarkers?.Records.ToArray() ?? Array.Empty<GasMarkerRecord>(),
        };

        public void Restore(object data, PersistenceContext context)
        {
            if (data is SavedGasMarkerChunkPayload payload)
                _tileSubSystemProvider()?.GasMarkers?.ReplaceAll(payload.records);
        }
    }
}
