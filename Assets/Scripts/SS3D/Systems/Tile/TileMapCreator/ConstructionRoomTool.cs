using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Commands;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// The room tool (<see cref="MapEditorTool.Room"/>): previews and sends walls on a dragged rectangle's border and
    /// floor inside, each with its own ghost set. It only adds, keeping the walls, doors and floors already there
    /// (<see cref="MapEditorPlacementRules.RoomBuildsOver"/>).
    /// </summary>
    internal sealed class ConstructionRoomTool
    {
        private readonly ConstructionHologramSet _wallGhosts = new();
        private readonly ConstructionHologramSet _floorGhosts = new();
        private readonly List<Vector3> _border = new();
        private readonly List<Vector3> _interior = new();
        private readonly List<Vector3> _shown = new();
        private readonly List<bool> _blocked = new();

        public TileObjectSo Wall { get; private set; }
        public TileObjectSo Floor { get; private set; }
        public bool HasParts => Wall != null && Floor != null;
        public bool HasGhosts => _wallGhosts.Count > 0 || _floorGhosts.Count > 0;

        public void SetParts(TileObjectSo wall, TileObjectSo floor)
        {
            Wall = wall;
            Floor = floor;
        }

        public void KeepActive()
        {
            _wallGhosts.KeepActive();
            _floorGhosts.KeepActive();
        }

        public void HideGhosts()
        {
            _wallGhosts.Resize(0, null);
            _floorGhosts.Resize(0, null);
        }

        public void Clear()
        {
            _wallGhosts.Clear();
            _floorGhosts.Clear();
        }

        /// <summary>Previews a drag between two corner tiles: ghosts, the size readout and the hint for blocked cells.</summary>
        public void Preview(TileMap map, Vector2Int corner, Vector2Int oppositeCorner, bool autoSubfloor,
            Func<TileObjectSo, ConstructionHologram> create, ConstructionDragFeedback feedback, MapEditorSubSystem editor)
        {
            if (map == null || !HasParts)
                return;

            if (MapEditorPlacementRules.RectangleCellCount(corner, oppositeCorner) > MapEditorPlacementRules.MaxDragCells)
            {
                HideGhosts();
                feedback.ShowTooBig(editor);
                return;
            }

            feedback.TooBig = false;
            string hint = Sync(map, corner, oppositeCorner, autoSubfloor, create);
            feedback.Show(editor, "Room · " + MapEditorPlacementRules.RectangleReadout(corner, oppositeCorner), hint);
        }

        /// <summary>Sends the room as one gesture, so one undo takes the whole room (and the subfloor laid for it).</summary>
        public void Submit(TileMap map, Vector2Int corner, Vector2Int oppositeCorner, bool autoSubfloor, MapEditorSubSystem editor)
        {
            if (map == null || editor == null)
                return;

            if (!HasParts)
            {
                editor.ShowLocalToast("Pick a wall and a floor in the library first");
                return;
            }

            var commands = new List<MapEditorCommandDto>();
            var skipMessages = new List<string>(4);
            int skipped = CollectCommands(map, corner, oppositeCorner, autoSubfloor, commands, skipMessages);
            if (commands.Count > 0)
                editor.SubmitCommands(commands.ToArray());

            if (skipped > 0)
                editor.ShowLocalToast(ConstructionHologramManager.FormatSkippedPlacementToast(skipped, commands.Count, skipMessages));
        }

        /// <summary>
        /// Lays the ghosts over the rectangle between two corner tiles: none on cells the room keeps, red where something
        /// blocks the build. Returns the hint for the blocked cells, or null when every cell can be built.
        /// </summary>
        private string Sync(TileMap map, Vector2Int corner, Vector2Int oppositeCorner, bool autoSubfloor,
            Func<TileObjectSo, ConstructionHologram> create)
        {
            MapEditorPlacementRules.RoomCells(corner, oppositeCorner, _border, _interior);
            int blocked = 0;
            string firstReason = null;
            int shown = SyncPart(map, Wall, _border, _wallGhosts, autoSubfloor, create, ref blocked, ref firstReason)
                        + SyncPart(map, Floor, _interior, _floorGhosts, autoSubfloor, create, ref blocked, ref firstReason);

            return blocked > 0 ? $"{blocked} of {shown} can't go here: {firstReason}" : null;
        }

        /// <summary>
        /// The room's place commands, one per cell it builds, into <paramref name="into"/>. Returns how many cells were
        /// blocked, with their distinct reasons in <paramref name="skipMessages"/>.
        /// </summary>
        private int CollectCommands(TileMap map, Vector2Int corner, Vector2Int oppositeCorner, bool autoSubfloor,
            List<MapEditorCommandDto> into, List<string> skipMessages)
        {
            MapEditorPlacementRules.RoomCells(corner, oppositeCorner, _border, _interior);
            return AddCommands(map, Wall, _border, autoSubfloor, into, skipMessages)
                   + AddCommands(map, Floor, _interior, autoSubfloor, into, skipMessages);
        }

        private int SyncPart(TileMap map, TileObjectSo part, List<Vector3> cells, ConstructionHologramSet ghosts,
            bool autoSubfloor, Func<TileObjectSo, ConstructionHologram> create, ref int blocked, ref string firstReason)
        {
            _shown.Clear();
            _blocked.Clear();
            foreach (Vector3 cell in cells)
            {
                PlacementPlan plan = CellPlan(map, part, cell, autoSubfloor);
                if (plan.Outcome == PlacementOutcome.AlreadyThere)
                    continue;

                _shown.Add(cell);
                _blocked.Add(!plan.CanPlace);
                if (!plan.CanPlace)
                {
                    blocked++;
                    firstReason ??= plan.PrimaryMessage;
                }
            }

            ghosts.Resize(_shown.Count, () => create(part));
            for (int i = 0; i < _shown.Count; i++)
            {
                ghosts.MoveTo(i, _shown[i]);
                ghosts[i].ChangeHologramColor(_blocked[i] ? ConstructionMode.Invalid : ConstructionMode.Valid);
            }

            return _shown.Count;
        }

        private static int AddCommands(TileMap map, TileObjectSo part, List<Vector3> cells, bool autoSubfloor,
            List<MapEditorCommandDto> into, List<string> skipMessages)
        {
            int skipped = 0;
            foreach (Vector3 cell in cells)
            {
                PlacementPlan plan = CellPlan(map, part, cell, autoSubfloor);
                if (plan.Outcome == PlacementOutcome.AlreadyThere)
                    continue;

                if (!plan.CanPlace)
                {
                    skipped++;
                    string message = plan.PrimaryMessage;
                    if (!string.IsNullOrEmpty(message) && !skipMessages.Contains(message))
                        skipMessages.Add(message);
                    continue;
                }

                into.Add(new MapEditorCommandDto
                {
                    Kind = MapEditorCommandKind.PlaceTile,
                    AssetName = part.NameString,
                    Position = cell,
                    Direction = Direction.North,
                    ReplaceExisting = plan.Replace,
                    AutoSubfloor = autoSubfloor,
                });
            }

            return skipped;
        }

        /// <summary>What the room does on one cell: skip what it keeps, else place as the editor would.</summary>
        private static PlacementPlan CellPlan(TileMap map, TileObjectSo part, Vector3 cell, bool autoSubfloor)
        {
            TurfKind existing = MapEditorPlacementRules.TurfKindAt(map, cell);
            if (!MapEditorPlacementRules.RoomBuildsOver(MapEditorPlacementRules.KindOf(part), existing))
                return PlacementPlan.AlreadyThere;

            return MapEditorPlacementRules.Plan(map, part, cell, Direction.North, forceReplace: false, autoSubfloor);
        }
    }
}
