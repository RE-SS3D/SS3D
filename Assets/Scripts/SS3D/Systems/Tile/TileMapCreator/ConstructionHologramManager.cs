using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile.FloorVisuals;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Commands;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.SpawnPoints;
using SS3D.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Class for managing tile objects, that will be used for building or deleting by the tile map menu.
    /// It handles creating construction holograms, replacing, adding or deleting tile objects and items upon placement,
    /// and placing the holograms in cool shapes like lines and squares.
    /// </summary>
    public class ConstructionHologramManager : NetworkActor
    {
        /// <summary>
        /// The last direction registered by a build ghost.
        /// </summary>
        private Direction _lastRegisteredDirection;
        private InputSubSystem _inputSystem;
        private Controls.TileCreatorActions _controls;
        private bool _isPlacingItem = false;

        /// <summary>Item cursor is over a wall/door (items may sit on tables, never on walls).</summary>
        private bool _itemPointBlocked;
        private bool _lastItemPointBlocked;

        private const string ItemOnWallMessage = "Items can't be placed on walls";
        /// <summary>
        /// Snapped position are positions in the center of tiles, to display tile objects ghosts properly.
        /// </summary>
        private Vector3 _lastSnappedPosition;
        /// <summary>
        /// The snapped position of the mouse, in the middle of a tile, when the player starts dragging with the mouse.
        /// </summary>
        private Vector3 _dragStartPostion;
        private Vector2Int _dragStartTile;
        private Vector2Int _lastDragEndTile;
        private bool _hasDragEndTile;
        private bool _lastSquareDrag;
        /// <summary>
        /// Is the player currently dragging ?
        /// </summary>
        public bool IsDragging => _isDragging;
        private bool _isDragging;
        private bool _placePressActive;
        /// <summary>True when LMB went down over UI — never commit that gesture.</summary>
        private bool _pressStartedOverUi;
        private GenericObjectSo _selectedObject;
        private FloorDecalDefinition _selectedFloorDecal;
        private string _selectedMarkerAssetName;
        /// <summary>
        /// List of build ghosts currently displaying in game.
        /// </summary>
        private List<ConstructionHologram> _holograms = new();
        /// <summary>Inactive holograms retained across drag resize to avoid Instantiate/Destroy thrash.</summary>
        private readonly List<ConstructionHologram> _hologramPool = new();
        private readonly List<Vector3> _dragTileBuffer = new();
        private readonly List<Vector2> _lineTileBuffer = new();
        private readonly List<MapEditorDeleteTarget> _deleteTargets = new();
        private string _lastHoverFailMessage;
        private string _deleteGhostAssetName;
        [SerializeField]
        private MapEditorSubSystem _mapEditor;

        // Room tool (MapEditorTool.Room): walls on the drag's border, floor inside.
        private bool _roomMode;
        private readonly ConstructionRoomTool _room = new();
        private readonly ConstructionDragFeedback _drag = new();
        private readonly ConstructionAutoFacing _facing = new();

        /// <summary>The cursor badge with the drag's size after it ("Steel Floor · 7 × 5 · 35 tiles").</summary>
        public string WithDragReadout(string label) => _drag.LabelWith(label);

        private bool AutoSubfloor => _mapEditor == null || _mapEditor.AutoSubfloor;

        // Ctrl held while building: erase what the open sub-tab places, as the Delete tool does, keeping the held object.
        private bool _erasing;
        public bool IsErasing => _erasing;
        private bool IsDeleting => _mapEditor != null && (_mapEditor.IsDeleting || _erasing);

        /// <summary>Ctrl flips between building and erasing between gestures; a drag keeps the mode it started in.</summary>
        private void UpdateEraseModifier()
        {
            Keyboard keyboard = Keyboard.current;
            bool held = keyboard != null && !InputInterface.IsCapturingText
                        && _mapEditor.CurrentTool is MapEditorTool.Edit or MapEditorTool.Room
                        && (keyboard.ctrlKey.isPressed || keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed);
            if (held == _erasing || _isDragging || _placePressActive)
                return;

            _erasing = held;
            ResetHologramsToCursor();
            if (!held)
                _mapEditor.RestoreDefaultPlacementHint();
        }

        public void ClearSelection()
        {
            CancelPlacementGesture(resetHolograms: false);
            _roomMode = false;
            _facing.Reset();
            _selectedObject = null;
            _selectedFloorDecal = null;
            _selectedMarkerAssetName = null;
            _deleteGhostAssetName = null;
            DestroyHolograms();
        }

        /// <summary>Room tool: a drag lays walls on the rectangle's border and floor inside; the cursor carries a wall.</summary>
        public void EnterRoomMode(TileObjectSo wall, TileObjectSo floor)
        {
            CancelPlacementGesture(resetHolograms: false);
            _roomMode = true;
            _facing.Reset();
            _room.SetParts(wall, floor);
            _selectedObject = wall;
            _selectedFloorDecal = null;
            _selectedMarkerAssetName = null;
            _deleteGhostAssetName = null;
            _isPlacingItem = false;
            DestroyHolograms();
            if (wall != null)
                CreateHologram(wall.PrefabAsset, GetPlacementPoint());
        }

        /// <summary>
        /// Delete / eraser mode: keep a cursor ghost scoped to the library subcategory.
        /// </summary>
        public void EnterDeleteMode()
        {
            CancelPlacementGesture(resetHolograms: false);
            _roomMode = false;
            _facing.Reset();
            _selectedObject = null;
            _selectedFloorDecal = null;
            _selectedMarkerAssetName = null;
            _isPlacingItem = false;
            _deleteGhostAssetName = null;
            DestroyHolograms();
            EnsureDeleteCursorHologram(GetPlacementPoint(forceTileSnap: true));
        }

        /// <summary>Re-resolve delete targets after subcategory / rotation changes.</summary>
        public void RefreshDeletePreview()
        {
            if (!IsDeleting)
                return;

            RefreshDeleteCursorHologram(GetPlacementPoint(forceTileSnap: true));
        }

        public void SetSelectedObject(GenericObjectSo genericObjectSo)
        {
            CancelPlacementGesture(resetHolograms: false);
            _roomMode = false;
            _facing.Reset();
            _selectedFloorDecal = null;
            _selectedMarkerAssetName = null;
            _deleteGhostAssetName = null;
            _isPlacingItem = genericObjectSo switch
            {
                TileObjectSo => false,
                ItemObjectSo => true,
                _ => _isPlacingItem,
            };
            _selectedObject = genericObjectSo;

            DestroyHolograms();
            CreateHologram(genericObjectSo.PrefabAsset, GetPlacementPoint());
        }

        /// <summary>
        /// V / Shift+V: swap the held object for another variant the way R rotates,
        /// keeping a drag in progress. The ghosts rebuild with the new prefab on the next update.
        /// </summary>
        public void SwapSelectedObject(GenericObjectSo genericObjectSo)
        {
            if (!_isDragging || genericObjectSo is not TileObjectSo || _selectedObject is not TileObjectSo)
            {
                SetSelectedObject(genericObjectSo);
                return;
            }

            _selectedObject = genericObjectSo;
            _facing.Reset();
            DestroyHolograms();
            _hasDragEndTile = false;
        }

        /// <summary>
        /// World point under the cursor for placement. Tile objects snap to the tile centre; items do not.
        /// </summary>
        public Vector3 GetPlacementPoint(bool forceTileSnap = false)
        {
            // Tile objects always sit on the grid; items are placed freely.
            bool snapToTile = forceTileSnap || !_isPlacingItem;
            Camera camera = _mapEditor != null ? _mapEditor.PickCamera : Camera.main;
            if (!snapToTile)
                return GetItemPlacementPoint(camera);

            if (IsPlacingWallMount())
                return _facing.AimWallMount(camera, SubSystems.Get<TileSubSystem>()?.CurrentMap);

            return TileHelper.GetPointedPosition(snapToTile, camera);
        }

        private bool IsPlacingWallMount() =>
            !_roomMode
            && _selectedObject is TileObjectSo { layer: TileLayer.WallMountHigh or TileLayer.WallMountLow }
            && !IsDeleting;

        /// <summary>Turns the cursor ghost the way <see cref="ConstructionAutoFacing"/> works out; returns whether it turned.</summary>
        private bool ApplyAutoFacing(ConstructionHologram hologram, Vector3 position)
        {
            if (_roomMode || _selectedObject is not TileObjectSo tile || IsDeleting
                || !_facing.Apply(hologram, position, tile, IsPlacingWallMount(), SubSystems.Get<TileSubSystem>()?.CurrentMap))
                return false;

            _lastRegisteredDirection = hologram.Direction;
            return true;
        }

        /// <summary>
        /// Items rest on whatever upward-facing surface is under the cursor (tables, counters, floor),
        /// falling back to the floor plane. Walls and doors block placement.
        /// </summary>
        private Vector3 GetItemPlacementPoint(Camera camera)
        {
            Vector3 floorPoint = TileHelper.GetPointedPosition(false, camera);
            _itemPointBlocked = false;

            if (camera == null || Mouse.current == null)
                return floorPoint;

            Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f, StationBounds.WithoutCeiling(Physics.DefaultRaycastLayers), QueryTriggerInteraction.Ignore))
                return floorPoint;

            PlacedTileObject placed = hit.collider.GetComponentInParent<PlacedTileObject>();
            if (placed != null && placed.tileObjectSO != null
                && placed.GenericType is TileObjectGenericType.Wall or TileObjectGenericType.Door)
            {
                _itemPointBlocked = true;
                return floorPoint;
            }

            // Characters are not surfaces; side faces (table edge, machine front) drop to the floor in front.
            if (hit.collider.GetComponentInParent<SS3D.Systems.Entities.Entity>() != null)
                return floorPoint;

            return hit.normal.y > 0.5f ? hit.point : floorPoint;
        }

        public void SetSelectedFloorDecal(FloorDecalDefinition definition)
        {
            CancelPlacementGesture(resetHolograms: false);
            _roomMode = false;
            _selectedFloorDecal = definition;
            _selectedObject = null;
            _selectedMarkerAssetName = null;
            _isPlacingItem = false;
            DestroyHolograms();
            CreateFloorDecalHologram(TileHelper.GetPointedPosition(true));
        }

        public void SetSelectedMarker(string markerAssetName)
        {
            CancelPlacementGesture(resetHolograms: false);
            _roomMode = false;
            _selectedMarkerAssetName = markerAssetName;
            _selectedObject = null;
            _selectedFloorDecal = null;
            _isPlacingItem = false;
            DestroyHolograms();
            CreateMarkerHologram(GetPlacementPoint(forceTileSnap: true));
        }

        protected override void OnAwake()
        {
            base.OnAwake();

#if !UNITY_SERVER
            _inputSystem = SubSystems.Get<InputSubSystem>();
            _controls = _inputSystem.Inputs.TileCreator;

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
#endif
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();

#if !UNITY_SERVER
            _controls.Replace.performed += HandleReplace;
            _controls.Replace.canceled += HandleReplace;
            _controls.Rotate.performed += HandleRotate;
#endif
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

#if !UNITY_SERVER
            _controls.Replace.performed -= HandleReplace;
            _controls.Replace.canceled -= HandleReplace;
            _controls.Rotate.performed -= HandleRotate;
#endif
            CancelPlacementGesture(resetHolograms: false);
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (_mapEditor == null || !_mapEditor.IsActive || _mapEditor.IsRoomCategory || !IsActiveTool(_mapEditor.CurrentTool))
            {
                if (_placePressActive || _isDragging)
                    CancelPlacementGesture(resetHolograms: _selectedObject != null || _selectedFloorDecal != null ||
                        _selectedMarkerAssetName != null ||
                        IsDeleting);
                // Non-construct tools (Select/Dropper/Move) used to leave the last ghost frozen in-world
                // because this path returned without tearing holograms down.
                if (_holograms.Count > 0 || _room.HasGhosts)
                    DestroyHolograms();
                return;
            }

            if (_holograms.Count == 1)
            {
                _holograms[0].UpdateRotationAndPosition();
            }

            ActivateGhosts();
            _room.KeepActive();

            UpdateEraseModifier();

            // Always resolve LMB up/down first — orbit used to early-return before this and
            // leave _placePressActive stuck so placement never worked again.
            HandlePlacementInput();

            // Freeze world picks while orbiting only. Do NOT freeze on MouseOverUI — the bottom
            // library used to be a full-width Position region, so vertical aiming (and anything
            // camera-framed over that band) looked like a "stuck" drag. Start/commit still gate on UI.
            if (_mapEditor.IsOrbiting)
                return;

            Vector3 position = GetPlacementPoint();
            // Grid tile for drag — ignore float Y / boundary jitter. Vertical mouse motion is
            // foreshortened under the orbit camera, so Vector3 equality used to rebuild the
            // Bresenham path nearly every frame while left/right stayed stable.
            Vector2Int cursorTile = ToTile(position);

            bool hasPlacementSelection = _selectedObject != null || _selectedFloorDecal != null ||
                                         _selectedMarkerAssetName != null;
            bool deleteMode = IsDeleting;

            // Move hologram, that sticks to the mouse. Currently it exists only if player is not dragging.
            if (_holograms.Count == 1 && !_isDragging)
            {
                ConstructionHologram cursor = _holograms[0];
                cursor.TargetPosition = position;
                bool turned = !deleteMode && ApplyAutoFacing(cursor, position);
                if (turned || cursorTile != ToTile(_lastSnappedPosition) || (_isPlacingItem && _itemPointBlocked != _lastItemPointBlocked))
                {
                    if (deleteMode)
                        RefreshDeleteCursorHologram(position);
                    else
                        RefreshHologram(cursor);
                }
            }
            else if (deleteMode && _holograms.Count == 0 && !_isDragging)
            {
                EnsureDeleteCursorHologram(position);
            }

            bool squareDrag = _controls.SquareDrag.IsPressed();
            if (_isDragging && _roomMode && !deleteMode)
            {
                if (!_hasDragEndTile || cursorTile != _lastDragEndTile)
                {
                    _lastDragEndTile = cursorTile;
                    _hasDragEndTile = true;
                    SyncRoomHolograms();
                }
            }
            else if (_isDragging && (hasPlacementSelection || deleteMode) &&
                (!_hasDragEndTile || cursorTile != _lastDragEndTile || squareDrag != _lastSquareDrag))
            {
                _lastDragEndTile = cursorTile;
                _hasDragEndTile = true;
                _lastSquareDrag = squareDrag;

                // Shift (Square Drag): filled rectangle. Default drag: Bresenham line.
                _drag.TooBig = squareDrag
                    && MapEditorPlacementRules.RectangleCellCount(_dragStartTile, cursorTile) > MapEditorPlacementRules.MaxDragCells;
                if (_drag.TooBig)
                    _dragTileBuffer.Clear();
                else if (squareDrag)
                    FillSquareDrag(cursorTile, _dragTileBuffer);
                else
                    FillLineDrag(cursorTile, _dragTileBuffer);

                SyncDragHolograms(_dragTileBuffer);
            }

            _lastSnappedPosition = position;
            _lastItemPointBlocked = _itemPointBlocked;
        }

        /// <summary>
        /// Edit places the selected asset; Delete is a dedicated always-erasing tool that reuses the same drag/hologram pipeline with no
        /// selected asset (see <see cref="IMapEditorHost.IsDeleting"/>).
        /// </summary>
        private static bool IsActiveTool(MapEditorTool tool) =>
            tool is MapEditorTool.Edit or MapEditorTool.Delete or MapEditorTool.Room;

        private static Vector2Int ToTile(Vector3 world) =>
            new(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z));

        /// <summary>
        /// Grow/shrink the active hologram list to match drag tiles without per-step Instantiate/Destroy.
        /// Validity uses local <see cref="MapEditorPlacementRules.Plan"/> (same as the cursor ghost); hover toasts stay off
        /// during drag, and the hint bar counts the cells that can't be placed instead.
        /// </summary>
        private void SyncDragHolograms(List<Vector3> tiles)
        {
            bool deleteMode = IsDeleting;

            while (_holograms.Count < tiles.Count)
            {
                ConstructionHologram rented = RentHologram();
                _holograms.Add(rented);
            }

            while (_holograms.Count > tiles.Count)
            {
                ConstructionHologram extra = _holograms[_holograms.Count - 1];
                _holograms.RemoveAt(_holograms.Count - 1);
                ReturnHologram(extra);
            }

            int blocked = 0;
            string firstReason = null;
            for (int i = 0; i < tiles.Count; i++)
            {
                ConstructionHologram hologram = _holograms[i];
                Vector3 tile = tiles[i];
                if (hologram.TargetPosition != tile || !hologram.ActiveSelf)
                {
                    hologram.TargetPosition = tile;
                    hologram.Hologram.transform.position = tile + new Vector3(0f, hologram.PlacementYOffset + 0.1f, 0f);
                }

                if (deleteMode)
                {
                    hologram.ChangeHologramColor(ConstructionMode.Delete);
                    continue;
                }

                string reason = RefreshHologram(hologram, reportHoverFeedback: false);
                if (!string.IsNullOrEmpty(reason))
                {
                    blocked++;
                    firstReason ??= reason;
                }
            }

            if (_drag.TooBig)
            {
                _drag.ShowTooBig(_mapEditor);
                return;
            }

            _drag.Show(_mapEditor,
                _lastSquareDrag
                    ? MapEditorPlacementRules.RectangleReadout(_dragStartTile, _lastDragEndTile)
                    : tiles.Count == 1 ? "1 tile" : $"{tiles.Count} tiles",
                blocked > 0 ? $"{blocked} of {tiles.Count} can't go here: {firstReason}" : null);
        }

        /// <summary>Lays the room tool's ghosts over the dragged rectangle; the cursor's wall ghost gives way to them.</summary>
        private void SyncRoomHolograms()
        {
            for (int i = _holograms.Count - 1; i >= 0; i--)
                _holograms[i].Destroy();
            _holograms.Clear();
            _room.Preview(SubSystems.Get<TileSubSystem>()?.CurrentMap, _dragStartTile, _lastDragEndTile, AutoSubfloor,
                part => CreateHologram(part.PrefabAsset, Vector3.zero, addToActive: false, asset: part), _drag, _mapEditor);
        }

        private ConstructionHologram RentHologram()
        {
            if (_hologramPool.Count > 0)
            {
                ConstructionHologram pooled = _hologramPool[_hologramPool.Count - 1];
                _hologramPool.RemoveAt(_hologramPool.Count - 1);
                pooled.SetActive = true;
                return pooled;
            }

            if (IsDeleting)
                return IsSpawnDeleteMode()
                    ? CreateMarkerHologram(Vector3.zero, addToActive: false)
                    : CreateDeleteMarkerHologram(Vector3.zero, addToActive: false);

            if (_selectedFloorDecal != null)
                return CreateFloorDecalHologram(Vector3.zero, addToActive: false);

            if (_selectedMarkerAssetName != null)
                return CreateMarkerHologram(Vector3.zero, addToActive: false);

            return _selectedObject != null
                ? CreateHologram(_selectedObject.PrefabAsset, Vector3.zero, addToActive: false)
                : CreateDeleteMarkerHologram(Vector3.zero, addToActive: false);
        }

        private void ReturnHologram(ConstructionHologram hologram)
        {
            if (hologram?.Hologram == null)
                return;

            hologram.SetActive = false;
            _hologramPool.Add(hologram);
        }

        private void ClearHologramPool()
        {
            for (int i = _hologramPool.Count - 1; i >= 0; i--)
                _hologramPool[i].Destroy();

            _hologramPool.Clear();
        }

        private void HandlePlacementInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            bool overUi = _mapEditor.MouseOverUI;
            bool pressed = mouse.leftButton.isPressed;
            bool justPressed = mouse.leftButton.wasPressedThisFrame;
            bool justReleased = mouse.leftButton.wasReleasedThisFrame;

            // Right click or Esc cancels a drag; right click with no drag puts down what the cursor holds (Esc doesn't:
            // it also opens the game menu).
            bool rightClick = mouse.rightButton.wasPressedThisFrame;
            bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame
                                                   && !InputInterface.IsCapturingText;
            if (rightClick || escape)
            {
                if (_placePressActive || _isDragging)
                {
                    CancelPlacementGesture(resetHolograms: true);
                    return;
                }

                if (rightClick && !overUi && !IsDeleting)
                {
                    _mapEditor.DropHeldObject();
                    return;
                }
            }

            // Missed-release recovery (orbit / focus loss / UI flicker skipped wasReleasedThisFrame).
            if (_placePressActive && !pressed && !justReleased)
            {
                EndPlacementGesture(commit: false);
                return;
            }

            if (justPressed)
            {
                if (overUi)
                {
                    _pressStartedOverUi = true;
                    return;
                }

                _pressStartedOverUi = false;
                _placePressActive = true;

                // Items are not on the grid: placing and deleting them are single clicks, never drags.
                if (!_isPlacingItem && !IsItemDeleteMode())
                {
                    _isDragging = true;
                    _dragStartPostion = GetPlacementPoint(forceTileSnap: true);
                    _dragStartTile = ToTile(_dragStartPostion);
                    _lastDragEndTile = _dragStartTile;
                    _hasDragEndTile = true;
                }

                return;
            }

            if (!_placePressActive)
                return;

            if (!justReleased)
                return;

            // Commit only if the gesture began and ended off UI. Ending over chrome cancels
            // without placing (and rebuilds a single cursor hologram).
            bool commit = !_pressStartedOverUi && !overUi;
            EndPlacementGesture(commit);
        }

        private void EndPlacementGesture(bool commit)
        {
            _placePressActive = false;
            _isDragging = false;
            _pressStartedOverUi = false;
            _hasDragEndTile = false;
            _lastSquareDrag = false;
            bool tooBig = _drag.TooBig;
            _drag.Clear(_mapEditor);

            if (commit && !tooBig)
            {
                PerformPlaceOrDelete();
                return;
            }

            if (commit)
                _mapEditor.ShowLocalToast(ConstructionDragFeedback.TooBigMessage);
            ResetHologramsToCursor();
        }

        private void CancelPlacementGesture(bool resetHolograms)
        {
            _placePressActive = false;
            _isDragging = false;
            _pressStartedOverUi = false;
            _hasDragEndTile = false;
            _lastSquareDrag = false;
            _erasing = false;
            _drag.Clear(_mapEditor);
            if (resetHolograms)
                ResetHologramsToCursor();
        }

        private void ResetHologramsToCursor()
        {
            if (IsDeleting)
            {
                DestroyHolograms();
                _deleteGhostAssetName = null;
                EnsureDeleteCursorHologram(GetPlacementPoint(forceTileSnap: true));
                return;
            }

            if (_selectedFloorDecal != null)
            {
                DestroyHolograms();
                CreateFloorDecalHologram(GetPlacementPoint(forceTileSnap: true));
                return;
            }

            if (_selectedMarkerAssetName != null)
            {
                DestroyHolograms();
                CreateMarkerHologram(GetPlacementPoint(forceTileSnap: true));
                return;
            }

            if (_selectedObject == null)
            {
                DestroyHolograms();
                return;
            }

            DestroyHolograms();
            CreateHologram(_selectedObject.PrefabAsset, GetPlacementPoint());
        }

        /// <summary>
        /// Rotate all existing ghosts in the next allowed rotation.
        /// </summary>
        public void SetNextRotation()
        {
            foreach (ConstructionHologram hologram in _holograms)
            {
                hologram.SetNextRotation();
                RefreshHologram(hologram);
            }

            if (_holograms.Count > 0)
                _lastRegisteredDirection = _holograms.First().Direction;

            if (IsDeleting)
            {
                RefreshDeleteCursorHologram(GetPlacementPoint(forceTileSnap: true));
                _mapEditor.SetPlacementHint(
                    $"Delete {MapEditorSubTabs.LabelOf(_mapEditor.CurrentSubcategory)} ({_lastRegisteredDirection}) — R to change face");
            }
        }

        public ConstructionHologram CreateFloorDecalHologram(Vector3 position, bool addToActive = true)
        {
            Material material = _selectedFloorDecal.MaterialOverride != null
                ? new Material(_selectedFloorDecal.MaterialOverride)
                : FloorVisualMesh.CreateCutoutMaterial(
                    _selectedFloorDecal.Texture != null
                        ? _selectedFloorDecal.Texture
                        : FloorVisualMesh.GetStripeCornerTexture(),
                    _selectedFloorDecal.Tint);

            GameObject tileObject = FloorVisualMesh.CreateQuadObject(
                "FloorDecalHologram",
                null,
                position,
                material);
            ConstructionHologram hologram = new(tileObject, position, _lastRegisteredDirection, FloorVisualMesh.SurfaceLift);
            if (addToActive)
                _holograms.Add(hologram);
            RefreshHologram(hologram, reportHoverFeedback: addToActive);
            return hologram;
        }

        public ConstructionHologram CreateMarkerHologram(Vector3 position, bool addToActive = true)
        {
            GameObject root = ConstructionMarkerGhost.Create(position, _selectedMarkerAssetName);
            ConstructionHologram hologram = new(root, position, _lastRegisteredDirection, 0f);
            if (addToActive)
                _holograms.Add(hologram);
            RefreshHologram(hologram, reportHoverFeedback: addToActive);
            return hologram;
        }

        /// <summary>
        /// Instantiate in the correct position and rotation a single hologram.
        /// </summary>
        /// <param name="addToActive">False when renting into the drag pool path (caller adds).</param>
        /// <param name="asset">What the ghost shows when not the held object (the room's floor); the caller colours it.</param>
        public ConstructionHologram CreateHologram(ObjectAssetReference prefabAsset, Vector3 position, bool addToActive = true,
            GenericObjectSo asset = null)
        {
            // Not ??: it skips Unity's null check for destroyed objects (UNT0007, an error under the project ruleset).
            GenericObjectSo shown = asset != null ? asset : _selectedObject;
            GameObject prefab = Assets.Get<GameObject>(prefabAsset);
            GameObject tileObject = Instantiate(prefab);
            float placementYOffset = shown is TileObjectSo tileObjectSo
                ? tileObjectSo.placementYOffset
                : 0f;

            // PlacedTileObject.Create overwrites the prefab root rotation with yaw only. Match it, or a
            // prefab with an authored root rotation (APC: 180°) previews on the opposite wall face.
            if (shown is TileObjectSo)
                tileObject.transform.localRotation = Quaternion.identity;

            ConstructionHologram hologram = new(tileObject, position, _lastRegisteredDirection, placementYOffset);
            tileObject.transform.rotation = hologram.TargetWorldRotation;
            tileObject.transform.position = hologram.TargetPosition + new Vector3(0, placementYOffset + 0.1f, 0);
            if (addToActive)
                _holograms.Add(hologram);
            if (asset == null)
                RefreshHologram(hologram, reportHoverFeedback: addToActive);
            return hologram;
        }

        /// <summary>
        /// Destroy all existing holograms.
        /// </summary>
        public void DestroyHolograms()
        {
            for (int i = _holograms.Count - 1; i >= 0; i--)
            {
                _holograms[i].Destroy();
            }
            _holograms.Clear();
            ClearHologramPool();
            _room.Clear();
        }

        /// <summary>
        /// Called upon control triggered to rotate holograms.
        /// </summary>
        private void HandleRotate(InputAction.CallbackContext context)
        {
            SetNextRotation();
        }

        private void PerformPlaceOrDelete()
        {
            if (IsDeleting)
                DeleteOnHolograms();
            else if (_roomMode)
                _room.Submit(SubSystems.Get<TileSubSystem>()?.CurrentMap, _dragStartTile, _lastDragEndTile, AutoSubfloor, _mapEditor);
            else
                PlaceOnHolograms();

            DestroyHolograms();
            _deleteGhostAssetName = null;

            if (IsDeleting)
            {
                EnsureDeleteCursorHologram(GetPlacementPoint(forceTileSnap: true));
                return;
            }

            if (_selectedFloorDecal != null)
            {
                CreateFloorDecalHologram(TileHelper.GetPointedPosition(true));
                return;
            }

            if (_selectedMarkerAssetName != null)
            {
                CreateMarkerHologram(GetPlacementPoint(forceTileSnap: true));
                return;
            }

            if (_selectedObject == null)
                return;

            CreateHologram(_selectedObject.PrefabAsset, GetPlacementPoint());
        }

        /// <summary>
        /// Called when the control is triggered to replace already present tile objects with new ones.
        /// </summary>
        /// <param name="context"></param>
        private void HandleReplace(InputAction.CallbackContext context)
        {
            foreach (ConstructionHologram buildGhost in _holograms)
            {
                RefreshHologram(buildGhost);
            }
        }

        /// <summary>
        /// Place all objects on the tilemap that are at the same locations as existing holograms.
        /// Invalid cells are skipped; successful cells are submitted as one undoable compound.
        /// </summary>
        private void PlaceOnHolograms()
        {
            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            if (tileSystem == null || _mapEditor == null ||
                (_selectedObject == null && _selectedFloorDecal == null && _selectedMarkerAssetName == null))
                return;

            bool isReplacing = _controls.Replace.phase == InputActionPhase.Performed;
            int skipped = 0;
            var skipMessages = new List<string>(4);
            var commands = new List<MapEditorCommandDto>(Mathf.Max(1, _holograms.Count));

            void NoteSkip(string message)
            {
                skipped++;
                if (string.IsNullOrEmpty(message))
                    return;

                if (!skipMessages.Contains(message))
                    skipMessages.Add(message);
            }

            void TryPlaceAt(Vector3 position, Direction direction)
            {
                if (_selectedFloorDecal != null)
                {
                    if (!HasPlenumAt(tileSystem.CurrentMap, position))
                    {
                        NoteSkip(BuildFailMessages.Format(BuildFailReason.MissingOrInvalidPlenum));
                        return;
                    }

                    commands.Add(new MapEditorCommandDto
                    {
                        Kind = MapEditorCommandKind.SetFloorDecal,
                        Position = position,
                        DecalId = _selectedFloorDecal.Id,
                    });
                    return;
                }

                if (_selectedMarkerAssetName != null)
                {
                    if (!HasPlenumAt(tileSystem.CurrentMap, position))
                    {
                        NoteSkip(BuildFailMessages.Format(BuildFailReason.MissingOrInvalidPlenum));
                        return;
                    }

                    if (MapEditorGasMarkerCatalog.TryDecode(_selectedMarkerAssetName, out _))
                    {
                        GasMarkerRecord marker = _mapEditor.GasMarkerDraft;
                        marker.Position = position;
                        commands.Add(new MapEditorCommandDto
                        {
                            Kind = MapEditorCommandKind.PlaceGasMarker,
                            AssetName = _selectedMarkerAssetName,
                            Position = position,
                            GasMarker = marker,
                        });
                        return;
                    }

                    commands.Add(new MapEditorCommandDto
                    {
                        Kind = MapEditorCommandKind.PlaceSpawnPoint,
                        AssetName = _selectedMarkerAssetName,
                        Position = position,
                        Direction = direction,
                    });
                    return;
                }

                if (_selectedObject is TileObjectSo tileObjectSo && tileSystem.CurrentMap != null)
                {
                    PlacementPlan plan = MapEditorPlacementRules.Plan(
                        tileSystem.CurrentMap, tileObjectSo, position, direction, isReplacing, AutoSubfloor);

                    if (plan.Outcome == PlacementOutcome.AlreadyThere) // dragging over it again is no mistake
                        return;

                    if (!plan.CanPlace)
                    {
                        NoteSkip(plan.PrimaryMessage);
                        return;
                    }

                    commands.Add(new MapEditorCommandDto
                    {
                        Kind = MapEditorCommandKind.PlaceTile,
                        AssetName = _selectedObject.NameString,
                        Position = position,
                        Direction = direction,
                        ReplaceExisting = plan.Replace,
                        AutoSubfloor = AutoSubfloor,
                    });
                    return;
                }

                if (_selectedObject is ItemObjectSo)
                {
                    if (_itemPointBlocked)
                    {
                        NoteSkip(ItemOnWallMessage);
                        return;
                    }

                    commands.Add(new MapEditorCommandDto
                    {
                        Kind = MapEditorCommandKind.PlaceItem,
                        AssetName = _selectedObject.NameString,
                        Position = position,
                        Direction = direction,
                    });
                }
            }

            if (_holograms.Count == 0)
            {
                TryPlaceAt(GetPlacementPoint(), _lastRegisteredDirection);
            }
            else
            {
                foreach (ConstructionHologram buildGhost in _holograms)
                    TryPlaceAt(buildGhost.TargetPosition, buildGhost.Direction);
            }

            if (commands.Count > 0)
                _mapEditor.SubmitCommands(commands.ToArray());

            if (skipped > 0)
                _mapEditor.ShowLocalToast(FormatSkippedPlacementToast(skipped, commands.Count, skipMessages));
        }

        internal static string FormatSkippedPlacementToast(int skipped, int placed, List<string> skipMessages)
        {
            if (skipMessages.Count == 0)
                return skipped == 1 ? "Cannot place here" : $"Skipped {skipped} tiles";

            if (placed == 0 && skipped == 1)
                return skipMessages[0];

            if (skipMessages.Count == 1)
                return $"Skipped {skipped} tiles: {skipMessages[0]}";

            return $"Skipped {skipped} tiles: {string.Join("; ", skipMessages)}";
        }

        /// <summary>
        /// Delete objects matching the current library subcategory at hologram tiles.
        /// </summary>
        private void DeleteOnHolograms()
        {
            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            if (tileSystem == null || _mapEditor == null)
                return;

            MapEditorSubcategory subcategory = _mapEditor.CurrentSubcategory;

            if (MapEditorDeleteTargeting.ScopeOf(subcategory) == MapEditorDeleteScope.ItemUnderCursor)
            {
                FindAndDeleteItem();
                return;
            }

            var commands = new List<MapEditorCommandDto>(8);

            if (_holograms.Count == 0)
            {
                CollectScopedClear(tileSystem, GetPlacementPoint(forceTileSnap: true), subcategory, commands);
            }
            else
            {
                foreach (ConstructionHologram hologram in _holograms)
                    CollectScopedClear(tileSystem, hologram.TargetPosition, subcategory, commands);
            }

            if (commands.Count == 0)
            {
                _mapEditor.ShowLocalToast(MapEditorDeleteTargeting.EmptyHint(subcategory, _lastRegisteredDirection));
                return;
            }

            _mapEditor.SubmitCommands(commands.ToArray());
        }

        private bool CollectScopedClear(
            TileSubSystem tileSystem,
            Vector3 position,
            MapEditorSubcategory subcategory,
            List<MapEditorCommandDto> into)
        {
            TileMap map = tileSystem.CurrentMap;
            if (map == null)
                return false;

            int before = into.Count;
            MapEditorDeleteScope scope = MapEditorDeleteTargeting.ScopeOf(subcategory);

            if (scope == MapEditorDeleteScope.FloorDecal)
            {
                if (!map.TryGetFloorDecalId(position, out ushort decalId) || decalId == 0)
                    return false;

                into.Add(new MapEditorCommandDto
                {
                    Kind = MapEditorCommandKind.ClearFloorDecal,
                    Position = position,
                });
                return true;
            }

            if (scope == MapEditorDeleteScope.SpawnPoint)
            {
                SpawnPointRegistry spawnPoints = tileSystem.SpawnPoints;
                if (spawnPoints == null || !spawnPoints.HasAt(position))
                    return false;

                into.Add(new MapEditorCommandDto
                {
                    Kind = MapEditorCommandKind.ClearSpawnPoint,
                    Position = position,
                });
                return true;
            }

            if (scope == MapEditorDeleteScope.GasMarker)
            {
                if (tileSystem.GasMarkers == null || !tileSystem.GasMarkers.HasAt(position))
                    return false;

                into.Add(new MapEditorCommandDto
                {
                    Kind = MapEditorCommandKind.ClearGasMarker,
                    Position = position,
                });
                return true;
            }

            if (!map.TryGetTileLocations(position, out ITileLocation[] locations))
                return false;

            MapEditorDeleteTargeting.Resolve(scope, _lastRegisteredDirection, locations, _deleteTargets);
            if (_deleteTargets.Count == 0)
                return false;

            foreach (MapEditorDeleteTarget target in _deleteTargets)
            {
                if (target.IsFloorDecal)
                {
                    into.Add(new MapEditorCommandDto
                    {
                        Kind = MapEditorCommandKind.ClearFloorDecal,
                        Position = position,
                    });
                    continue;
                }

                into.Add(new MapEditorCommandDto
                {
                    Kind = MapEditorCommandKind.ClearTile,
                    AssetName = target.AssetName,
                    Position = position,
                    Direction = target.Direction,
                });
            }

            return into.Count > before;
        }

        private void EraseAtPointer()
        {
            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            if (tileSystem == null || _mapEditor == null)
                return;

            var commands = new List<MapEditorCommandDto>(4);
            CollectScopedClear(tileSystem, GetPlacementPoint(forceTileSnap: true), _mapEditor.CurrentSubcategory, commands);
            if (commands.Count > 0)
                _mapEditor.SubmitCommands(commands.ToArray());
        }

        private void RefreshHologram(ConstructionHologram hologram) =>
            RefreshHologram(hologram, reportHoverFeedback: true);

        /// <summary>
        /// Update material of holograms based build (or anything else) mode and holograms position.
        /// Checks locally with <see cref="MapEditorPlacementRules.Plan"/> — never ServerRpc per ghost;
        /// drag used to spam RpcSendCanBuild and hitch so hard the path looked frozen.
        /// </summary>
        /// <returns>Why the ghost can't be placed, or null when it can.</returns>
        private string RefreshHologram(ConstructionHologram hologram, bool reportHoverFeedback)
        {
            if (IsDeleting)
            {
                hologram.ChangeHologramColor(ConstructionMode.Delete);
                return null;
            }

            if (_selectedFloorDecal != null || _selectedMarkerAssetName != null)
            {
                TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
                bool canPlace = HasPlenumAt(tileSystem?.CurrentMap, hologram.TargetPosition);
                string reason = canPlace ? null : BuildFailMessages.Format(BuildFailReason.MissingOrInvalidPlenum);
                hologram.ChangeHologramColor(canPlace ? ConstructionMode.Valid : ConstructionMode.Invalid);
                if (reportHoverFeedback)
                    ReportInvalidHover(reason);
                return reason;
            }

            if (_isPlacingItem)
            {
                hologram.ChangeHologramColor(_itemPointBlocked ? ConstructionMode.Invalid : ConstructionMode.Valid);
                if (reportHoverFeedback)
                    ReportInvalidHover(_itemPointBlocked ? ItemOnWallMessage : null);
                return _itemPointBlocked ? ItemOnWallMessage : null;
            }

            TileMap map = SubSystems.Get<TileSubSystem>()?.CurrentMap;
            if (_selectedObject is not TileObjectSo tileObjectSo || map == null)
            {
                hologram.ChangeHologramColor(ConstructionMode.Valid);
                return null;
            }

            bool isReplacing = _controls.Replace.phase == InputActionPhase.Performed;
            PlacementPlan plan = MapEditorPlacementRules.Plan(
                map, tileObjectSo, hologram.TargetPosition, hologram.Direction, isReplacing, AutoSubfloor);
            string failure = plan.Outcome == PlacementOutcome.Blocked ? plan.PrimaryMessage : null;
            hologram.ChangeHologramColor(failure == null ? ConstructionMode.Valid : ConstructionMode.Invalid);
            if (reportHoverFeedback)
                ReportInvalidHover(failure);
            return failure;
        }

        private void ReportInvalidHover(string message)
        {
            if (_mapEditor == null)
                return;

            if (string.IsNullOrEmpty(message))
            {
                if (_lastHoverFailMessage != null)
                {
                    _lastHoverFailMessage = null;
                    _mapEditor.RestoreDefaultPlacementHint();
                }

                return;
            }

            // Hover feedback lives in the hint bar only; toasts are for committed actions
            // (skipped tiles, nothing deleted). Toasting here showed every message twice.
            _lastHoverFailMessage = message;
            _mapEditor.SetPlacementHint(message);
        }

        private static bool HasPlenumAt(TileMap map, Vector3 worldPosition)
        {
            if (map == null)
                return false;

            return map.TryGetTileLocation(TileLayer.Plenum, worldPosition, out ITileLocation plenum)
                   && !plenum.IsFullyEmpty();
        }

        private void EnsureDeleteCursorHologram(Vector3 position)
        {
            if (IsItemDeleteMode())
            {
                RefreshItemDeleteCursor();
                return;
            }

            if (_holograms.Count == 0)
                CreateDeleteMarkerHologram(position);

            RefreshDeleteCursorHologram(position);
        }

        private void RefreshDeleteCursorHologram(Vector3 position)
        {
            if (IsItemDeleteMode())
            {
                RefreshItemDeleteCursor();
                return;
            }

            if (_holograms.Count == 0)
            {
                CreateDeleteMarkerHologram(position);
            }

            ConstructionHologram hologram = _holograms[0];
            hologram.TargetPosition = position;
            if (_holograms.Count == 1)
                _lastRegisteredDirection = hologram.Direction;

            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            TileMap map = tileSystem?.CurrentMap;
            MapEditorSubcategory subcategory = _mapEditor.CurrentSubcategory;
            MapEditorDeleteScope scope = MapEditorDeleteTargeting.ScopeOf(subcategory);
            _deleteTargets.Clear();

            string hintAsset = null;
            if (map != null && scope == MapEditorDeleteScope.FloorDecal)
            {
                if (map.TryGetFloorDecalId(position, out ushort id) && id != 0)
                {
                    _deleteTargets.Add(MapEditorDeleteTarget.FloorDecal);
                    hintAsset = "floor-decal";
                }
            }
            else if (map != null && scope == MapEditorDeleteScope.SpawnPoint)
            {
                if (tileSystem.SpawnPoints != null && tileSystem.SpawnPoints.HasAt(position))
                {
                    _deleteTargets.Add(MapEditorDeleteTarget.SpawnPoint);
                    hintAsset = "spawn-point";
                }
            }
            else if (map != null && scope == MapEditorDeleteScope.GasMarker)
            {
                if (tileSystem.GasMarkers != null && tileSystem.GasMarkers.HasAt(position))
                {
                    _deleteTargets.Add(MapEditorDeleteTarget.SpawnPoint);
                    hintAsset = "spawn-point";
                }
            }
            else if (map != null && map.TryGetTileLocations(position, out ITileLocation[] locations))
            {
                MapEditorDeleteTargeting.Resolve(scope, _lastRegisteredDirection, locations, _deleteTargets);
                if (_deleteTargets.Count > 0 && !_deleteTargets[0].IsFloorDecal && !_deleteTargets[0].IsSpawnPoint)
                    hintAsset = _deleteTargets[0].AssetName;
            }

            if (_deleteTargets.Count > 0 && hintAsset != null && hintAsset != "floor-decal" && hintAsset != "spawn-point")
            {
                MaybeSwapDeleteGhostPrefab(hintAsset, position);
            }
            else if (scope is MapEditorDeleteScope.SpawnPoint or MapEditorDeleteScope.GasMarker)
            {
                // Same pin ghost as placing a spawn, tinted red by the delete colouring below.
                ResetDeleteGhostToSpawnPin(position);
            }
            else if (hintAsset == "floor-decal")
            {
                ResetDeleteGhostToMarker(position);
            }
            else if (_mapEditor.TryGetSubcategoryPrototypeAssetName(subcategory, out string prototype))
            {
                // Show a representative prefab for this subcategory immediately (e.g. Wall
                // Mounts tab) — do not wait until the cursor finds a mounted object.
                MaybeSwapDeleteGhostPrefab(prototype, position);
            }
            else
            {
                ResetDeleteGhostToMarker(position);
            }

            if (_holograms.Count > 0)
            {
                ConstructionMode mode = _deleteTargets.Count > 0 ? ConstructionMode.Delete : ConstructionMode.Invalid;
                _holograms[0].ChangeHologramColor(mode);
            }

            if (_deleteTargets.Count == 0)
            {
                _mapEditor.SetPlacementHint(MapEditorDeleteTargeting.EmptyHint(subcategory, _lastRegisteredDirection));
            }
            else
            {
                string label = MapEditorSubTabs.LabelOf(subcategory);
                _mapEditor.SetPlacementHint(
                    scope == MapEditorDeleteScope.WallMountFace
                        ? $"Delete {label} ({_lastRegisteredDirection}) — R to change face"
                        : $"Delete {label}");
            }
        }

        /// <summary>
        /// Items sit off-grid, so item deletes raycast the item under the cursor instead of resolving
        /// tile layers. No ghost: the hint names the item that a click would remove.
        /// </summary>
        private bool IsItemDeleteMode() =>
            _mapEditor != null
            && IsDeleting
            && MapEditorDeleteTargeting.ScopeOf(_mapEditor.CurrentSubcategory) == MapEditorDeleteScope.ItemUnderCursor;

        private void RefreshItemDeleteCursor()
        {
            if (_holograms.Count > 0)
                DestroyHolograms();
            _deleteGhostAssetName = null;
            _deleteTargets.Clear();

            _mapEditor.SetPlacementHint(TryGetItemUnderCursor(out PlacedItemObject item)
                ? $"Click to delete {item.NameString}"
                : "Point at an item to delete it");
        }

        private bool TryGetItemUnderCursor(out PlacedItemObject item)
        {
            item = null;
            Camera camera = _mapEditor != null ? _mapEditor.PickCamera : Camera.main;
            if (camera == null || Mouse.current == null)
                return false;

            Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hitInfo, 500f, StationBounds.WithoutCeiling(Physics.DefaultRaycastLayers), QueryTriggerInteraction.Ignore))
                return false;

            item = hitInfo.collider.GetComponentInParent<PlacedItemObject>();
            return item != null;
        }

        private void ResetDeleteGhostToMarker(Vector3 position)
        {
            if (_deleteGhostAssetName == null && _holograms.Count > 0)
                return;

            DestroyHolograms();
            _deleteGhostAssetName = null;
            CreateDeleteMarkerHologram(position);
        }

        private const string SpawnPinDeleteGhost = "spawn-point";

        private bool IsSpawnDeleteMode() =>
            IsDeleting
            && MapEditorDeleteTargeting.ScopeOf(_mapEditor.CurrentSubcategory) is MapEditorDeleteScope.SpawnPoint or MapEditorDeleteScope.GasMarker;

        private void ResetDeleteGhostToSpawnPin(Vector3 position)
        {
            if (_deleteGhostAssetName == SpawnPinDeleteGhost && _holograms.Count > 0)
                return;

            DestroyHolograms();
            CreateMarkerHologram(position);
            _deleteGhostAssetName = SpawnPinDeleteGhost;
        }

        private void MaybeSwapDeleteGhostPrefab(string assetName, Vector3 position)
        {
            if (_deleteGhostAssetName == assetName && _holograms.Count > 0)
                return;

            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            GenericObjectSo asset = tileSystem?.GetAsset(assetName);
            if (asset?.PrefabAsset == null)
                return;

            Direction dir = _holograms.Count > 0 ? _holograms[0].Direction : _lastRegisteredDirection;
            _lastRegisteredDirection = dir;
            DestroyHolograms();
            CreateHologram(asset.PrefabAsset, position, asset: asset);
            if (_holograms.Count > 0)
                _holograms[0].ChangeHologramColor(ConstructionMode.Delete);

            _deleteGhostAssetName = assetName;
        }

        private ConstructionHologram CreateDeleteMarkerHologram(Vector3 position, bool addToActive = true)
        {
            Material material = FloorVisualMesh.CreateCutoutMaterial(
                FloorVisualMesh.GetStripeCornerTexture(),
                new Color(1f, 0.35f, 0.35f, 0.65f));
            GameObject marker = FloorVisualMesh.CreateQuadObject(
                "DeleteHologramMarker",
                null,
                position,
                material);
            ConstructionHologram hologram = new(marker, position, _lastRegisteredDirection, FloorVisualMesh.SurfaceLift);
            hologram.ChangeHologramColor(ConstructionMode.Delete);
            if (addToActive)
                _holograms.Add(hologram);
            return hologram;
        }

        /// <summary>
        /// Activate all buildGhosts. This method is important, because for some reason network objects disable themselves after a few frames.
        /// </summary>
        private void ActivateGhosts()
        {
            foreach (ConstructionHologram buildGhost in _holograms)
            {
                if (!buildGhost.ActiveSelf)
                {
                    buildGhost.SetActive = true;
                }
            }
        }

        private void FillLineDrag(Vector2Int endTile, List<Vector3> into)
        {
            into.Clear();
            MathUtility.FillTilesOnLine(
                _dragStartTile.x, _dragStartTile.y,
                endTile.x, endTile.y,
                _lineTileBuffer);
            for (int i = 0; i < _lineTileBuffer.Count; i++)
            {
                Vector2 tile = _lineTileBuffer[i];
                into.Add(new Vector3(tile.x, 0f, tile.y));
            }
        }

        private void FillSquareDrag(Vector2Int endTile, List<Vector3> into)
        {
            into.Clear();
            int x1 = Math.Min(_dragStartTile.x, endTile.x);
            int x2 = Math.Max(_dragStartTile.x, endTile.x);
            int y1 = Math.Min(_dragStartTile.y, endTile.y);
            int y2 = Math.Max(_dragStartTile.y, endTile.y);

            for (int i = y1; i <= y2; i++)
            {
                for (int j = x1; j <= x2; j++)
                {
                    into.Add(new Vector3(j, 0f, i));
                }
            }
        }
        
        /// <summary>
        /// Method called when trying to delete an item from the tilemap (as opposed to a tile object).
        /// Items need a special method because they are not tied to specific coordinates like tile objects.
        /// </summary>
        private void FindAndDeleteItem()
        {
            if (_mapEditor == null || !TryGetItemUnderCursor(out PlacedItemObject placedItem))
                return;

            // Undo re-places at this direction: take the yaw without the rest pose's own yaw.
            float yaw = placedItem.TryGetComponent(out SS3D.Systems.Inventory.Items.Item item)
                ? SS3D.Systems.Inventory.Items.ItemPlacement.YawAtRest(item, placedItem.transform.rotation)
                : placedItem.transform.eulerAngles.y;
            Direction direction = NearestCardinalDirection(yaw);
            _mapEditor.SubmitCommands(new[]
            {
                new MapEditorCommandDto
                {
                    Kind = MapEditorCommandKind.ClearItem,
                    AssetName = placedItem.NameString,
                    Position = placedItem.gameObject.transform.position,
                    Direction = direction,
                },
            });
        }

        private static Direction NearestCardinalDirection(float yaw)
        {
            int snapped = Mathf.RoundToInt(yaw / 90f) & 3;
            return snapped switch
            {
                0 => Direction.North,
                1 => Direction.East,
                2 => Direction.South,
                _ => Direction.West,
            };
        }
    }
}