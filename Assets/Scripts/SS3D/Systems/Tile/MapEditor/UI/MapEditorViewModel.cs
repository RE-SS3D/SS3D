using SS3D.Systems.Tile.GasMarkers;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile.TileMapCreator;
using SS3D.Systems.Tile.Validation;
using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    /// <summary>
    /// View-model state for the map editor UI.
    /// </summary>
    public sealed class MapEditorViewModel
    {
        public string AreaRoomName { get; set; } = "New room";
        public ushort AreaRoomId { get; set; }
        public bool IsRoomCategory => CurrentSubcategory == MapEditorSubcategory.Rooms;
        public MapEditorTool CurrentTool { get; set; } = MapEditorTool.Edit;
        public MapEditorMode CurrentMode { get; set; } = MapEditorMode.Upper;
        public MapEditorSubcategory CurrentSubcategory { get; set; } = MapEditorSubTabs.Default;
        /// <summary>Decides which sub-tabs are drawn (empty ones are not); null draws every row the sandbox allows.</summary>
        public MapEditorCatalog Catalog { get; set; }
        public string SearchText { get; set; } = string.Empty;
        /// <summary>A non-empty search shows matches from every tab instead of the open sub-tab.</summary>
        public bool IsSearching => !string.IsNullOrWhiteSpace(SearchText);
        public GenericObjectSo SelectedAsset { get; set; }
        public MapEditorCatalogEntry SelectedEntry { get; set; }
        public bool HideUI { get; set; }
        public bool DebugOverlay { get; set; }
        /// <summary>Light fixtures glow without grid power (server-wide; persists after closing the editor).</summary>
        public bool AlwaysPowerLights { get; set; }
        /// <summary>Builder takes no damage and walks on open space (persists after closing the editor).</summary>
        public bool Invincible { get; set; } = true;
        public float CameraFov { get; set; } = 72f;
        public float CameraZoomSpeed { get; set; } = 5f;
        public float CameraRotationSpeed { get; set; } = 4f;
        public int UndoDepth { get; set; }
        public int RedoDepth { get; set; }
        public string SelectedObjectHint { get; set; } = "Select the Edit tool to place this object";
        public MapValidationIssue[] ValidationIssues { get; private set; } = Array.Empty<MapValidationIssue>();
        public bool ValidationChecked { get; private set; }
        public string ValidationMode { get; private set; }
        public bool ValidationStale { get; set; }
        public int ValidationRevision { get; private set; }
        public int ValidationContext { get; private set; }

        public void SetValidation(MapValidationIssue[] issues, string mode, bool resetDismissed = false)
        {
            ValidationIssues = issues ?? Array.Empty<MapValidationIssue>();
            ValidationMode = mode;
            ValidationChecked = true;
            ValidationStale = false;
            ValidationRevision++;
            if (resetDismissed) ValidationContext++;
            NotifyChanged();
        }
        /// <summary>What the server lets this player use; full until the server answers on open.</summary>
        public MapEditorCapabilities Capabilities { get; private set; } = MapEditorCapabilities.Full;

        public void SetCapabilities(MapEditorCapabilities capabilities)
        {
            Capabilities = capabilities;
            if (!capabilities.Persistence && OpenPopover is "saveMenu" or "maps")
                OpenPopover = null;
            // A tab remembered from an admin session can be one this player may not see.
            if (MapEditorSubTabs.TryGet(CurrentSubcategory, out MapEditorSubTab open) && !IsSubTabShown(open))
            {
                SetMode(CurrentMode);
                return;
            }
            NotifyChanged();
        }

        public string ToastMessage { get; set; }
        public string OpenPopover { get; set; }

        /// <summary>What the next placed gas marker writes, or what the marker form is editing.</summary>
        public GasMarkerRecord GasMarkerDraft { get; set; } = GasMarkerRecord.Default(GasMarkerKind.Source);
        public string SaveMapName { get; set; } = "Untitled Map";
        public Direction CurrentRotation { get; set; } = Direction.North;
        public MapEditorSaveTarget SaveTarget { get; set; } = MapEditorSaveTarget.LocalTemplate;
        public MapEditorPlacementMode PlacementMode { get; set; } = MapEditorPlacementMode.Normal;

        private readonly Dictionary<TileLayerCategory, bool> _layerCategoryVisibility = new();

        // True while a Lower run tab (Cables, Gas Pipes, Disposal Pipes) hid Turfs; leaving those tabs restores it. A manual Turfs toggle clears it.
        private bool _turfsAutoHidden;

        public event Action StateChanged;

        public void EnsureLayerDefaults()
        {
            foreach (TileLayerCategory category in TileLayerCategoryMapping.AllCategories)
            {
                if (!_layerCategoryVisibility.ContainsKey(category))
                    _layerCategoryVisibility[category] = category is not (TileLayerCategory.Areas or TileLayerCategory.Scripts);
            }
        }

        public bool IsLayerCategoryVisible(TileLayerCategory category) =>
            _layerCategoryVisibility.TryGetValue(category, out bool visible) ? visible : category is not (TileLayerCategory.Areas or TileLayerCategory.Scripts);

        public void SetLayerCategoryVisible(TileLayerCategory category, bool visible)
        {
            _layerCategoryVisibility[category] = visible;
            if (category == TileLayerCategory.Turfs) _turfsAutoHidden = false;
            NotifyChanged();
        }

        public void NotifyChanged() => StateChanged?.Invoke();

        public void SetTool(MapEditorTool tool)
        {
            CurrentTool = tool;
            _layerCategoryVisibility[TileLayerCategory.Areas] = IsRoomCategory;
            UpdateHint();
            NotifyChanged();
        }

        public void SetMode(MapEditorMode mode)
        {
            CurrentMode = mode;
            MapEditorSubTab first = FirstShownSubTab(mode);
            if (first != null)
                CurrentSubcategory = first.Id;
            _layerCategoryVisibility[TileLayerCategory.Scripts] = mode == MapEditorMode.Scripting;
            UpdateCategoryTool();
            UpdateHint();
            NotifyChanged();
        }

        public void SetSubcategory(MapEditorSubcategory subcategory)
        {
            CurrentSubcategory = subcategory;
            UpdateCategoryTool();
            UpdateHint();
            NotifyChanged();
        }

        /// <summary>Opens <paramref name="subcategory"/> together with its mode (search picks, Show in palette, last tab).</summary>
        public void OpenSubTab(MapEditorSubcategory subcategory)
        {
            if (!MapEditorSubTabs.TryGet(subcategory, out MapEditorSubTab row))
                return;

            CurrentMode = row.Mode;
            CurrentSubcategory = subcategory;
            _layerCategoryVisibility[TileLayerCategory.Scripts] = row.Mode == MapEditorMode.Scripting;
            UpdateCategoryTool();
            UpdateHint();
            NotifyChanged();
        }

        /// <summary>Whether the palette draws <paramref name="row"/> for this player.</summary>
        public bool IsSubTabShown(MapEditorSubTab row) =>
            Catalog?.IsShown(row, Capabilities.RestrictedPalette)
            ?? (row != null && !(Capabilities.RestrictedPalette && row.SandboxRestricted));

        /// <summary>The mode's sub-tabs this player sees, in build order.</summary>
        public IEnumerable<MapEditorSubTab> ShownSubTabs(MapEditorMode mode)
        {
            foreach (MapEditorSubTab row in MapEditorSubTabs.InMode(mode))
            {
                if (IsSubTabShown(row))
                    yield return row;
            }
        }

        private MapEditorSubTab FirstShownSubTab(MapEditorMode mode)
        {
            foreach (MapEditorSubTab row in ShownSubTabs(mode))
                return row;

            return null;
        }

        private void UpdateCategoryTool()
        {
            bool rooms = IsRoomCategory;
            if (CurrentTool == MapEditorTool.Area) CurrentTool = MapEditorTool.Edit;
            _layerCategoryVisibility[TileLayerCategory.Areas] = rooms;
            UpdateTurfsForLowerRuns();
        }

        /// <summary>Cables, gas pipes and disposals run under the turf, so their tabs hide Turfs until you leave them.</summary>
        private void UpdateTurfsForLowerRuns()
        {
            bool underTurf = CurrentSubcategory is MapEditorSubcategory.Cables
                or MapEditorSubcategory.GasPipes or MapEditorSubcategory.DisposalPipes;
            if (underTurf && !_turfsAutoHidden && IsLayerCategoryVisible(TileLayerCategory.Turfs))
            {
                _layerCategoryVisibility[TileLayerCategory.Turfs] = false;
                _turfsAutoHidden = true;
            }
            else if (!underTurf && _turfsAutoHidden)
            {
                _layerCategoryVisibility[TileLayerCategory.Turfs] = true;
                _turfsAutoHidden = false;
            }
        }

        public void SelectEntry(MapEditorCatalogEntry entry, GenericObjectSo asset)
        {
            SelectedEntry = entry;
            SelectedAsset = asset;
            RememberVariant(entry);
            UpdateHint();
            NotifyChanged();
        }

        /// <summary>
        /// The one route for Dropper, search and Show in palette: holds the catalog's own entry for
        /// <paramref name="entry"/>'s asset, so a variant becomes its family's current one and is remembered, and opens
        /// the sub-tab it lives in when this player sees it. An asset the catalog lacks is held as given.
        /// Returns whether the open sub-tab changed.
        /// </summary>
        public bool SelectAsset(MapEditorCatalogEntry entry, GenericObjectSo asset)
        {
            if (entry == null)
                return false;

            bool inCatalog = Catalog != null && Catalog.TryGetEntry(entry.AssetName, out MapEditorCatalogEntry known)
                && (entry = known) != null;

            // Selected first, so opening the sub-tab keeps this entry instead of picking its first one.
            SelectEntry(entry, asset);
            if (!inCatalog
                || entry.Subcategory == CurrentSubcategory
                || !MapEditorSubTabs.TryGet(entry.Subcategory, out MapEditorSubTab row)
                || !IsSubTabShown(row))
                return false;

            OpenSubTab(entry.Subcategory);
            return true;
        }

        /// <summary>
        /// The variant a family slot shows: the held one, else the family's last pick this session,
        /// else its base variant. A plain slot shows its entry.
        /// </summary>
        public MapEditorCatalogEntry ShownVariant(MapEditorPaletteSlot slot)
        {
            if (slot.IndexOf(SelectedEntry) >= 0)
                return SelectedEntry;

            if (slot.IsFamily && _lastVariant.TryGetValue(FamilyKey(slot.Variants[0]), out string assetName))
            {
                foreach (MapEditorCatalogEntry variant in slot.Variants)
                {
                    if (string.Equals(variant.AssetName, assetName, StringComparison.OrdinalIgnoreCase))
                        return variant;
                }
            }

            return slot.Variants[0];
        }

        /// <summary>Whether the held entry belongs to a family with more than one variant (V cycles it).</summary>
        public bool HoldsFamily => SelectedEntry != null && !string.IsNullOrEmpty(SelectedEntry.Family)
            && (Catalog?.SlotOf(SelectedEntry).IsFamily ?? true);

        // Last variant picked per family, for this session only (not PlayerPrefs).
        private readonly Dictionary<string, string> _lastVariant = new(StringComparer.OrdinalIgnoreCase);

        private void RememberVariant(MapEditorCatalogEntry entry)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.Family))
                _lastVariant[FamilyKey(entry)] = entry.AssetName;
        }

        // Family labels are unique within a sub-tab (MapEditorCatalogGeneration.FamilyProblems), not across them.
        private static string FamilyKey(MapEditorCatalogEntry entry) => $"{(int)entry.Subcategory}/{entry.Family}";

        public void ClearSelection()
        {
            SelectedEntry = null;
            SelectedAsset = null;
            UpdateHint();
            NotifyChanged();
        }

        public void ShowToast(string message)
        {
            ToastMessage = message;
            NotifyChanged();
        }

        public void ClearToast()
        {
            ToastMessage = null;
            NotifyChanged();
        }

        public void SetUndoState(int undoDepth, int redoDepth)
        {
            UndoDepth = undoDepth;
            RedoDepth = redoDepth;
            NotifyChanged();
        }

        /// <summary>Recompute the default hint (after a transient hover message such as a build failure).</summary>
        public void RefreshHint() => UpdateHint();

        /// <summary>
        /// Construct-tool hint for the selected asset. Lower-layer runs (plenum, pipes, disposals) and
        /// flooring are usually laid in areas, so Shift fill leads; wall mounts lead with R to rotate.
        /// </summary>
        public string PlacementHint()
        {
            TileLayer? layer = (SelectedAsset as TileObjectSo)?.layer;

            if (CurrentMode == MapEditorMode.Items || SelectedAsset is ItemObjectSo)
                return "Click to place the item";

            if (layer is TileLayer.WallMountHigh or TileLayer.WallMountLow
                || (layer == null && CurrentSubcategory == MapEditorSubcategory.WallMounts))
                return "Press R to rotate — click a wall to place, drag for a line";

            if (CurrentMode == MapEditorMode.Lower
                || layer is TileLayer.Plenum or TileLayer.Turf or TileLayer.Wire or TileLayer.Disposal
                    or TileLayer.PipeLeft or TileLayer.PipeMiddle or TileLayer.PipeRight or TileLayer.PipeSurface
                    or TileLayer.PipeManifold)
                return "Hold Shift and drag to fill a rectangle — plain drag places a line";

            return "Drag to place a line, Shift+drag to fill a rectangle — R to rotate";
        }

        private void UpdateHint()
        {
            if (IsRoomCategory)
            {
                SelectedObjectHint = CurrentTool switch
                {
                    MapEditorTool.Edit => $"Construct: {AreaRoomName} — drag a line; Shift-drag a rectangle",
                    MapEditorTool.Select => "Select a room to edit its properties",
                    MapEditorTool.Dropper => "Pick a room to paint with",
                    MapEditorTool.Delete => "Delete room assignments — drag a line; Shift-drag a rectangle",
                    _ => "Choose Construct, Select, Dropper or Delete for rooms",
                };
                return;
            }
            if (CurrentTool == MapEditorTool.Delete)
            {
                string label = MapEditorSubTabs.LabelOf(CurrentSubcategory);
                SelectedObjectHint = MapEditorDeleteTargeting.ScopeOf(CurrentSubcategory) == MapEditorDeleteScope.WallMountFace
                    ? $"Delete {label} — R to change face"
                    : $"Delete {label} — drag a line, Shift+drag a rectangle";
                return;
            }

            if (CurrentTool == MapEditorTool.Edit && SelectedAsset != null)
            {
                SelectedObjectHint = HoldsFamily ? PlacementHint() + " — V: variant" : PlacementHint();
                return;
            }

            if (CurrentTool == MapEditorTool.Select)
            {
                SelectedObjectHint = "Select tool active — click a tile to inspect";
                return;
            }

            if (CurrentTool == MapEditorTool.Move)
            {
                SelectedObjectHint = "Move tool active — click and drag an object";
                return;
            }

            if (CurrentTool == MapEditorTool.Dropper)
            {
                SelectedObjectHint = "Dropper tool active — click a placed object to copy it";
                return;
            }

            SelectedObjectHint = SelectedAsset != null
                ? "Select the Edit tool to place this object"
                : "Choose an object from the library";
        }
    }
}
