using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.StructuralDamage;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    public partial class TileSubSystem
    {
        [SyncObject] private readonly SyncList<SteelWreckageRecord> _steelWreckage = new();
        private SteelWreckageView _wreckageView;

        public override void OnStartClient()
        {
            base.OnStartClient();
            _wreckageView = gameObject.AddComponent<SteelWreckageView>();
            _steelWreckage.OnChange += OnWreckageChanged;
            RefreshWreckage();
        }

        public override void OnStopClient()
        {
            _steelWreckage.OnChange -= OnWreckageChanged;
            if (_wreckageView != null) Destroy(_wreckageView);
            base.OnStopClient();
        }

        private void OnWreckageChanged(SyncListOperation op, int index, SteelWreckageRecord oldItem,
            SteelWreckageRecord newItem, bool asServer)
        {
            if (!asServer || IsClient) RefreshWreckage();
        }

        private void RefreshWreckage()
        {
            if (_wreckageView != null) _wreckageView.Reconcile(_steelWreckage);
        }

        [Server]
        public void ServerAddSteelWreckage(SteelWreckageRecord record)
        {
            RemoveWreckage(record.Coord, record.Layer);
            _steelWreckage.Add(record);
            // Host presentation must not depend on a SyncList client callback.
            if (IsClient) RefreshWreckage();
            RpcSteelBreakup(record);
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcSteelBreakup(SteelWreckageRecord record)
        {
            if (IsClient && _wreckageView != null) _wreckageView.BreakApart(record);
        }

        private void RemoveWreckage(TileCoord coord, TileLayer layer)
        {
            for (int i = _steelWreckage.Count - 1; i >= 0; i--)
                if (_steelWreckage[i].Coord == coord && _steelWreckage[i].Layer == layer)
                    _steelWreckage.RemoveAt(i);
            if (IsClient) RefreshWreckage();
        }

        private sealed class WreckageMutationObserver : ITileMutationObserver
        {
            private readonly TileSubSystem _owner;
            public WreckageMutationObserver(TileSubSystem owner) => _owner = owner;
            public void OnTilePlaced(ITileOccupant occupant, TileCoord coord)
            {
                if (_owner.IsServer && occupant is PlacedTileObject placed)
                    _owner.RemoveWreckage(coord, placed.Layer);
            }
            public void OnTileCleared(ITileOccupant occupant, TileCoord coord, TileLayer layer) { }
            public void OnChunkCreated(TileChunkRef chunk) { }
            public void OnTileStateChanged(TileCoord coord) { }
        }
    }
}
