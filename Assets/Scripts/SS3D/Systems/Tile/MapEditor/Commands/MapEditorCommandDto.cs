using SS3D.Data.Persistence;
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

        /// <summary>
        /// An object's setup changed in-world under an editor user (<see cref="SetObjectSetupCommand"/>). Server-made
        /// only: the RPC refuses it from clients.
        /// </summary>
        SetObjectSetup,
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

        /// <summary>
        /// Place: the setup a Dropper copy carries (null from the palette). Set setup: the setup after the change.
        /// </summary>
        public SavedSetupEntry[] Setup;

        /// <summary>Set setup: the setup before the change.</summary>
        public SavedSetupEntry[] PreviousSetup;
    }

    [Serializable]
    public struct MapEditorUndoStateDto
    {
        public int UndoDepth;
        public int RedoDepth;
    }
}
