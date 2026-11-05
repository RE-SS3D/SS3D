using FishNet.Object;
using SS3D.Systems.Tile;
using System.Collections.Generic;

namespace SS3D.Systems.Area
{
    public sealed partial class AreaSubSystem
    {
        private AreaPaintingService _painting;
        public bool HasAuthoredAreas => _painting != null && _painting.HasAuthoredAreas;

        [Server]
        public AreaRecord CreateAuthoredRoom(string name = null)
        {
            if (_painting == null) return null;
            AreaRecord room = new() { Id = _registry.AllocateId(), DisplayName = string.IsNullOrWhiteSpace(name) ? _painting.NextRoomName() : name.Trim(), ParentTag = "" };
            _registry.Register(room);
            _painting.HasAuthoredAreas = true;
            _painting.NotifyChanged();
            return room;
        }

        [Server]
        public void RestoreCreatedRoom(AreaRecord room)
        {
            _registry.Register(room);
            _painting.HasAuthoredAreas = true;
            _painting.NotifyChanged();
        }

        [Server]
        public void UndoCreatedRoom(AreaRecord room, bool wasAuthored)
        {
            if (!_registry.TryGet(room.Id, out AreaRecord current) || !ReferenceEquals(room, current)) return;
            _registry.Unregister(room.Id);
            _painting.HasAuthoredAreas = wasAuthored;
            _painting.NotifyChanged();
        }

        [Server]
        public bool TryPaintArea(ushort target, IReadOnlyList<UnityEngine.Vector2Int> tiles,
            out AreaPaintingService.AreaPaintChange change, out string error)
        {
            change = null;
            error = "Area system is not ready.";
            return _painting != null && _painting.TryPaint(target, tiles, out change, out error);
        }

        private void HandleAuthoredAreasChanged()
        {
            InvalidateAreaMembershipIndexes();
            UpdateAreaLightingStates();
            NotifyAreaVisualsChanged();
        }

        [Server]
        public void ResetForMapClear()
        {
            _registeredApcs.Clear();
            _registry.Clear();
            _lightingStates.Clear();
            _equipmentPowered.Clear();
            _lightingSwitchOn.Clear();
            _overlapFlaggedApcs.Clear();
            _pendingSavedByApcPosition = null;
            _templateRestoreActive = false;
            _pendingLiveBoundaryRecompute = false;
            if (_painting != null) _painting.HasAuthoredAreas = false;
            InvalidateAreaMembershipIndexes();
            FloorVisualCache.Clear();
            RpcSyncAreaFloorVisuals(System.Array.Empty<SyncedAreaTint>(), System.Array.Empty<SyncedChunkAreaIds>());
            RpcSyncAreaNames(System.Array.Empty<ushort>(), System.Array.Empty<string>());
            RpcSyncAreaAmbience(System.Array.Empty<SyncedAreaAmbience>());
            PushLightingSnapshotToObservers();
        }
    }
}
