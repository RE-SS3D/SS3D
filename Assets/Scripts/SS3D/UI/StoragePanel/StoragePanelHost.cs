using System.Collections.Generic;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Screens;
using SS3D.UI.MachineInterface;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.StoragePanel
{
    /// <summary>
    /// On-demand storage panel host (design doc: Documents/design/inventory-storage.md §6). Unlike
    /// MachineInterfaceHost (deliberately single-panel), this host manages any number of
    /// simultaneously open panels — backpack, a world locker, and a nested lockbox opened from that
    /// locker can all be visible side by side, per the imported "Looting Scene" mockup.
    /// <para>
    /// Self-bootstraps the same way MainHudSubSystem/ScreenEffectsSubSystem do rather than living on a
    /// scene/prefab GameObject — hand-editing scene/prefab YAML outside the Unity Editor isn't safe.
    /// </para>
    /// <para>
    /// Binds to the local player's ContainerViewer (replacing the old condemned ContainerView) so a
    /// panel opens/closes automatically whenever the server-authoritative open/close RPCs fire —
    /// whether that request came from this host (gear strip / world container click) or any other
    /// source.
    /// </para>
    /// <para>
    /// Observes <see cref="MachineInterfaceSubSystem"/> open/close like Main HUD and hides the panel
    /// layer while a machine UI is up (does not close panels — they return when MI closes).
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class StoragePanelHost : SubSystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SubSystems.TryGet(out StoragePanelHost _))
            {
                return;
            }

            GameObject host = new(nameof(StoragePanelHost));
            DontDestroyOnLoad(host);
            host.AddComponent<UIDocument>();
            host.AddComponent<StoragePanelHost>();
        }

        [SerializeField] private UIDocument _document;
        [SerializeField] private StyleSheet _storagePanelStyle;
        [SerializeField] private StyleSheet _inventorySlotStyle;

        private readonly Dictionary<AttachedContainer, StoragePanelView> _openPanels = new();
        private readonly Dictionary<AttachedContainer, Vector2> _pendingAnchors = new();
        private readonly Dictionary<AttachedContainer, string> _pendingBreadcrumbs = new();
        private readonly List<HudDropTarget> _hudDropTargets = new();

        private VisualElement _root;
        private HumanInventory _localInventory;
        private int _cascadeIndex;

        // Active drag state (panel slot and/or HUD-origin drag).
        private StoragePanelView _dragSourcePanel;
        private StorageSlot _dragSourceSlot;
        private Item _dragItem;
        private AttachedContainer _dragSourceContainer;
        private VisualElement _dragGhost;
        private Image _dragGhostIcon;
        private StorageSlot _highlightedSlot;
        private VisualElement _highlightedHudElement;

        private MachineInterfaceSubSystem _machineUi;
        private bool _subscribedToMachineUi;
        private bool _machineUiOpen;

        protected override void OnAwake()
        {
            base.OnAwake();

            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            if (!TryEnsureAssets())
            {
                enabled = false;
                return;
            }

            BuildRoot();
            InputInterface.RegisterDocument(_document);
        }

        protected override void OnStart()
        {
            base.OnStart();
            EnsureMachineUiSubscription();
        }

        private void Update()
        {
            // MI may bootstrap after this host — keep trying until subscribed (same as MainHudSubSystem).
            EnsureMachineUiSubscription();
        }

        protected override void OnDestroyed()
        {
            UnsubscribeMachineUi();
            InputInterface.UnregisterDocument(_document);
            base.OnDestroyed();
        }

        private bool TryEnsureAssets()
        {
            StoragePanelAssetCatalog catalog =
                Resources.Load<StoragePanelAssetCatalog>(StoragePanelAssetPaths.ResourcesCatalogName);
            if (catalog == null)
            {
#if UNITY_EDITOR
                EnsureEditorAssets();
                if (_storagePanelStyle != null)
                {
                    ApplyDocumentPanelSettings(null);
                    return true;
                }
#endif
                Debug.LogError(
                    $"StoragePanelHost could not load Resources/{StoragePanelAssetPaths.ResourcesCatalogName}. "
                    + "Run SS3D → Storage Panel → Rebuild Asset Catalog and commit the asset.",
                    this);
                return false;
            }

            if (!catalog.HasRequiredAssets(out string missingField))
            {
                Debug.LogError(
                    $"StoragePanelAssetCatalog is missing required assets ({missingField}). "
                    + "Run SS3D → Storage Panel → Rebuild Asset Catalog.",
                    this);
                return false;
            }

            _storagePanelStyle = catalog.StoragePanelStyle;
            _inventorySlotStyle = catalog.InventorySlotStyle;
            ApplyDocumentPanelSettings(catalog.PanelSettings);
            return true;
        }

        private void ApplyDocumentPanelSettings(PanelSettings panelSettings)
        {
            if (_document == null)
            {
                return;
            }

            if (_document.panelSettings == null && panelSettings != null)
            {
                _document.panelSettings = panelSettings;
            }

            // Above the persistent Main HUD (-10) so panels read as on top of it, same relative
            // ordering rule as the radial/armed overlays.
            _document.sortingOrder = 0;
        }

        private void BuildRoot()
        {
            _root = _document.rootVisualElement;
            _root.styleSheets.Add(_storagePanelStyle);
            _root.styleSheets.Add(_inventorySlotStyle);
            _root.style.position = UnityEngine.UIElements.Position.Absolute;
            _root.style.left = 0;
            _root.style.top = 0;
            _root.style.right = 0;
            _root.style.bottom = 0;
            _root.pickingMode = PickingMode.Ignore;
            ApplyMachineUiVisibility();
        }

        private void EnsureMachineUiSubscription()
        {
            if (_subscribedToMachineUi)
            {
                return;
            }

            if (!SubSystems.TryGet(out MachineInterfaceSubSystem machineUi))
            {
                return;
            }

            _machineUi = machineUi;
            _machineUi.InterfaceOpened += HandleMachineUiOpened;
            _machineUi.InterfaceClosed += HandleMachineUiClosed;
            _subscribedToMachineUi = true;
            _machineUiOpen = _machineUi.IsOpen;
            ApplyMachineUiVisibility();
        }

        private void UnsubscribeMachineUi()
        {
            if (!_subscribedToMachineUi || _machineUi == null)
            {
                return;
            }

            _machineUi.InterfaceOpened -= HandleMachineUiOpened;
            _machineUi.InterfaceClosed -= HandleMachineUiClosed;
            _machineUi = null;
            _subscribedToMachineUi = false;
        }

        private void HandleMachineUiOpened()
        {
            _machineUiOpen = true;
            CleanupDrag();
            ApplyMachineUiVisibility();
        }

        private void HandleMachineUiClosed()
        {
            _machineUiOpen = false;
            ApplyMachineUiVisibility();
        }

        /// <summary>
        /// Hide open storage panels while machine UI is up (same suppress as Main HUD). Panels stay
        /// bound and return when MI closes — do not tear them down here.
        /// </summary>
        private void ApplyMachineUiVisibility()
        {
            if (_root == null)
            {
                return;
            }

            _root.style.display = _machineUiOpen ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private ContainerViewer _boundViewer;
        private ContainerViewer.ContainerEventHandler _boundOpenedHandler;

        /// <summary>
        /// Called by MainHudSubSystem once it resolves the local player's ContainerViewer (same
        /// binding moment as HumanInventory/Hands) — Systems-layer code must not depend on a UI
        /// assembly, so this host can't discover its own local player the way MainHudSubSystem does;
        /// it piggybacks on that already-correct lifecycle instead of duplicating it.
        /// </summary>
        public void BindContainerViewer(ContainerViewer viewer)
        {
            EnsureMachineUiSubscription();

            if (_boundViewer == viewer)
            {
                return;
            }

            UnbindContainerViewer();

            _boundViewer = viewer;
            _localInventory = viewer.inventory;
            _boundOpenedHandler = container => HandleContainerOpened(viewer, container);
            viewer.OnContainerOpened += _boundOpenedHandler;
            viewer.OnContainerClosed += HandleContainerClosed;
        }

        /// <summary>Called by MainHudSubSystem when the local player unbinds (respawn, disconnect).</summary>
        public void UnbindContainerViewer()
        {
            if (_boundViewer != null)
            {
                _boundViewer.OnContainerOpened -= _boundOpenedHandler;
                _boundViewer.OnContainerClosed -= HandleContainerClosed;
            }

            _boundViewer = null;
            _boundOpenedHandler = null;
            _localInventory = null;
            ClearHudDropTargets();

            foreach (AttachedContainer container in new List<AttachedContainer>(_openPanels.Keys))
            {
                ClosePanel(container);
            }
        }

        /// <summary>
        /// Registers Main HUD slots (equipment / gear / hands) as peers for panel↔HUD drag-drop.
        /// Cleared on unbind; MainHudSubSystem re-registers after each equipment refresh.
        /// </summary>
        public void SetHudDropTargets(IEnumerable<HudDropTarget> targets)
        {
            _hudDropTargets.Clear();
            if (targets != null)
            {
                _hudDropTargets.AddRange(targets);
            }
        }

        public void ClearHudDropTargets()
        {
            _hudDropTargets.Clear();
        }

        /// <summary>
        /// Requests opening a container's panel anchored near a screen position (gear-strip icon,
        /// world-container click). The actual open still round-trips through ContainerViewer/the
        /// server, same as every other container-open path — this only supplies a positioning hint
        /// consumed once the OnContainerOpened callback fires.
        /// </summary>
        public void RequestOpenNear(ContainerViewer viewer, AttachedContainer container, Vector2 screenAnchor)
        {
            if (_openPanels.ContainsKey(container))
            {
                return;
            }

            _pendingAnchors[container] = screenAnchor;
            viewer.ShowContainerUI(container);
        }

        /// <summary>Player-initiated close (× button) — tears the panel down locally and notifies the server.</summary>
        public void RequestClose(ContainerViewer viewer, AttachedContainer container)
        {
            ClosePanel(container);
            viewer.CmdContainerClose(container);
        }

        private void HandleContainerOpened(ContainerViewer viewer, AttachedContainer container)
        {
            if (_openPanels.ContainsKey(container))
            {
                return;
            }

            Vector2 anchor = _pendingAnchors.TryGetValue(container, out Vector2 pending)
                ? pending
                : NextCascadePosition();
            _pendingAnchors.Remove(container);

            _pendingBreadcrumbs.TryGetValue(container, out string breadcrumb);
            _pendingBreadcrumbs.Remove(container);

            StoragePanelView view = new(breadcrumb);
            view.CloseRequested += () => RequestClose(viewer, container);
            view.SlotDragStarted += HandleSlotDragStarted;
            view.SlotDragMoved += HandleSlotDragMoved;
            view.SlotDragEnded += HandleSlotDragEnded;
            view.SlotNestedOpenRequested += HandleSlotNestedOpenRequested;
            view.Bind(container);

            _root.Add(view);
            PositionPanel(view, anchor);
            _openPanels[container] = view;
            container.OpenPanel = view;
        }

        private void HandleContainerClosed(AttachedContainer container)
        {
            ClosePanel(container);
        }

        private void ClosePanel(AttachedContainer container)
        {
            if (!_openPanels.TryGetValue(container, out StoragePanelView view))
            {
                return;
            }

            ((IContainerPanel)view).Close();
            _openPanels.Remove(container);

            if (ReferenceEquals(container.OpenPanel, view))
            {
                container.OpenPanel = null;
            }
        }

        private void PositionPanel(StoragePanelView view, Vector2 anchor)
        {
            // Tentative placement; GeometryChanged repositions once the panel has a real size so
            // bottom-of-screen gear anchors (belt etc.) open above the strip instead of off-screen.
            view.style.left = anchor.x;
            view.style.top = anchor.y;

            EventCallback<GeometryChangedEvent> onGeometry = null;
            onGeometry = _ =>
            {
                float width = view.resolvedStyle.width;
                float height = view.resolvedStyle.height;
                if (width <= 0f || height <= 0f || float.IsNaN(width) || float.IsNaN(height))
                {
                    return;
                }

                view.UnregisterCallback(onGeometry);
                Vector2 positioned = ChoosePanelPosition(anchor, width, height);
                view.style.left = positioned.x;
                view.style.top = positioned.y;
            };
            view.RegisterCallback(onGeometry);
        }

        /// <summary>
        /// Prefer opening above the anchor (Main HUD gear strip sits on the bottom edge), then clamp
        /// into the host root so the panel never hangs off-screen.
        /// </summary>
        private Vector2 ChoosePanelPosition(Vector2 anchor, float width, float height)
        {
            const float margin = 8f;

            float parentWidth = _root.resolvedStyle.width;
            float parentHeight = _root.resolvedStyle.height;
            if (parentWidth <= 0f || parentHeight <= 0f)
            {
                return anchor;
            }

            float top = anchor.y - height - margin;
            if (top < 0f)
            {
                top = anchor.y + margin;
            }

            float left = Mathf.Clamp(anchor.x, 0f, Mathf.Max(0f, parentWidth - width));
            top = Mathf.Clamp(top, 0f, Mathf.Max(0f, parentHeight - height));
            return new Vector2(left, top);
        }

        private Vector2 NextCascadePosition()
        {
            Vector2 basePosition = new(160f, 120f);
            Vector2 offset = new(_cascadeIndex * 40f, _cascadeIndex * 30f);
            _cascadeIndex = (_cascadeIndex + 1) % 6;
            return basePosition + offset;
        }

        // --- Nested containers -------------------------------------------------------------------

        /// <summary>
        /// Opens a nested container (e.g. a lockbox found inside an already-open locker) with an
        /// origin breadcrumb, per Documents/design/inventory-storage.md §7 (one level deep, sequential
        /// opening — this doesn't recurse further, matching the design doc's explicit limit).
        /// </summary>
        public void OpenNested(ContainerViewer viewer, AttachedContainer nestedContainer, AttachedContainer originContainer, Vector2Int originSlot, Vector2 screenAnchor)
        {
            _pendingBreadcrumbs[nestedContainer] = $"from {originContainer.ContainerName} — Slot {originSlot}";
            RequestOpenNear(viewer, nestedContainer, screenAnchor);
        }

        // --- Drag and drop -------------------------------------------------------------------------
        // Screen-space slot-to-slot dragging is a new UITK pointer-capture implementation, not the
        // world-space InteractionTier.Combine grammar — see the architecture effort doc's "Scope
        // decisions" for why. UITK doesn't support CSS @keyframes, so drop-state highlighting is a
        // static class toggle rather than the mockup's pulsing border animation.

        private void HandleSlotDragStarted(StoragePanelView panel, StorageSlot slot, Vector2 position)
        {
            BeginDrag(slot.BoundItem, panel.Container, panel, slot, position);
        }

        /// <summary>Starts a drag that originated on a Main HUD slot (equipment / gear / hand).</summary>
        public void BeginHudDrag(Item item, AttachedContainer sourceContainer, Vector2 position)
        {
            BeginDrag(item, sourceContainer, null, null, position);
        }

        /// <summary>Pointer moved during a HUD-originated drag.</summary>
        public void MoveHudDrag(Vector2 position)
        {
            HandleSlotDragMoved(position);
        }

        /// <summary>Pointer released during a HUD-originated drag.</summary>
        public void EndHudDrag(Vector2 releasePosition)
        {
            TryCompleteTransfer(releasePosition);
            CleanupDrag();
        }

        private void BeginDrag(
            Item item,
            AttachedContainer sourceContainer,
            StoragePanelView sourcePanel,
            StorageSlot sourceSlot,
            Vector2 position)
        {
            if (item == null || sourceContainer == null)
            {
                return;
            }

            _dragItem = item;
            _dragSourceContainer = sourceContainer;
            _dragSourcePanel = sourcePanel;
            _dragSourceSlot = sourceSlot;

            _dragGhost = new VisualElement();
            _dragGhost.AddToClassList("storage-drag-ghost");
            _dragGhost.pickingMode = PickingMode.Ignore;

            _dragGhostIcon = new Image { sprite = item.ItemSprite };
            _dragGhostIcon.style.width = new Length(100, LengthUnit.Percent);
            _dragGhostIcon.style.height = new Length(100, LengthUnit.Percent);
            _dragGhostIcon.pickingMode = PickingMode.Ignore;
            _dragGhost.Add(_dragGhostIcon);

            _root.Add(_dragGhost);
            PositionGhost(position);
        }

        private void HandleSlotDragMoved(Vector2 position)
        {
            if (_dragGhost == null)
            {
                return;
            }

            PositionGhost(position);
            UpdateDropHighlight(position);
        }

        private void HandleSlotDragEnded(StoragePanelView sourcePanel, StorageSlot sourceSlot, Vector2 releasePosition)
        {
            TryCompleteTransfer(releasePosition);
            CleanupDrag();
        }

        private void TryCompleteTransfer(Vector2 releasePosition)
        {
            ClearDropHighlight();

            if (_dragItem == null || _localInventory == null)
            {
                return;
            }

            (StoragePanelView targetPanel, StorageSlot targetSlot) = HitTestPanels(releasePosition);
            if (targetPanel != null && targetSlot != null)
            {
                // Panel slot under the pointer owns the drop — including the source slot (cancel) and
                // invalid targets. Never fall through to HUD or world.
                if (targetSlot != _dragSourceSlot
                    && targetPanel.Container.CanContainItemAtPosition(_dragItem, targetSlot.Position))
                {
                    _localInventory.ClientTransferItem(_dragItem, targetSlot.Position, targetPanel.Container);
                }

                return;
            }

            HudDropTarget hudTarget = HitTestHud(releasePosition);
            if (hudTarget?.Element != null)
            {
                if (hudTarget.Container != null
                    && hudTarget.Container != _dragSourceContainer
                    && TryGetHudDropPosition(hudTarget, _dragItem, out Vector2Int dropPosition))
                {
                    _localInventory.ClientTransferItem(_dragItem, dropPosition, hudTarget.Container);
                }

                return;
            }

            // Over a storage panel but not a slot (header/body) — cancel. Do not use
            // InputInterface.IsPointerOverInterface(): leftover uGUI canvases often keep it true
            // and would block every world drop.
            if (IsOverOpenPanel(releasePosition))
            {
                return;
            }

            TryPlaceDraggedItemInWorld();
        }

        /// <summary>
        /// Camera-raycasts the current pointer into the world and requests a DropInteraction-equivalent
        /// placement via <see cref="HumanInventory.ClientPlaceItemInWorld"/>.
        /// </summary>
        private void TryPlaceDraggedItemInWorld()
        {
            if (_dragItem == null || _localInventory == null)
            {
                return;
            }

            Camera camera = ResolveGameplayCamera();
            if (camera == null)
            {
                return;
            }

            Ray ray = camera.ScreenPointToRay(InputInterface.GetPointerScreenPosition());
            // Match gameplay interaction rays: hit whatever the camera sees (tiles are often not on Default).
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            _localInventory.ClientPlaceItemInWorld(_dragItem, hit.point, hit.normal);
        }

        private static Camera ResolveGameplayCamera()
        {
            if (SubSystems.TryGet(out CameraSubSystem cameras)
                && cameras.PlayerCamera != null
                && cameras.PlayerCamera.TryGetComponent(out Camera playerCamera))
            {
                return playerCamera;
            }

            return Camera.main;
        }

        private bool IsOverOpenPanel(Vector2 panelPosition)
        {
            foreach (StoragePanelView panel in _openPanels.Values)
            {
                if (panel != null && panel.worldBound.Contains(panelPosition))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleSlotNestedOpenRequested(StoragePanelView panel, StorageSlot slot)
        {
            if (_boundViewer == null || panel.Container == null || slot.BoundItem == null)
            {
                return;
            }

            AttachedContainer nested = slot.BoundItem.GetComponentInChildren<AttachedContainer>();
            if (nested == null || _openPanels.ContainsKey(nested))
            {
                return;
            }

            Vector2 anchor = new(slot.worldBound.xMax + 16f, slot.worldBound.y);
            OpenNested(_boundViewer, nested, panel.Container, slot.Position, anchor);
        }

        private void PositionGhost(Vector2 position)
        {
            _dragGhost.style.left = position.x - 26f;
            _dragGhost.style.top = position.y - 26f;
        }

        private void UpdateDropHighlight(Vector2 position)
        {
            ClearDropHighlight();

            if (_dragItem == null)
            {
                return;
            }

            (StoragePanelView targetPanel, StorageSlot targetSlot) = HitTestPanels(position);
            if (targetSlot != null && targetPanel != null && targetSlot != _dragSourceSlot)
            {
                bool valid = targetPanel.Container.CanContainItemAtPosition(_dragItem, targetSlot.Position);
                targetSlot.SetDropState(valid ? SlotDropState.Valid : SlotDropState.Invalid);
                _highlightedSlot = targetSlot;
                return;
            }

            HudDropTarget hudTarget = HitTestHud(position);
            if (hudTarget?.Element == null)
            {
                return;
            }

            // Same-container drag (reordering within one HUD well) — skip. Null container (missing
            // body slot during bind) is not "same as source"; it still needs a reject highlight.
            if (hudTarget.Container != null && hudTarget.Container == _dragSourceContainer)
            {
                return;
            }

            // Prefer the registered cell; for multi-slot wells (pockets) scan for a free/merge cell.
            bool hudValid = TryGetHudDropPosition(hudTarget, _dragItem, out _);
            hudTarget.Element.EnableInClassList("inventory-slot--valid-drop", hudValid);
            hudTarget.Element.EnableInClassList("inventory-slot--invalid-drop", !hudValid);
            _highlightedHudElement = hudTarget.Element;
        }

        private static bool TryGetHudDropPosition(HudDropTarget target, Item item, out Vector2Int position)
        {
            position = default;
            if (target?.Container == null || item == null)
            {
                return false;
            }

            if (target.Container.CanContainItemAtPosition(item, target.Position))
            {
                position = target.Position;
                return true;
            }

            return target.Container.TryFindPositionFor(item, out position);
        }

        private void ClearDropHighlight()
        {
            _highlightedSlot?.SetDropState(SlotDropState.None);
            _highlightedSlot = null;

            if (_highlightedHudElement != null)
            {
                _highlightedHudElement.EnableInClassList("inventory-slot--valid-drop", false);
                _highlightedHudElement.EnableInClassList("inventory-slot--invalid-drop", false);
                _highlightedHudElement = null;
            }
        }

        private (StoragePanelView panel, StorageSlot slot) HitTestPanels(Vector2 position)
        {
            foreach (StoragePanelView panel in _openPanels.Values)
            {
                StorageSlot slot = panel.HitTestSlot(position);
                if (slot != null)
                {
                    return (panel, slot);
                }
            }

            return (null, null);
        }

        private HudDropTarget HitTestHud(Vector2 position)
        {
            foreach (HudDropTarget target in _hudDropTargets)
            {
                if (target?.Element != null && target.Element.worldBound.Contains(position))
                {
                    return target;
                }
            }

            return null;
        }

        private void CleanupDrag()
        {
            ClearDropHighlight();
            _dragGhost?.RemoveFromHierarchy();
            _dragGhost = null;
            _dragGhostIcon = null;
            _dragSourcePanel = null;
            _dragSourceSlot = null;
            _dragItem = null;
            _dragSourceContainer = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only fallback when the Resources catalog is missing (pre-rebuild iteration).
        /// Player builds never hit this path.
        /// </summary>
        private void EnsureEditorAssets()
        {
            if (_storagePanelStyle == null)
            {
                _storagePanelStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    StoragePanelAssetPaths.StoragePanelStyle);
            }

            if (_inventorySlotStyle == null)
            {
                _inventorySlotStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    StoragePanelAssetPaths.InventorySlotStyle);
            }

            if (_document != null && _document.panelSettings == null)
            {
                _document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    StoragePanelAssetPaths.PanelSettings);
            }
        }
#endif
    }
}
