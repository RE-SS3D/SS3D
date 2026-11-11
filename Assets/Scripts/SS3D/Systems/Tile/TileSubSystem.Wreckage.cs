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
        private StructuralAudioPresenter _structuralAudio;

        public override void OnStartClient()
        {
            base.OnStartClient();
            _wreckageView = gameObject.AddComponent<SteelWreckageView>();
            _structuralAudio = gameObject.AddComponent<StructuralAudioPresenter>();
            _steelWreckage.OnChange += OnWreckageChanged;
            RefreshWreckage();
        }

        public override void OnStopClient()
        {
            _steelWreckage.OnChange -= OnWreckageChanged;
            if (_wreckageView != null) Destroy(_wreckageView);
            if (_structuralAudio != null) Destroy(_structuralAudio);
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
            if (IsClient && _structuralAudio != null)
                _structuralAudio.OnSteelBreakup(record.Position + Vector3.up * Mathf.Min(record.Size.y * 0.5f, 1f));
        }

        /// <summary>Replicated, so clients can show "Clear the wreckage first" on the ghost.</summary>
        public bool HasWreckage(TileCoord coord, TileLayer layer)
        {
            for (int i = 0; i < _steelWreckage.Count; i++)
                if (_steelWreckage[i].Coord == coord && _steelWreckage[i].Layer == layer)
                    return true;
            return false;
        }

        /// <summary>Crowbar "Clear wreckage" and map edits remove it; construction placement does not (D9).</summary>
        [Server]
        public bool RemoveWreckage(TileCoord coord, TileLayer layer)
        {
            bool removed = false;
            for (int i = _steelWreckage.Count - 1; i >= 0; i--)
            {
                if (_steelWreckage[i].Coord == coord && _steelWreckage[i].Layer == layer)
                {
                    _steelWreckage.RemoveAt(i);
                    removed = true;
                }
            }

            if (IsClient) RefreshWreckage();
            return removed;
        }

        private int _keepWreckageDepth;

        /// <summary>
        /// Construction placements inside this scope leave wreckage alone (D9). Construction never places on
        /// wreckage, but a replace on a neighbouring layer must not sweep it either. The map editor keeps
        /// delete-on-place.
        /// </summary>
        public KeepWreckageScope KeepWreckage() => new(this);

        public readonly struct KeepWreckageScope : System.IDisposable
        {
            private readonly TileSubSystem _owner;

            public KeepWreckageScope(TileSubSystem owner)
            {
                _owner = owner;
                if (_owner != null) _owner._keepWreckageDepth++;
            }

            public void Dispose()
            {
                if (_owner != null) _owner._keepWreckageDepth--;
            }
        }

        private sealed class WreckageMutationObserver : ITileMutationObserver
        {
            private readonly TileSubSystem _owner;
            public WreckageMutationObserver(TileSubSystem owner) => _owner = owner;
            public void OnTilePlaced(ITileOccupant occupant, TileCoord coord)
            {
                if (_owner.IsServer && _owner._keepWreckageDepth == 0 && occupant is PlacedTileObject placed)
                    _owner.RemoveWreckage(coord, placed.Layer);
            }
            public void OnTileCleared(ITileOccupant occupant, TileCoord coord, TileLayer layer) { }
            public void OnChunkCreated(TileChunkRef chunk) { }
            public void OnTileStateChanged(TileCoord coord) { }
        }
    }
}
