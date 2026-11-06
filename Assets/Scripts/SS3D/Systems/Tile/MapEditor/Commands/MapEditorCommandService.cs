using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.SpawnPoints;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.Commands
{
    /// <summary>
    /// Executes invertible map editor mutations on the server tilemap.
    /// </summary>
    public sealed class MapEditorCommandService
    {
        private readonly MapEditorUndoStack _undoStack = new();
        private MapEditorCommandContext _context;
        private MapEditorPlacementMode _placementMode = MapEditorPlacementMode.Normal;

        public bool HasUnsavedChanges => _undoStack.HasUnsavedChanges;
        public long Revision => _undoStack.Revision;
        public void MarkSaved() => _undoStack.MarkSaved();
        public void MarkNewMap() => _undoStack.MarkNewMap();

        public int UndoDepth => _undoStack.UndoDepth;
        public int RedoDepth => _undoStack.RedoDepth;

        public MapEditorPlacementMode PlacementMode
        {
            get => _placementMode;
            set => _placementMode = value;
        }

        public void Bind(TileMap map, TileResourceLoader loader, ConstructionService construction,
            System.Action floorDecalsChanged = null,
            SpawnPointRegistry spawnPoints = null,
            System.Action spawnPointsChanged = null,
            GasMarkerRegistry gasMarkers = null)
        {
            _context = new MapEditorCommandContext(map, loader, construction)
            {
                PlacementMode = _placementMode,
                FloorDecalsChanged = floorDecalsChanged,
                SpawnPoints = spawnPoints,
                GasMarkers = gasMarkers,
                SpawnPointsChanged = spawnPointsChanged,
            };
        }

        public MapEditorCommandContext Context => _context;

        public void ClearHistory() => _undoStack.Clear();

        /// <param name="owner">Client that made the edit; see <see cref="MapEditorUndoStack.TryUndo"/>.</param>
        public bool Execute(IMapEditorCommand command, int owner = MapEditorUndoStack.AnyOwner)
        {
            if (_context == null)
                return false;

            _context.PlacementMode = _placementMode;
            command.Apply(_context);
            _undoStack.Push(command, owner);
            return true;
        }

        public bool ExecuteCompound(IReadOnlyList<IMapEditorCommand> commands, int owner = MapEditorUndoStack.AnyOwner)
        {
            if (commands == null || commands.Count == 0)
                return false;

            if (commands.Count == 1)
                return Execute(commands[0], owner);

            return Execute(new CompoundCommand(System.Array.ConvertAll(commands.ToArray(), c => c)), owner);
        }

        public bool Undo(int actor = MapEditorUndoStack.AnyOwner)
        {
            if (_context == null)
                return false;

            _context.PlacementMode = _placementMode;
            return _undoStack.TryUndo(_context, actor);
        }

        public bool Redo(int actor = MapEditorUndoStack.AnyOwner)
        {
            if (_context == null)
                return false;

            _context.PlacementMode = _placementMode;
            return _undoStack.TryRedo(_context, actor);
        }

        public static IMapEditorCommand FromPlacement(string assetName, Vector3 position, Direction direction, bool replaceExisting, bool isItem)
        {
            return isItem
                ? new PlaceItemCommand(assetName, position, direction)
                : new PlaceTileCommand(assetName, position, direction, replaceExisting);
        }

        public static IMapEditorCommand FromClear(string assetName, Vector3 position, Direction direction, bool isItem)
        {
            return isItem
                ? new ClearItemCommand(assetName, position, direction)
                : new ClearTileCommand(assetName, position, direction);
        }
    }
}
