using SS3D.Systems.Tile.GasMarkers;
using System;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.Commands
{
    public enum MapEditorCommandKind
    {
        PlaceTile,
        PlaceItem,
        ClearTile,
        ClearItem,
        MoveTile,
        MoveItem,
        Compound,
        SetFloorDecal,
        ClearFloorDecal,
        PlaceSpawnPoint,
        ClearSpawnPoint,
        PlaceGasMarker,
        ClearGasMarker,
    }

    [Serializable]
    public struct MapEditorCommandDto
    {
        public MapEditorCommandKind Kind;
        public string AssetName;
        public Vector3 Position;
        public Vector3 PreviousPosition;
        public Direction Direction;
        public Direction PreviousDirection;
        public bool ReplaceExisting;
        public ushort DecalId;
        public GasMarkerRecord GasMarker;
    }

    [Serializable]
    public struct MapEditorUndoStateDto
    {
        public int UndoDepth;
        public int RedoDepth;
    }
}
