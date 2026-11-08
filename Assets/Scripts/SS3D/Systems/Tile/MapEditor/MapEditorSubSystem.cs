using Coimbra.Services.Events;
using Cysharp.Threading.Tasks;
using Coimbra.Services.PlayerLoopEvents;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using SS3D.Rendering.URP;
using SS3D.Systems.Area;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Electricity;
using SS3D.Systems.Entities;
using SS3D.Systems.Examine;
using SS3D.Systems.Furniture.Disposal;
using SS3D.Systems.Inputs;
using SS3D.Systems.Rounds;
using SS3D.Systems.Screens;
using SS3D.Systems.ScreenEffects;
using SS3D.Systems.Persistence;
using SS3D.Systems.Tile.MapEditor.Commands;
using SS3D.Systems.Tile.FloorVisuals;
using SS3D.Systems.Tile.MapEditor.Persistence;
using SS3D.Systems.Tile.MapEditor.UI;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.TileMapCreator;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Full-screen in-game map editor replacing the legacy TileMap Creator UI.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed partial class MapEditorSubSystem : NetworkSubSystem, IMapEditorHost
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private StyleSheet _styleSheet;
        [SerializeField] private MapEditorCatalogSo _catalogAsset;
        [SerializeField] private MapEditorIconsSo _icons;
        [SerializeField] private ConstructionHologramManager _hologramManager;

        private readonly MapEditorViewModel _viewModel = new();
        private MapEditorGameplayHud _gameplayHud;
        private readonly MapEditorCatalog _catalog = new();
        private readonly MapEditorSession _session = new();
        private readonly MapEditorCommandService _commandService = new();
        private readonly MapEditorRateLimiter _rateLimiter = new();
        private readonly MapEditorLighting _lighting = new();
        private readonly MapEditorAreaOverlay _areaOverlay = new();

        private MapEditorView _view;
        private IMapEditorPersistence _persistence;
        private InputSubSystem _inputSystem;
        private Controls.TileCreatorActions _controls;
        private ExamineSubSystem _examine;
        private TileSubSystem _tileSystem;
        private CameraFollow _cameraFollow;
        private Camera _playerCamera;
        private IInputHandle _mapEditorHandle;
        private VisualElement _overlayRoot;
        private bool _active;
        private bool _lastTabRestored;
        /// <summary>OnStart finished; OnDestroyed may run first when a scene unloads mid-load.</summary>
        private bool _started;
        /// <summary>Unscaled time at which the pending Map Check runs after an edit; negative when none is pending.</summary>
        private float _validationDueTime = -1f;
        /// <summary>Quiet period after an edit before re-validating, so drags and bursts of edits trigger one check.</summary>
        private const float ValidationDebounceSeconds = 0.4f;
        private bool _hadWorldSimsSuspend;
        private bool _restoredAtmosPaused;
        private bool _mouseOverUI;
        private bool _documentRegistered;
        private float _toastTimer;
        private Vector3? _moveSource;
        private string _moveAssetName;
        private Direction _moveDirection;
        private bool _moveIsItem;
        /// <summary>Last map name successfully saved or loaded this session — target for Ctrl+S.</summary>
        private string _lastQuickSaveName;

        /// <summary>Fired after the editor opens (Main HUD listens to suppress chrome).</summary>
        public event Action EditorOpened;

        /// <summary>Fired after the editor closes.</summary>
        public event Action EditorClosed;

        public bool IsActive => _active;
        public bool HasUnsavedChanges => _commandService.HasUnsavedChanges;
        public long EditRevision => _commandService.Revision;
        public string EditingTemplateName { get; private set; }
        private PersistenceSubSystem _templatePersistence;

        private void TemplateSaved(string name)
        {
            EditingTemplateName = name;
            _commandService.MarkSaved();
        }

        private void TemplateLoaded(string name)
        {
            EditingTemplateName = name;
            _commandService.ClearHistory();
            _inspectedRooms.Clear();
        }

        /// <summary>
        /// Live pointer-over-UI via <see cref="InputInterface"/> (registered UITK + uGUI).
        /// Cached <see cref="_mouseOverUI"/> is only for the CSS hover class.
        /// </summary>
        public bool MouseOverUI => _active && InputInterface.IsPointerOverInterface();

        public bool IsOrbiting => _session.IsOrbiting;

        /// <summary>True for the Delete tool, which erases whatever the open sub-tab's delete scope covers.</summary>
        public bool IsDeleting => _viewModel.CurrentTool == MapEditorTool.Delete;
        public MapEditorTool CurrentTool => _viewModel.CurrentTool;
        public bool IsRoomCategory => _viewModel.IsRoomCategory;
        public MapEditorMode CurrentMode => _viewModel.CurrentMode;
        public MapEditorSubcategory CurrentSubcategory => _viewModel.CurrentSubcategory;

        /// <summary>Client-local toast (placement blocked, delete miss, etc.).</summary>
        public void ShowLocalToast(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            _viewModel.ShowToast(message);
            _toastTimer = 1.6f;
        }

        public void SetPlacementHint(string hint)
        {
            if (_viewModel.SelectedObjectHint == hint)
                return;

            _viewModel.SelectedObjectHint = hint;
            // Hover feedback changes often; update the hint bar only, not the whole chrome.
            _view?.RefreshHint();
        }

        /// <summary>Drop a transient hover message and show the tool/asset default hint again.</summary>
        public void RestoreDefaultPlacementHint()
        {
            string previous = _viewModel.SelectedObjectHint;
            _viewModel.RefreshHint();
            if (_viewModel.SelectedObjectHint != previous)
                _view?.RefreshHint();
        }
        public Camera PickCamera => _playerCamera != null ? _playerCamera : Camera.main;

        protected override void OnAwake()
        {
            base.OnAwake();
            if (_document == null)
                _document = GetComponent<UIDocument>();
        }

        protected override void OnStart()
        {
            base.OnStart();
            _inputSystem = SubSystems.Get<InputSubSystem>();
            _tileSystem = SubSystems.Get<TileSubSystem>();
            _controls = _inputSystem.Inputs.TileCreator;
            _persistence = new MapEditorLocalPersistence(_tileSystem);
            _templatePersistence = SubSystems.Get<PersistenceSubSystem>();
            _templatePersistence.StationTemplateSaved += TemplateSaved;
            _templatePersistence.StationTemplateLoaded += TemplateLoaded;
            _gameplayHud = new MapEditorGameplayHud(transform);

            // TileCreator.ToggleMenu is always enabled by the Global context.
            _controls.ToggleMenu.performed += HandleToggleMenu;
            EnsureCommandServiceBound();

            ResolvePlayerCamera();
            _started = true;
        }

        /// <summary>
        /// Forgets the template identity and edit history once the station they belong to is gone
        /// (round stop unloads it), so the next round's map doesn't inherit stale unsaved changes.
        /// </summary>
        [Server]
        public void ResetAuthoringState()
        {
            EditingTemplateName = null;
            _commandService.ClearHistory();
            _inspectedRooms.Clear();
        }

        private void ResolvePlayerCamera()
        {
            if (SubSystems.TryGet(out CameraSubSystem cameraSystem) && cameraSystem.PlayerCamera != null)
                _playerCamera = cameraSystem.PlayerCamera.GetComponent<Camera>();

            if (_playerCamera == null)
                _playerCamera = Camera.main;

            if (_playerCamera != null)
                _cameraFollow = _playerCamera.GetComponent<CameraFollow>();
        }

        private void EnsureCommandServiceBound()
        {
            if (!IsServer || _tileSystem?.CurrentMap == null || _tileSystem.Loader == null)
                return;

            _commandService.Bind(
                _tileSystem.CurrentMap,
                _tileSystem.Loader,
                new ConstructionService(_tileSystem.CurrentMap, _tileSystem.QueryService),
                () => _tileSystem.SyncFloorDecalsToClients(),
                _tileSystem.SpawnPoints,
                () => MapMarkerEditorView.RefreshAll(),
                _tileSystem.GasMarkers);
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        protected override void OnDestroyed()
        {
            if (_templatePersistence != null)
            {
                _templatePersistence.StationTemplateSaved -= TemplateSaved;
                _templatePersistence.StationTemplateLoaded -= TemplateLoaded;
            }
            // OnStart may not have run when the scene unloads mid-load (a client disconnecting).
            if (_started)
                _controls.ToggleMenu.performed -= HandleToggleMenu;
            SetGameplayInputBlocked(false);
            _lighting.Restore();
            _gameplayHud?.SetVisible(true);
            _mapEditorHandle?.Dispose();
            _mapEditorHandle = null;
            _areaOverlay.Dispose();
            TeardownView();
            UnregisterDocument();
            if (_document != null)
                _document.enabled = false;
            if (_active)
            {
                _active = false;
                // Static render suppression would survive a scene unload; never leave it latched.
                ApplyEditorVisualSuppression(false);
                EditorClosed?.Invoke();
            }

            base.OnDestroyed();
        }

        private void BuildView()
        {
            if (_document == null || _document.rootVisualElement == null)
                return;

            _document.rootVisualElement.pickingMode = PickingMode.Ignore;

            _overlayRoot = _document.rootVisualElement.Q<VisualElement>("overlay-root") ??
                           _document.rootVisualElement;
            _overlayRoot.pickingMode = PickingMode.Ignore;

            _view = new MapEditorView(_styleSheet, _icons, _viewModel, _catalog, _tileSystem.Loader);
            _view.Build(_overlayRoot);
            WireViewEvents();
            RebuildCatalog();
            RegisterDocument();
        }

        private void RegisterDocument()
        {
            if (_documentRegistered || _document == null)
                return;

            InputInterface.RegisterDocument(_document);
            _documentRegistered = true;
        }

        private void UnregisterDocument()
        {
            if (!_documentRegistered || _document == null)
                return;

            InputInterface.UnregisterDocument(_document);
            _documentRegistered = false;
        }

        private void WireViewEvents()
        {
            _view.ValidationLocateRequested += OnValidationLocateRequested;
            _view.ValidationShowInPaletteRequested += OnValidationShowInPaletteRequested;
            _view.ToolSelected += OnToolSelected;
            _view.AreaApplyRequested += OnAreaApplyRequested;
            _view.GasMarkerApplyRequested += OnGasMarkerApplyRequested;
            _view.RoomPaletteSelected += OnRoomPaletteSelected;
            _view.UndoRequested += OnUndoRequested;
            _view.RedoRequested += OnRedoRequested;
            _view.SaveAsRequested += OnSaveAsRequested;
            _view.ExportRequested += OnExportRequested;
            _view.OpenMapsFolderRequested += OnOpenMapsFolderRequested;
            _view.LoadMapRequested += OnLoadMapRequested;
            _view.DeleteMapRequested += OnDeleteMapRequested;
            _view.NewMapRequested += OnNewMapRequested;
            _view.MapListRefreshRequested += RefreshMapList;
            _view.ResetViewRequested += OnResetViewRequested;
            _view.ShowUIRequested += OnShowUiRequested;
            _view.DebugOverlayChanged += OnDebugOverlayChanged;
            _view.AlwaysPowerLightsChanged += OnAlwaysPowerLightsChanged;
            _view.InvincibleChanged += OnInvincibleChanged;
            _view.CameraSettingsChanged += OnCameraSettingsChanged;
            _view.CameraRotateRequested += OnCameraRotateRequested;
            _view.CameraZoomRequested += OnCameraZoomRequested;
            _view.LayerCategoryVisibilityChanged += OnLayerCategoryVisibilityChanged;
            _view.ModeSelected += OnModeSelected;
            _view.SubcategorySelected += OnSubcategorySelected;
            _view.SearchChanged += OnSearchChanged;
            _view.AssetSelected += OnPaletteEntryPicked;
        }

        private void RebuildCatalog()
        {
            if (_tileSystem?.Loader == null)
                return;

            _catalog.Build(_tileSystem.Loader.Assets, _catalogAsset);
            _viewModel.Catalog = _catalog;
            _viewModel.NotifyChanged();
        }

        private void HandleToggleMenu(InputAction.CallbackContext context)
        {
            if (_active)
                SetActive(false);
            else if (CanOpenEditor())
                SetActive(true);
        }

        /// <summary>
        /// The editor takes over the in-world player camera, so it only opens while a round is
        /// running and the local player has a body. From the lobby or before a round starts there
        /// is no station to edit and the camera has nowhere sensible to be.
        /// </summary>
        private static bool CanOpenEditor()
        {
            if (!SubSystems.TryGet(out RoundSubSystem rounds) || rounds.CurrentRoundState != RoundState.Ongoing)
                return false;

            NetworkConnection local = InstanceFinder.ClientManager != null ? InstanceFinder.ClientManager.Connection : null;
            return local != null
                && SubSystems.TryGet(out EntitySubSystem entities)
                && entities.IsPlayerSpawned(local);
        }

        /// <summary>The first open of a session reopens the tab used last on this machine (creative-mode §2).</summary>
        private void RestoreLastTab()
        {
            if (_lastTabRestored)
                return;

            _lastTabRestored = true;
            _viewModel.OpenSubTab(MapEditorLastTab.Load(_viewModel.IsSubTabShown));
        }

        private void SetActive(bool active)
        {
            _active = active;

            if (active)
            {
                _viewModel.HideUI = false;
                EnableDocument();
                RebuildCatalog();
                RestoreLastTab();
                EnsureCommandServiceBound();
                _viewModel.EnsureLayerDefaults();
                MapEditorLayerVisibility.Activate();
                MapEditorLayerVisibility.Apply(_viewModel);

                _mapEditorHandle ??= _inputSystem.PushContext(InputContext.MapEditor);

                // Stop CameraFollow BEFORE taking the camera — its LateUpdateEvent still fires
                // while disabled unless it early-outs, and it was overwriting orbit every frame.
                SetGameplayInputBlocked(true);

                if (_playerCamera == null)
                    ResolvePlayerCamera();

                Vector3 entry = _playerCamera != null ? _playerCamera.transform.position : Vector3.zero;
                if (_session.IsActive)
                    _session.Exit();
                _session.Enter(_playerCamera, entry);

                _hologramManager.enabled = true;
                OnToolSelected(MapEditorTool.Edit);
                OnCameraSettingsChanged(_viewModel.CameraFov, _viewModel.CameraZoomSpeed, _viewModel.CameraRotationSpeed);
                _lighting.Apply();
                SetMouseOverUI(false);
                _gameplayHud.SetVisible(false);
                MapMarkerEditorView.EnsureExists().SetEditorOpen(true);
                BeginWorldSimsSuspend();
                ApplyEditorVisualSuppression(true);
                RefreshTileHostVisibility();
                EditorOpened?.Invoke();
                RpcRequestUndoState(LocalConnection);
                ApplySessionSettingsOnOpen();
                RequestValidation();
            }
            else
            {
                _gameplayHud.SetVisible(true);
                MapMarkerEditorView.EnsureExists().SetEditorOpen(false);
                _hologramManager.DestroyHolograms();
                _hologramManager.enabled = false;
                _session.Exit();
                _lighting.Restore();
                CancelAreaStroke();
                _areaOverlay.SetVisible(false);
                MapEditorLayerVisibility.Deactivate();
                SetGameplayInputBlocked(false);
                _mapEditorHandle?.Dispose();
                _mapEditorHandle = null;
                ShutdownDocument();
                EndWorldSimsSuspend();
                ApplyEditorVisualSuppression(false);
                // IsActive is already false — re-apply HashGrid AOI host visibility.
                RefreshTileHostVisibility();
                ReleaseCursorLabel();
                EditorClosed?.Invoke();
            }
        }

        /// <summary>
        /// The editor camera is not the player's in-body camera: hide the health/damage overlays and
        /// the animated atmos volume/wind streaks while authoring, then restore them on close.
        /// </summary>
        private static void ApplyEditorVisualSuppression(bool suppressed)
        {
            if (SubSystems.TryGet(out ScreenEffectsSubSystem screenEffects))
            {
                screenEffects.SetGameplayFeedbackSuppressed(suppressed);
            }

            AtmosRenderContext.SetRenderingSuppressed(suppressed);
        }

        /// <summary>
        /// Full-station authoring keeps MeshRenderers on; pause per-tick world sims so Metastation-scale
        /// maps stay interactive in the editor.
        /// </summary>
        private void BeginWorldSimsSuspend()
        {
            if (_hadWorldSimsSuspend)
                return;

            _hadWorldSimsSuspend = true;
            if (SubSystems.TryGet(out AtmosSubSystem atmos))
            {
                _restoredAtmosPaused = atmos.SimulationPaused;
                atmos.SimulationPaused = true;
            }

            if (SubSystems.TryGet(out ElectricitySubSystem electricity))
                electricity.SuspendCircuitUpdates(true);

            if (SubSystems.TryGet(out DisposalSubSystem disposal))
                disposal.CapsulesPaused = true;
        }

        private void EndWorldSimsSuspend()
        {
            if (!_hadWorldSimsSuspend)
                return;

            _hadWorldSimsSuspend = false;
            if (SubSystems.TryGet(out AtmosSubSystem atmos))
                atmos.SimulationPaused = _restoredAtmosPaused;

            if (SubSystems.TryGet(out ElectricitySubSystem electricity))
                electricity.SuspendCircuitUpdates(false);

            if (SubSystems.TryGet(out DisposalSubSystem disposal))
                disposal.CapsulesPaused = false;
        }

        /// <summary>
        /// Map Editor free-fly bypasses HashGrid AOI for host MeshRenderers; leaving restores AOI.
        /// </summary>
        private void RefreshTileHostVisibility()
        {
            if (_tileSystem?.CurrentMap == null)
                return;

            _tileSystem.CurrentMap.RefreshAllHostVisibility();
        }

        /// <summary>
        /// Disables the normal follow camera while the Map Editor's own free-fly session is active.
        /// Movement/Camera/Hotkeys action maps are masked separately by <see cref="InputContext.MapEditor"/>.
        /// </summary>
        private void SetGameplayInputBlocked(bool blocked)
        {
            if (_cameraFollow != null)
                _cameraFollow.enabled = !blocked;
        }

        private void HandleAssetSelected(MapEditorCatalogEntry entry, GenericObjectSo asset)
        {
            if (entry == null)
                return;

            _viewModel.SelectEntry(entry, asset);

            if (MapEditorFloorDecalCatalog.TryDecode(entry.AssetName, out ushort decalId) &&
                FloorDecalCatalog.Get()?.TryGet(decalId, out FloorDecalDefinition definition) == true &&
                _viewModel.CurrentTool == MapEditorTool.Edit)
            {
                _hologramManager.SetSelectedFloorDecal(definition);
                return;
            }

            if (MapEditorSpawnCatalog.IsSpawnKey(entry.AssetName) &&
                _viewModel.CurrentTool == MapEditorTool.Edit)
            {
                _hologramManager.SetSelectedMarker(entry.AssetName);
                return;
            }

            if (MapEditorGasMarkerCatalog.TryDecode(entry.AssetName, out GasMarkerKind gasKind) &&
                _viewModel.CurrentTool == MapEditorTool.Edit)
            {
                SelectGasMarkerKind(gasKind);
                return;
            }

            if (asset != null && _viewModel.CurrentTool == MapEditorTool.Edit)
                _hologramManager.SetSelectedObject(asset);
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (!_active)
                return;

            // Round ended (or the body went away) while editing: drop back out instead of leaving
            // the editor driving a camera over a station that is being torn down.
            if (!CanOpenEditor())
            {
                SetActive(false);
                return;
            }

            bool showAreas = _viewModel.IsLayerCategoryVisible(TileLayerCategory.Areas);
            _areaOverlay.SetVisible(showAreas);
            SubSystems.TryGet(out AreaSubSystem labelAreas);
            _view?.UpdateAreaLabels(_areaOverlay.LabelPositions, labelAreas?.FloorVisualCache, PickCamera, showAreas);

            UpdatePendingValidation();

            if (_session.IsActive)
                _session.Update(updateEvent.DeltaTime);

            // While orbiting, skip UI hover / tool clicks — placement picks are frozen too.
            if (!_session.IsOrbiting)
            {
                UpdateMouseOverUI();

                if (IsRoomCategory)
                    HandleAreaInput();

                if (!IsRoomCategory && _viewModel.CurrentTool == MapEditorTool.Select && _controls.Place.WasPerformedThisFrame() && !MouseOverUI)
                    HandleSelectClick();

                if (!IsRoomCategory && _viewModel.CurrentTool == MapEditorTool.Dropper && _controls.Place.WasPerformedThisFrame() && !MouseOverUI)
                    HandleDropperClick();

                if (!IsRoomCategory && _viewModel.CurrentTool == MapEditorTool.Move && !MouseOverUI)
                    HandleMoveInput();
            }
            else
            {
                CancelAreaStroke();
                SetMouseOverUI(false);
            }

            SyncCursorLabel();
            if (Keyboard.current != null)
            {
                if (Keyboard.current.f7Key.wasPressedThisFrame)
                {
                    _viewModel.HideUI = !_viewModel.HideUI;
                    _viewModel.NotifyChanged();
                }

                if (!InputInterface.IsCapturingText)
                {
                    HandleToolHotkeys();
                    HandleVariantHotkey();

                    if (IsChordModifierPressed(Keyboard.current) && Keyboard.current.zKey.wasPressedThisFrame)
                        RpcUndo(LocalConnection);
                    if (IsChordModifierPressed(Keyboard.current) && Keyboard.current.yKey.wasPressedThisFrame)
                        RpcRedo(LocalConnection);
                }

                // File shortcuts work even while a search/save field is focused.
                // Ctrl+Shift+O — Unity Editor steals plain Ctrl+O for File/Open Scene (read-only profile).
                if (IsChordModifierPressed(Keyboard.current)
                    && Keyboard.current.shiftKey.isPressed
                    && Keyboard.current.oKey.wasPressedThisFrame)
                    HandleOpenMapHotkey();
                if (IsChordModifierPressed(Keyboard.current)
                    && !Keyboard.current.shiftKey.isPressed
                    && Keyboard.current.sKey.wasPressedThisFrame)
                    HandleQuickSaveHotkey();
            }

            if (_toastTimer > 0f)
            {
                _toastTimer -= updateEvent.DeltaTime;
                if (_toastTimer <= 0f)
                    _viewModel.ClearToast();
            }

            // Do NOT RefreshMapList every frame — PopulateLoadList destroys/recreates Load/Del
            // buttons, so pointer-down never meets pointer-up on the same element (New Map
            // worked because it is built once in BuildMapsPopover). Refresh on open / after save/delete.
        }

        private void RefreshMapList() => _view?.PopulateMapList(_persistence.ListMaps());

        private void HandleSelectClick()
        {
            if (TryPickGasMarker(MapEditorTool.Select))
                return;

            if (!MapEditorCursorPick.TryPick(PickCamera, _tileSystem.CurrentMap, _viewModel,
                    out PlacedTileObject placed, out PlacedItemObject item))
                return;

            if (placed != null)
            {
                GenericObjectSo asset = _tileSystem.GetAsset(placed.NameString);
                _viewModel.SelectEntry(new MapEditorCatalogEntry { AssetName = placed.NameString }, asset);
                _viewModel.ShowToast($"Selected {placed.NameString} on {placed.Layer}");
                _toastTimer = 1.6f;
                return;
            }

            GenericObjectSo itemAsset = _tileSystem.GetAsset(item.NameString);
            _viewModel.SelectEntry(new MapEditorCatalogEntry { AssetName = item.NameString }, itemAsset);
            _viewModel.ShowToast($"Selected item {item.NameString}");
            _toastTimer = 1.6f;
        }

        /// <summary>
        /// Copies whatever is under the cursor into the active library selection and switches to
        /// Edit so the user can immediately place more of it.
        /// </summary>
        private void HandleDropperClick()
        {
            if (TryPickGasMarker(MapEditorTool.Dropper))
                return;

            if (!MapEditorCursorPick.TryPick(PickCamera, _tileSystem.CurrentMap, _viewModel,
                    out PlacedTileObject placed, out PlacedItemObject item))
                return;

            CopyIntoActiveSelection(placed != null ? placed.NameString : item.NameString);
        }

        private void CopyIntoActiveSelection(string assetName)
        {
            GenericObjectSo asset = _tileSystem.GetAsset(assetName);
            _viewModel.SetTool(MapEditorTool.Edit);
            SelectAsset(new MapEditorCatalogEntry { AssetName = assetName }, asset);
            _viewModel.ShowToast($"Copied {assetName}");
            _toastTimer = 1.6f;
        }

        private void HandleMoveInput()
        {
            if (_controls.Place.WasPerformedThisFrame())
            {
                if (_moveSource == null)
                    TryBeginMove();
                else
                    TryCompleteMove();
            }
        }

        private void TryBeginMove()
        {
            if (!MapEditorCursorPick.TryPick(PickCamera, _tileSystem.CurrentMap, _viewModel,
                    out PlacedTileObject placed, out PlacedItemObject item))
                return;

            if (placed != null)
            {
                _moveSource = new Vector3(placed.WorldOrigin.x, 0f, placed.WorldOrigin.y);
                _moveAssetName = placed.NameString;
                _moveDirection = placed.Direction;
                _moveIsItem = false;
                _viewModel.ShowToast($"Moving {placed.NameString} — click destination");
                _toastTimer = 2f;
                return;
            }

            _moveSource = item.transform.position;
            _moveAssetName = item.NameString;
            _moveDirection = Direction.North;
            _moveIsItem = true;
            _viewModel.ShowToast($"Moving item {item.NameString} — click destination");
            _toastTimer = 2f;
        }

        private void TryCompleteMove()
        {
            if (_moveSource == null)
                return;

            Vector3 destination = TileHelper.GetPointedPosition(!_moveIsItem, PickCamera);
            RpcMoveObject(_moveAssetName, _moveSource.Value, destination, _moveDirection, _moveIsItem, LocalConnection);
            _moveSource = null;
            _moveAssetName = null;
        }

        /// <summary>
        /// The examine badge by the cursor names what Edit is about to place ("Office Chair (Blue)", the room being
        /// painted) instead of examining the object under the pointer, which is rarely what you're building with.
        /// Other tools, the pointer over the editor's UI, and F7's hidden UI show no badge.
        /// </summary>
        private void SyncCursorLabel()
        {
            if (_examine == null && !SubSystems.TryGet(out _examine))
                return;

            string label = string.Empty;
            if (!_viewModel.HideUI && !_mouseOverUI && _viewModel.CurrentTool == MapEditorTool.Edit)
            {
                if (IsRoomCategory)
                    label = _viewModel.AreaRoomName ?? string.Empty;
                else if (_viewModel.SelectedEntry != null)
                    label = PlacingLabel(_viewModel.SelectedEntry);
            }

            _examine.SetCursorLabelOverride(label);
        }

        // One string per held entry, not one per frame.
        private MapEditorCatalogEntry _placingLabelEntry;
        private string _placingLabel;

        private string PlacingLabel(MapEditorCatalogEntry entry)
        {
            if (!ReferenceEquals(entry, _placingLabelEntry))
            {
                _placingLabelEntry = entry;
                _placingLabel = MapEditorCatalog.PlacingLabel(entry);
            }

            return _placingLabel;
        }

        private void ReleaseCursorLabel()
        {
            if (_examine != null || SubSystems.TryGet(out _examine))
                _examine.SetCursorLabelOverride(null);
        }

        private void UpdateMouseOverUI()
        {
            bool over = InputInterface.IsPointerOverInterface();
            if (over != _mouseOverUI)
                SetMouseOverUI(over);
        }

        private void SetMouseOverUI(bool over)
        {
            _mouseOverUI = over;
            _view?.SetMouseOverUI(over);
        }

        private void EnableDocument()
        {
            if (_document == null)
                return;

            if (!_document.enabled)
                _document.enabled = true;

            if (_view == null || _view.Root?.panel == null)
            {
                TeardownView();
                BuildView();
            }

            if (_view?.Root != null)
                _view.Root.style.display = DisplayStyle.Flex;

            RegisterDocument();
            _viewModel.NotifyChanged();
        }

        private void ShutdownDocument()
        {
            if (_view?.Root != null)
                _view.Root.style.display = DisplayStyle.None;
        }

        private void TeardownView()
        {
            if (_view == null)
                return;

            UnwireViewEvents();
            _view.Destroy();
            _view = null;
        }

        private void UnwireViewEvents()
        {
            if (_view == null)
                return;

            _view.ValidationLocateRequested -= OnValidationLocateRequested;
            _view.ValidationShowInPaletteRequested -= OnValidationShowInPaletteRequested;
            _view.ToolSelected -= OnToolSelected;
            _view.AreaApplyRequested -= OnAreaApplyRequested;
            _view.GasMarkerApplyRequested -= OnGasMarkerApplyRequested;
            _view.RoomPaletteSelected -= OnRoomPaletteSelected;
            _view.UndoRequested -= OnUndoRequested;
            _view.RedoRequested -= OnRedoRequested;
            _view.SaveAsRequested -= OnSaveAsRequested;
            _view.ExportRequested -= OnExportRequested;
            _view.OpenMapsFolderRequested -= OnOpenMapsFolderRequested;
            _view.LoadMapRequested -= OnLoadMapRequested;
            _view.DeleteMapRequested -= OnDeleteMapRequested;
            _view.NewMapRequested -= OnNewMapRequested;
            _view.MapListRefreshRequested -= RefreshMapList;
            _view.ResetViewRequested -= OnResetViewRequested;
            _view.ShowUIRequested -= OnShowUiRequested;
            _view.DebugOverlayChanged -= OnDebugOverlayChanged;
            _view.AlwaysPowerLightsChanged -= OnAlwaysPowerLightsChanged;
            _view.InvincibleChanged -= OnInvincibleChanged;
            _view.CameraSettingsChanged -= OnCameraSettingsChanged;
            _view.CameraRotateRequested -= OnCameraRotateRequested;
            _view.CameraZoomRequested -= OnCameraZoomRequested;
            _view.LayerCategoryVisibilityChanged -= OnLayerCategoryVisibilityChanged;
            _view.ModeSelected -= OnModeSelected;
            _view.SubcategorySelected -= OnSubcategorySelected;
            _view.SearchChanged -= OnSearchChanged;
            _view.AssetSelected -= OnPaletteEntryPicked;
        }

        private void OnToolSelected(MapEditorTool tool)
        {
            _view.CloseAreaProperties();
            CancelAreaStroke();
            _viewModel.SetTool(tool);
            if (IsRoomCategory)
            {
                _hologramManager.ClearSelection();
                return;
            }

            switch (tool)
            {
                case MapEditorTool.Delete:
                    _hologramManager.EnterDeleteMode();
                    break;
                case MapEditorTool.Edit:
                    RestoreConstructHologramFromSelection();
                    break;
                default:
                    // Select / Dropper / Move — placement ghosts must not linger.
                    _hologramManager.ClearSelection();
                    break;
            }
        }

        private void RestoreConstructHologramFromSelection()
        {
            MapEditorCatalogEntry entry = _viewModel.SelectedEntry;
            if (entry == null)
            {
                _hologramManager.ClearSelection();
                return;
            }

            HandleAssetSelected(entry, _viewModel.SelectedAsset);
        }

        /// <summary>
        /// V next variant, Shift+V previous. With no family held it does nothing;
        /// mid-drag it swaps the ghosts' prefab and keeps the drag, as R keeps it when rotating.
        /// </summary>
        private void HandleVariantHotkey()
        {
            if (!_controls.CycleVariant.WasPressedThisFrame() || !_viewModel.HoldsFamily)
                return;

            Keyboard keyboard = Keyboard.current;
            bool previous = keyboard != null && keyboard.shiftKey.isPressed;
            StepVariant(_catalog.SlotOf(_viewModel.SelectedEntry), previous ? -1 : 1);
        }

        private void StepVariant(MapEditorPaletteSlot slot, int step)
        {
            if (slot == null || !slot.IsFamily)
                return;

            MapEditorCatalogEntry next = slot.Step(_viewModel.SelectedEntry, step);
            _catalog.TryGetAsset(next.AssetName, _tileSystem.Loader, out GenericObjectSo asset);
            if (asset == null)
                return;

            _viewModel.SelectEntry(next, asset);
            if (_viewModel.CurrentTool == MapEditorTool.Edit)
                _hologramManager.SwapSelectedObject(asset);
        }

        private void HandleToolHotkeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || IsChordModifierPressed(keyboard) || keyboard.altKey.isPressed)
                return;

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                OnToolSelected(MapEditorTool.Edit);
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                OnToolSelected(MapEditorTool.Select);
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
                OnToolSelected(MapEditorTool.Dropper);
            else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
                OnToolSelected(MapEditorTool.Delete);
        }

        private static bool IsChordModifierPressed(Keyboard keyboard) =>
            keyboard != null
            && (keyboard.ctrlKey.isPressed
                || keyboard.leftCommandKey.isPressed
                || keyboard.rightCommandKey.isPressed);

        private void HandleOpenMapHotkey()
        {
            if (!_active || _view == null || RefuseLocalPersistence())
                return;

            _view.OpenPopover("maps");
            RefreshMapList();
        }

        private void HandleQuickSaveHotkey()
        {
            if (!_active || RefuseLocalPersistence())
                return;

            string name = ResolveQuickSaveName();
            if (string.IsNullOrWhiteSpace(name))
            {
                _view?.OpenPopover("saveMenu");
                RefreshMapList();
                return;
            }

            RpcSaveMap(name, overwrite: true, LocalConnection);
        }

        /// <summary>Client-side mirror of the server's persistence gate, for the file hotkeys.</summary>
        private bool RefuseLocalPersistence()
        {
            if (_viewModel.Capabilities.Persistence)
                return false;

            ShowLocalToast(MapEditorPermissions.PersistenceRefusedMessage);
            return true;
        }

        private string ResolveQuickSaveName()
        {
            if (!string.IsNullOrWhiteSpace(_lastQuickSaveName))
                return _lastQuickSaveName.Trim();

            string typed = _viewModel.SaveMapName?.Trim();
            if (!string.IsNullOrWhiteSpace(typed) &&
                !string.Equals(typed, "Untitled Map", StringComparison.OrdinalIgnoreCase))
                return typed;

            return null;
        }

        private void RememberQuickSaveName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            _lastQuickSaveName = name.Trim();
            _viewModel.SaveMapName = _lastQuickSaveName;
        }

        private void OnUndoRequested() => RpcUndo(LocalConnection);

        private void OnRedoRequested() => RpcRedo(LocalConnection);

        // The Save Map popover always shows an "Overwriting" warning inline when the typed name
        // collides with an existing save, so the Save button itself can safely always overwrite.
        private void OnSaveAsRequested(string name)
        {
            RpcSaveMap(name, true, LocalConnection);
        }

        private void OnExportRequested(string name)
        {
            _viewModel.ShowToast($"Exporting {name}…");
            _toastTimer = 1.6f;
            RpcExportMap(name, LocalConnection);
        }

        private void OnOpenMapsFolderRequested()
        {
            // Maps live on the server's disk; a remote client has no local folder to open.
            if (!IsServer)
            {
                _viewModel.ShowToast("Maps are stored on the server. Ask the host to add shared maps.");
                _toastTimer = 3f;
                return;
            }

            System.IO.Directory.CreateDirectory(SS3D.Data.Persistence.MapFileSharing.StationTemplatesFolder);
            SS3D.Data.Persistence.MapFileSharing.Reveal(SS3D.Data.Persistence.MapFileSharing.StationTemplatesFolder);
        }

        private void OnLoadMapRequested(string name)
        {
            ClearAreaSelection();
            // Immediate local feedback — TargetToast only runs if the ServerRpc auth path succeeds.
            _viewModel.ShowToast($"Loading {name}…");
            _toastTimer = 1.6f;

            if (IsServer)
            {
                ApplyLoadMap(name, LocalConnection);
                return;
            }

            RpcLoadMap(name, LocalConnection);
        }

        private void ApplyLoadMap(string mapName, NetworkConnection conn) => LoadEditorMapAsync(mapName, conn).Forget();

        private async UniTask LoadEditorMapAsync(string mapName, NetworkConnection conn)
        {
            if (!MapEditorPermissions.TryAuthorize(conn))
            {
                TargetToast(conn, "Not authorized to load maps.");
                return;
            }

            if (RefusePersistence(conn))
                return;

            Log.Information(this, "Map editor loading template {mapName}", Logs.Important, mapName);
            try
            {
                if (!await _templatePersistence.LoadStationTemplateAsync(mapName))
                {
                    TargetToast(conn, $"Could not load {mapName}. Your edits are still here.");
                    return;
                }
                _tileSystem.SyncFloorDecalsToClients();
                TargetRememberTemplate(conn, mapName);
                TargetSyncUndoState(conn, 0, 0, false);
                SendMapValidation(conn, true);
                string skipped = DescribeSkipped(_tileSystem.CurrentMap?.LastLoadSkipped);
                if (skipped == null)
                    TargetToast(conn, $"Loaded {mapName}.");
                else
                    TargetToastFor(conn, $"Loaded {mapName}, but this version doesn't have {skipped}. "
                        + "They were left out. Saving over the map makes that permanent; save under a new name to keep the original.", 10f);
            }
            catch (Exception exception)
            {
                Log.Error(this, "Map editor load failed: {error}", Logs.ServerOnly, exception.Message);
                TargetToast(conn, $"Could not finish loading {mapName}. Reload a saved template before editing.");
            }
        }

        private void OnDeleteMapRequested(string name) => RpcDeleteMap(name, LocalConnection);

        private void OnNewMapRequested()
        {
            ClearAreaSelection();
            _lastQuickSaveName = null;
            _viewModel.SaveMapName = "Untitled Map";
            RpcNewMap(LocalConnection);
        }

        private void OnResetViewRequested() => _session.ResetPosition();

        private void OnCameraRotateRequested(int direction) => _session.RotateStep(15f * direction);

        private void OnCameraZoomRequested(int direction) => _session.ZoomStep(2f * direction);

        private void OnShowUiRequested()
        {
            _viewModel.HideUI = false;
            _viewModel.NotifyChanged();
        }


        private void OnCameraSettingsChanged(float fov, float zoomSpeed, float rotationSpeed)
        {
            Camera cam = PickCamera;
            if (cam != null)
                cam.fieldOfView = fov;

            _session?.SetSpeeds(zoomSpeed, rotationSpeed);

            // Sliders are shown on a 1-10 scale; 5 is the neutral (1x) speed multiplier.
            // CameraFollow is disabled while the map editor is open; keep multipliers in sync
            // for when the editor closes.
            if (_cameraFollow != null)
            {
                _cameraFollow.ZoomSpeedMultiplier = zoomSpeed / 5f;
                _cameraFollow.RotationSpeedMultiplier = rotationSpeed / 5f;
            }
        }

        private void OnDebugOverlayChanged(bool value)
        {
            _viewModel.DebugOverlay = value;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AreaDevSettings.ShowAreaGizmos = value;
#endif
        }

        private void OnLayerCategoryVisibilityChanged(TileLayerCategory category, bool visible)
        {
            _viewModel.SetLayerCategoryVisible(category, visible);
            MapEditorLayerVisibility.Apply(_viewModel);
        }

        // Clicking a tab means browsing it, so it ends a palette-wide search.
        private void OnModeSelected(MapEditorMode mode)
        {
            _viewModel.SearchText = string.Empty;
            _viewModel.SetMode(mode);
            AfterSubTabChanged();
        }

        private void OnSubcategorySelected(MapEditorSubcategory sub)
        {
            _viewModel.SearchText = string.Empty;
            _viewModel.SetSubcategory(sub);
            AfterSubTabChanged();
        }

        /// <summary>Opens a sub-tab in any mode, as a search pick or Show in palette does.</summary>
        private void OpenSubTab(MapEditorSubcategory sub)
        {
            if (_viewModel.CurrentSubcategory == sub)
                return;

            _viewModel.OpenSubTab(sub);
            AfterSubTabChanged();
        }

        private void AfterSubTabChanged()
        {
            TileLayerVisibilityService.SetGroupVisible(TileLayerCategory.Scripts,
                _viewModel.IsLayerCategoryVisible(TileLayerCategory.Scripts));
            SyncTurfsLayerVisibility();
            OnToolSelected(_viewModel.CurrentTool);
            SyncHologramToLibrarySelection();
            MapEditorLastTab.Save(_viewModel.CurrentSubcategory);
        }

        /// <summary>
        /// A palette click. Search results come from every tab, so the entry's sub-tab opens first:
        /// delete scope, forms and hints all key off the open sub-tab.
        /// </summary>
        private void OnPaletteEntryPicked(MapEditorCatalogEntry entry, GenericObjectSo asset) => SelectAsset(entry, asset);

        /// <summary>
        /// Dropper, search and Show in palette all select through here: the asset's family
        /// slot shows it as the current variant, its sub-tab opens, and the ghost follows in Edit.
        /// </summary>
        private void SelectAsset(MapEditorCatalogEntry entry, GenericObjectSo asset)
        {
            if (entry == null)
                return;

            if (_viewModel.SelectAsset(entry, asset))
                AfterSubTabChanged();
            HandleAssetSelected(_viewModel.SelectedEntry, asset);
        }

        /// <summary>Map Check's Show in palette: search for the entry and select it in the sub-tab it lives in.</summary>
        private void OnValidationShowInPaletteRequested(string entryKey)
        {
            if (string.IsNullOrEmpty(entryKey) || !_catalog.TryGetEntry(entryKey, out MapEditorCatalogEntry entry))
            {
                _viewModel.ShowToast("That isn't in this build's palette.");
                return;
            }

            _viewModel.SearchText = MapEditorCatalog.SearchTermFor(entry.AssetName);
            GenericObjectSo asset = null;
            if (!MapEditorCatalog.IsVirtualKey(entry.AssetName))
                _catalog.TryGetAsset(entry.AssetName, _tileSystem.Loader, out asset);
            SelectAsset(entry, asset);
            _viewModel.NotifyChanged();
        }

        /// <summary>Lower run tabs (Cables, Gas Pipes, Disposal Pipes) toggle Turfs in the view model; only re-dim when it changed (walks the map).</summary>
        private void SyncTurfsLayerVisibility()
        {
            bool visible = _viewModel.IsLayerCategoryVisible(TileLayerCategory.Turfs);
            if (TileLayerVisibilityService.IsGroupVisible(TileLayerCategory.Turfs) != visible)
                TileLayerVisibilityService.SetGroupVisible(TileLayerCategory.Turfs, visible);
        }

        /// <summary>
        /// Subcategory/mode tabs should update the ghost immediately — delete used to keep the
        /// previous prefab until the cursor hovered a matching tile (wall mounts especially).
        /// </summary>
        private void SyncHologramToLibrarySelection()
        {
            if (IsRoomCategory) { _hologramManager.ClearSelection(); return; }
            if (IsDeleting)
            {
                _hologramManager.RefreshDeletePreview();
                return;
            }

            if (CurrentTool != MapEditorTool.Edit)
                return;

            TrySelectFirstAssetInCurrentSubcategory();
        }

        /// <summary>
        /// First placeable catalog entry for the active subcategory (skips floor-decal slots).
        /// Used as the Delete-tool ghost prototype when the cursor is not over a matching target.
        /// </summary>
        public bool TryGetSubcategoryPrototypeAssetName(MapEditorSubcategory subcategory, out string assetName)
        {
            assetName = null;
            if (_catalog == null)
                return false;

            // Only tile-layer scopes have a prefab to ghost; decals, markers, items and rooms don't.
            if (!MapEditorSubTabs.TryGet(subcategory, out MapEditorSubTab row) ||
                !MapEditorDeleteTargeting.IsTileScope(row.DeleteScope))
                return false;

            foreach (MapEditorCatalogEntry entry in _catalog.Query(row.Mode, subcategory, null))
            {
                if (MapEditorFloorDecalCatalog.TryDecode(entry.AssetName, out _))
                    continue;

                assetName = entry.AssetName;
                return !string.IsNullOrEmpty(assetName);
            }

            return false;
        }

        private void TrySelectFirstAssetInCurrentSubcategory()
        {
            MapEditorSubcategory sub = _viewModel.CurrentSubcategory;
            if (_viewModel.SelectedEntry != null &&
                _viewModel.SelectedEntry.Subcategory == sub)
            {
                // Already holding something from this tab — keep it, but refresh the ghost.
                HandleAssetSelected(_viewModel.SelectedEntry, _viewModel.SelectedAsset);
                return;
            }

            foreach (MapEditorCatalogEntry entry in _catalog.Query(_viewModel.CurrentMode, sub, _viewModel.SearchText))
            {
                GenericObjectSo asset = null;
                if (MapEditorFloorDecalCatalog.TryDecode(entry.AssetName, out _) ||
                    MapEditorSpawnCatalog.IsSpawnKey(entry.AssetName) ||
                    MapEditorGasMarkerCatalog.TryDecode(entry.AssetName, out _))
                {
                    HandleAssetSelected(entry, null);
                    return;
                }

                _catalog.TryGetAsset(entry.AssetName, _tileSystem.Loader, out asset);
                HandleAssetSelected(entry, asset);
                return;
            }

            _hologramManager.ClearSelection();
        }

        private void OnSearchChanged(string text)
        {
            _viewModel.SearchText = text;
            _viewModel.NotifyChanged();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcSaveMap(string mapName, bool overwrite, NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn) || RefusePersistence(conn))
                return;

            if (TrySaveFor(conn, mapName, overwrite, out bool hasWarnings))
                TargetToast(conn, hasWarnings ? $"Saved {mapName} with validation warnings." : $"Saved {mapName}.");
        }

        /// <summary>
        /// Saves the current edits under <paramref name="mapName"/>, then zips the template into
        /// <c>Data/Exports/</c> on the server's machine for sharing.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void RpcExportMap(string mapName, NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn) || RefusePersistence(conn))
                return;

            if (!TrySaveFor(conn, mapName, true, out _))
                return;

            if (!SS3D.Data.Persistence.MapFileSharing.TryExport(mapName, out string zipPath, out string error))
            {
                Log.Error(this, "Map export failed: {error}", Logs.ServerOnly, error);
                TargetToast(conn, $"Saved {mapName}, but could not export it: {error}");
                return;
            }

            Log.Information(this, "Exported map {map} to {path}", Logs.ServerOnly, mapName, zipPath);
            TargetMapExported(conn, mapName, zipPath);
        }

        [TargetRpc]
        private void TargetMapExported(NetworkConnection conn, string mapName, string zipPath)
        {
            // The zip lives on the server's disk. Only a host can open it on this machine.
            if (IsServer)
            {
                GUIUtility.systemCopyBuffer = zipPath;
                SS3D.Data.Persistence.MapFileSharing.Reveal(zipPath);
                _viewModel.ShowToast($"Exported {mapName}.zip. Drag it from the opened folder into Discord.");
                _toastTimer = 5f;
            }
            else
            {
                _viewModel.ShowToast($"Exported {mapName}.zip on the server (Data/Exports). Ask the host for it.");
                _toastTimer = 5f;
            }
        }

        private bool TrySaveFor(NetworkConnection conn, string mapName, bool overwrite, out bool hasWarnings)
        {
            hasWarnings = false;
            if (_persistence.Exists(mapName) && !MapEditorPermissions.MayReplaceMapFile(conn, mapName))
            {
                TargetToast(conn, $"{mapName} belongs to someone else on this server. Save under a new name.");
                return false;
            }

            try
            {
                if (!_persistence.Save(mapName, overwrite))
                {
                    TargetToast(conn, $"Could not save {mapName}. Your edits are still unsaved.");
                    return false;
                }
                MapEditorPermissions.RecordSave(conn, mapName);
                TargetRememberTemplate(conn, mapName);
                string mode = SubSystems.Get<SS3D.Systems.Rounds.RoundSubSystem>().SettingsForServer.Mode;
                Validation.MapValidationReport report = Validation.MapValidationService.Validate(_tileSystem, mode);
                TargetMapValidation(conn, report.Issues.ToArray(), mode, false);
                TargetRefreshMapList(conn);
                hasWarnings = report.HasIssues;
                return true;
            }
            catch (Exception exception)
            {
                Log.Error(this, "Map editor save failed: {error}", Logs.ServerOnly, exception.Message);
                TargetToast(conn, $"Could not save {mapName}. Your edits are still unsaved.");
                return false;
            }
        }

        private void RequestValidation()
        {
            _validationDueTime = -1f;
            RpcValidateMap(LocalConnection);
        }

        /// <summary>Runs the debounced Map Check once edits have been quiet for <see cref="ValidationDebounceSeconds"/>.</summary>
        private void UpdatePendingValidation()
        {
            if (_validationDueTime >= 0f && Time.unscaledTime >= _validationDueTime)
                RequestValidation();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcValidateMap(NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn))
                return;

            SendMapValidation(conn);
        }

        [Server]
        private void SendMapValidation(NetworkConnection conn, bool resetDismissed = false)
        {
            string mode = SubSystems.Get<RoundSubSystem>().SettingsForServer.Mode;
            Validation.MapValidationReport report = Validation.MapValidationService.Validate(_tileSystem, mode);
            TargetMapValidation(conn, report.Issues.ToArray(), mode, resetDismissed);
        }

        [TargetRpc]
        private void TargetMapValidation(NetworkConnection conn, Validation.MapValidationIssue[] issues, string mode, bool resetDismissed)
        {
            _viewModel.SetValidation(issues, mode, resetDismissed);
        }

        private void OnValidationLocateRequested(Validation.MapValidationIssue issue)
        {
            if (issue.HasPosition)
                _session.CenterOn(issue.Position);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcLoadMap(string mapName, NetworkConnection conn = null)
        {
            ApplyLoadMap(mapName, conn);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcDeleteMap(string mapName, NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn) || RefusePersistence(conn))
                return;

            if (!MapEditorPermissions.MayReplaceMapFile(conn, mapName))
            {
                TargetToast(conn, $"Only admins can delete {mapName} on this server.");
                return;
            }

            _persistence.Delete(mapName);
            TargetToast(conn, $"Deleted {mapName}.");
            TargetRefreshMapList(conn);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcNewMap(NetworkConnection conn = null)
        {
            // New Map replaces the station for everyone, so it sits behind the persistence gate with Load.
            if (!MapEditorPermissions.TryAuthorize(conn) || RefusePersistence(conn))
                return;

            if (_tileSystem?.CurrentMap == null)
                return;

            _tileSystem.CurrentMap.Clear();
            _tileSystem.ClearMarkers();
            _commandService.MarkNewMap();
            EditingTemplateName = null;
            _hologramManager.DestroyHolograms();
            MapMarkerEditorView.RefreshAll();
            TargetSyncUndoState(conn, 0, 0, false);
            SendMapValidation(conn, true);
            TargetToast(conn, "Started a new empty map.");
        }

        [TargetRpc]
        private void TargetRefreshMapList(NetworkConnection conn) => RefreshMapList();

        /// <summary>True (and the player told why) when map persistence is off on this server.</summary>
        [Server]
        private bool RefusePersistence(NetworkConnection conn)
        {
            if (MapEditorPermissions.TryAllowPersistence(out string refusal))
                return false;

            TargetToastFor(conn, refusal, 4f);
            return true;
        }

        /// <summary>
        /// Drops what a sandbox player may not build (<see cref="MapEditorSandboxRules"/>) and tells them once.
        /// Admins and the host keep everything.
        /// </summary>
        [Server]
        private MapEditorCommandDto[] FilterDeniedCommands(MapEditorCommandDto[] commands, NetworkConnection conn)
        {
            if (!MapEditorPermissions.IsSandboxPlayer(conn) || !commands.Any(MapEditorSandboxRules.IsDenied))
                return commands;

            Log.Warning(this, "Refused denied map edits from client {clientId}", Logs.ServerOnly, conn.ClientId);
            TargetToastFor(conn, MapEditorSandboxRules.DeniedMessage, 4f);
            return commands.Where(command => !MapEditorSandboxRules.IsDenied(command)).ToArray();
        }

        public void SubmitCommands(MapEditorCommandDto[] commands) =>
            RpcExecuteCommands(commands, LocalConnection);

        [ServerRpc(RequireOwnership = false)]
        private void RpcExecuteCommands(MapEditorCommandDto[] commands, NetworkConnection conn = null)
        {
            EnsureCommandServiceBound();
            if (!MapEditorPermissions.TryAuthorize(conn))
                return;

            if (commands == null || commands.Length == 0 || _commandService.Context == null)
                return;

            commands = FilterDeniedCommands(commands, conn);
            if (commands.Length == 0)
                return;

            MapEditorCommandContext ctx = _commandService.Context;
            List<IMapEditorCommand> list = new(commands.Length);
            foreach (MapEditorCommandDto dto in commands)
            {
                if (dto.Kind is MapEditorCommandKind.PlaceTile or MapEditorCommandKind.PlaceItem
                    or MapEditorCommandKind.SetFloorDecal or MapEditorCommandKind.PlaceSpawnPoint
                    or MapEditorCommandKind.PlaceGasMarker)
                {
                    MapEditorCommandFactory.TryCreatePlaceCommands(dto, ctx, list);
                    continue;
                }

                list.Add(MapEditorCommandFactory.FromDto(dto, ctx));
            }

            if (list.Count == 0)
                return;

            if (MapEditorPermissions.IsSandboxPlayer(conn)
                && !_rateLimiter.TryConsume(conn.ClientId, list.Count, Time.unscaledTimeAsDouble))
            {
                Log.Warning(this, "Throttled map edits from client {clientId} ({count} operations)",
                    Logs.ServerOnly, conn.ClientId, list.Count);
                TargetToast(conn, "Slow down: too many edits at once. Try again in a few seconds.");
                return;
            }

            _commandService.ExecuteCompound(list, conn.ClientId);
            TargetSyncUndoState(conn, _commandService.UndoDepth, _commandService.RedoDepth, true);
        }

        /// <summary>Sandbox players may only undo their own edits; admins and hosts undo anything.</summary>
        private static int UndoActor(NetworkConnection conn) =>
            MapEditorPermissions.IsSandboxPlayer(conn) ? conn.ClientId : MapEditorUndoStack.AnyOwner;

        [ServerRpc(RequireOwnership = false)]
        private void RpcUndo(NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn))
                return;

            if (!_commandService.Undo(UndoActor(conn)) && _commandService.UndoDepth > 0)
                TargetToast(conn, "Someone else edited since your last change, so there's nothing of yours to undo.");
            TargetSyncUndoState(conn, _commandService.UndoDepth, _commandService.RedoDepth, true);
            TargetRefreshAreaProperties(conn);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcRedo(NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn))
                return;

            if (!_commandService.Redo(UndoActor(conn)) && _commandService.RedoDepth > 0)
                TargetToast(conn, "The next redo is someone else's change.");
            TargetSyncUndoState(conn, _commandService.UndoDepth, _commandService.RedoDepth, true);
            TargetRefreshAreaProperties(conn);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcMoveObject(string assetName, Vector3 from, Vector3 to, Direction direction, bool isItem,
            NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn))
                return;

            if (MapEditorPermissions.IsSandboxPlayer(conn) && MapEditorSandboxRules.IsDeniedAsset(assetName))
            {
                TargetToastFor(conn, MapEditorSandboxRules.DeniedMessage, 4f);
                return;
            }

            IMapEditorCommand command = isItem
                ? new CompoundCommand(new IMapEditorCommand[]
                {
                    new ClearItemCommand(assetName, from, direction),
                    new PlaceItemCommand(assetName, to, direction),
                })
                : new MoveTileCommand(assetName, from, to, direction);

            _commandService.Execute(command, conn.ClientId);
            TargetSyncUndoState(conn, _commandService.UndoDepth, _commandService.RedoDepth, true);
            TargetToast(conn, "Moved object.");
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcRequestUndoState(NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn))
                return;

            TargetSyncUndoState(conn, _commandService.UndoDepth, _commandService.RedoDepth, false);
            TargetRememberTemplate(conn, EditingTemplateName);
            TargetCapabilities(conn, MapEditorPermissions.CapabilitiesFor(conn));
        }

        /// <summary>Hides what this player can't use: persistence, the invincibility setting, denied palette entries.</summary>
        [TargetRpc]
        private void TargetCapabilities(NetworkConnection conn, MapEditorCapabilities capabilities)
        {
            _catalog.HideSandboxDenied = capabilities.RestrictedPalette;
            if (!capabilities.Invincibility)
                _viewModel.Invincible = false;
            _viewModel.SetCapabilities(capabilities);
        }

        [TargetRpc]
        private void TargetRememberTemplate(NetworkConnection conn, string name)
        {
            _lastQuickSaveName = name;
            _viewModel.SaveMapName = string.IsNullOrEmpty(name) ? "Untitled Map" : name;
        }

        [TargetRpc]
        private void TargetSyncUndoState(NetworkConnection conn, int undoDepth, int redoDepth, bool mapChanged)
        {
            if (mapChanged)
            {
                _viewModel.ValidationStale = true;
                _validationDueTime = Time.unscaledTime + ValidationDebounceSeconds;
            }
            _viewModel.SetUndoState(undoDepth, redoDepth);
        }

        /// <summary>"3 × OldLamp, 1 × RemovedChair" for the load summary, or null when nothing was skipped.</summary>
        public static string DescribeSkipped(IReadOnlyDictionary<string, int> skipped)
        {
            if (skipped == null || skipped.Count == 0)
                return null;

            const int shown = 4;
            IEnumerable<string> parts = skipped
                .OrderByDescending(pair => pair.Value)
                .Take(shown)
                .Select(pair => $"{pair.Value} × {pair.Key}");
            string text = string.Join(", ", parts);
            return skipped.Count > shown ? $"{text} and {skipped.Count - shown} more kinds" : text;
        }

        [TargetRpc]
        private void TargetToastFor(NetworkConnection conn, string message, float seconds)
        {
            _viewModel.ShowToast(message);
            _toastTimer = seconds;
        }

        [TargetRpc]
        private void TargetToast(NetworkConnection conn, string message)
        {
            _viewModel.ShowToast(message);
            _toastTimer = 1.6f;
        }
    }
}
