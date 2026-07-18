using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Inputs;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.UI.MachineInterface;
using SS3D.UI.MainHud.Components;
using SS3D.UI.MachineInterface.Components;
using SS3D.UI.StoragePanel;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud
{
    /// <summary>
    /// Main HUD overlay (design doc: Documents/design/main-hud.md). Binds the visual/interaction
    /// layer built in <see cref="MainHudView"/> to the local player's existing gameplay systems: hands/equipment
    /// (<see cref="Hands"/>/<see cref="HumanInventory"/>) and help/harm intent (<see cref="IIntentProvider"/>).
    /// <para>
    /// Hidden until a local spawned body exists during an in-game round; hidden again when the round leaves
    /// Ongoing/Ending (same spawn/round gate idea as <c>GameScreensController</c>). Also suppressed while a
    /// diegetic machine panel is open — visibility authority stays here (observes
    /// <see cref="MachineInterfaceSubSystem"/>); machine UI must not call into Main HUD.
    /// </para>
    /// <para>
    /// The alert icon stack has no hunger/thirst/restrained/pressure/radiation trackers to bind to yet - it
    /// always reports the all-clear <see cref="AlertStackState"/> until those systems exist, mirroring how
    /// <see cref="SS3D.Systems.ScreenEffects.ScreenEffectsSubSystem"/> itself was built ahead of its own hookup.
    /// </para>
    /// <para>
    /// Self-bootstraps the same way <c>ScreenEffectsSubSystem</c> does, instead of living on a scene/prefab
    /// GameObject - hand-editing scene/prefab YAML outside the Unity Editor isn't safe.
    /// </para>
    /// <para>
    /// Divergence from design: the hold-to-self-examine panel (main-hud.md §5/§15) is not implemented here —
    /// examine-self belongs with the general examine surface, not permanent HUD chrome.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainHudSubSystem : SubSystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SubSystems.TryGet(out MainHudSubSystem _))
            {
                return;
            }

            GameObject host = new(nameof(MainHudSubSystem));
            DontDestroyOnLoad(host);
            host.AddComponent<UIDocument>();
            host.AddComponent<MainHudSubSystem>();
        }

        [SerializeField] private UIDocument _document;
        [SerializeField] private StyleSheet _mainHudStyle;
        [SerializeField] private StyleSheet _alertIconStackStyle;
        [SerializeField] private StyleSheet _intentModuleStyle;
        [SerializeField] private StyleSheet _handsGearStripStyle;
        [SerializeField] private StyleSheet _equipmentGridStyle;
        [SerializeField] private StyleSheet _inventorySlotStyle;
        [SerializeField] private MainHudIconSet _icons;

        private MainHudView _view;
        private GameObject _localPlayer;
        private HumanInventory _inventory;
        private Hands _hands;
        private IIntentProvider _intentProvider;
        private Hand _cachedSelectedHand;
        private bool _machineUiOpen;
        private bool _subscribedToMachineUi;
        private MachineInterfaceSubSystem _machineUi;

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

            BuildView();
            InputInterface.RegisterDocument(_document);

            // Subscribe in OnAwake (same as PlayerCameraSubSystem): on pure clients the mind sync
            // often fires LocalPlayerObjectChanged before SubSystem OnStart would run.
            AddHandle(LocalPlayerObjectChanged.AddListener(HandleLocalPlayerObjectChanged));
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
            AddHandle(SpawnedPlayersUpdated.AddListener(HandleSpawnedPlayersUpdated));
        }

        // Self-bootstraps a bare GameObject (see Bootstrap()), so SerializeFields are never filled from a
        // scene/prefab. Assets come from a committed MainHudAssetCatalog under Resources — same pattern as
        // MachineInterfaceHost — so standalone builds work without Editor AssetDatabase.
        private bool TryEnsureAssets()
        {
            MainHudAssetCatalog catalog =
                Resources.Load<MainHudAssetCatalog>(MainHudAssetPaths.ResourcesCatalogName);
            if (catalog == null)
            {
#if UNITY_EDITOR
                EnsureEditorAssets();
                if (_mainHudStyle != null)
                {
                    ApplyDocumentPanelSettings(null);
                    return true;
                }
#endif
                Debug.LogError(
                    $"MainHudSubSystem could not load Resources/{MainHudAssetPaths.ResourcesCatalogName}. "
                    + "Run SS3D → Main HUD → Rebuild Asset Catalog and commit the asset.",
                    this);
                return false;
            }

            if (!catalog.HasRequiredAssets(out string missingField))
            {
                Debug.LogError(
                    $"MainHudAssetCatalog is missing required assets ({missingField}). "
                    + "Run SS3D → Main HUD → Rebuild Asset Catalog.",
                    this);
                return false;
            }

            ApplyCatalog(catalog);
            return true;
        }

        private void ApplyCatalog(MainHudAssetCatalog catalog)
        {
            _mainHudStyle = catalog.MainHudStyle;
            _alertIconStackStyle = catalog.AlertIconStackStyle;
            _intentModuleStyle = catalog.IntentModuleStyle;
            _handsGearStripStyle = catalog.HandsGearStripStyle;
            _equipmentGridStyle = catalog.EquipmentGridStyle;
            _inventorySlotStyle = catalog.InventorySlotStyle;
            _icons = catalog.Icons;
            ApplyDocumentPanelSettings(catalog.PanelSettings);
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

            // Stays below the radial menu / armed-interaction reticle (both on the same shared panel
            // settings), so those transient overlays still draw on top of the persistent HUD.
            _document.sortingOrder = -10;
        }

        protected override void OnStart()
        {
            base.OnStart();
            EnsureMachineUiSubscription();
            TryBindExistingLocalPlayer();
        }

        protected override void OnDestroyed()
        {
            UnsubscribeMachineUi();
            UnbindLocalPlayer();
            _view?.Detach();
            InputInterface.UnregisterDocument(_document);
            base.OnDestroyed();
        }

        private void Update()
        {
            EnsureMachineUiSubscription();

            // Late-joining clients can miss one-shot spawn events (SyncList Complete is ignored in
            // EntitySubSystem; mind may sync before Mind.player is linked). Keep trying until bound.
            if (_view != null && _localPlayer == null)
            {
                TryBindExistingLocalPlayer();
            }

            if (_view == null || _localPlayer == null)
            {
                return;
            }

            RefreshActiveHand();
        }

        private void BuildView()
        {
            StyleSheet[] styleSheets =
            {
                _mainHudStyle, _alertIconStackStyle, _intentModuleStyle, _handsGearStripStyle,
                _equipmentGridStyle, _inventorySlotStyle,
            };

            _view = new MainHudView(styleSheets, _icons);
            _view.IntentToggleRequested += HandleIntentToggleRequested;
            _view.HandSelectedRequested += HandleHandSelectedRequested;
            _view.GearSlotClicked += HandleGearSlotClicked;
            _view.EquipmentSlotClicked += HandleEquipmentSlotClicked;
            _view.EquipmentDragStarted += HandleEquipmentDragStarted;
            _view.EquipmentDragMoved += HandleHudDragMoved;
            _view.EquipmentDragEnded += HandleHudDragEnded;
            _view.GearDragStarted += HandleGearDragStarted;
            _view.GearDragMoved += HandleHudDragMoved;
            _view.GearDragEnded += HandleHudDragEnded;
            _view.HandDragStarted += HandleHandDragStarted;
            _view.HandDragMoved += HandleHudDragMoved;
            _view.HandDragEnded += HandleHudDragEnded;
            _view.Attach(_document.rootVisualElement);
            _view.SetAlertState(default);
        }

        private void HandleLocalPlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            UnbindLocalPlayer();

            if (!e.PlayerHasObject || e.PlayerObject == null)
            {
                ApplyVisibility();
                return;
            }

            BindLocalPlayer(e.PlayerObject);
            ApplyVisibility();
        }

        private void HandleSpawnedPlayersUpdated(ref EventContext context, in SpawnedPlayersUpdated e)
        {
            TryBindExistingLocalPlayer();
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            switch (e.RoundState)
            {
                case RoundState.Ongoing:
                case RoundState.Ending:
                    TryBindExistingLocalPlayer();
                    break;
                default:
                    UnbindLocalPlayer();
                    ApplyVisibility();
                    break;
            }
        }

        /// <summary>
        /// Catch-up for clients that already have a spawned body before this subsystem subscribed,
        /// or before RoundState became Ongoing.
        /// </summary>
        private void TryBindExistingLocalPlayer()
        {
            if (!IsRoundInGame())
            {
                return;
            }

            if (_localPlayer != null)
            {
                ApplyVisibility();
                return;
            }

            if (!SubSystems.TryGet(out EntitySubSystem entities))
            {
                return;
            }

            foreach (Entity entity in entities.SpawnedPlayers)
            {
                if (entity == null || !IsLocalPlayerEntity(entity))
                {
                    continue;
                }

                BindLocalPlayer(entity.gameObject);
                ApplyVisibility();
                return;
            }
        }

        private static bool IsLocalPlayerEntity(Entity entity)
        {
            // Prefer FishNet ownership — Mind.player can still be null on the first mind SyncVar tick.
            if (entity.IsOwner)
            {
                return true;
            }

            return entity.Mind?.player != null && entity.Mind.player.IsLocalConnection;
        }

        private static bool IsRoundInGame()
        {
            if (!SubSystems.TryGet(out RoundSubSystem rounds))
            {
                return false;
            }

            RoundState state = rounds.CurrentRoundState;
            return state is RoundState.Ongoing or RoundState.Ending;
        }

        private void BindLocalPlayer(GameObject playerObject)
        {
            _localPlayer = playerObject;
            _inventory = _localPlayer.GetComponentInChildren<HumanInventory>();
            _hands = _localPlayer.GetComponentInChildren<Hands>();
            _intentProvider = _localPlayer.GetComponent<IIntentProvider>()
                ?? _localPlayer.GetComponentInChildren<IIntentProvider>();

            if (_inventory != null)
            {
                _inventory.OnInventoryContainerAdded += HandleInventoryChanged;
                _inventory.OnInventoryContainerRemoved += HandleInventoryChanged;
                _inventory.OnContainerContentChanged += HandleContainerContentChanged;
                _inventory.OnInventorySetUp += RefreshEquipmentAndGear;

                if (_inventory.containerViewer != null && SubSystems.TryGet(out StoragePanelHost panelHost))
                {
                    panelHost.BindContainerViewer(_inventory.containerViewer);
                }
            }

            RefreshEquipmentAndGear();
            RefreshIntent();
        }

        private void UnbindLocalPlayer()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryContainerAdded -= HandleInventoryChanged;
                _inventory.OnInventoryContainerRemoved -= HandleInventoryChanged;
                _inventory.OnContainerContentChanged -= HandleContainerContentChanged;
                _inventory.OnInventorySetUp -= RefreshEquipmentAndGear;
            }

            if (SubSystems.TryGet(out StoragePanelHost panelHost))
            {
                panelHost.UnbindContainerViewer();
            }

            _localPlayer = null;
            _inventory = null;
            _hands = null;
            _intentProvider = null;
            _cachedSelectedHand = null;
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
            ApplyVisibility();
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
            ApplyVisibility();
        }

        private void HandleMachineUiClosed()
        {
            _machineUiOpen = false;
            ApplyVisibility();
        }

        /// <summary>
        /// Single visibility gate: spawn/round eligibility and machine-UI suppress.
        /// Call instead of bare show so catch-up cannot re-show chrome over an open panel.
        /// </summary>
        private void ApplyVisibility()
        {
            bool shouldShow = _localPlayer != null && IsRoundInGame() && !_machineUiOpen;
            _view?.SetVisible(shouldShow);
        }

        private void HandleIntentToggleRequested()
        {
            _intentProvider?.RequestToggleIntent();
            RefreshIntent();
        }

        private void HandleHandSelectedRequested(bool leftHand)
        {
            if (_inventory == null || _hands == null || _hands.PlayerHands.Count == 0)
            {
                return;
            }

            int index = leftHand ? 0 : 1;
            if (index >= _hands.PlayerHands.Count)
            {
                return;
            }

            Hand hand = _hands.PlayerHands[index];
            if (hand?.Container == null)
            {
                return;
            }

            // Same path as legacy SingleItemContainerSlot — ServerRpc via HumanInventory.ActivateHand.
            _inventory.ActivateHand(hand.Container);

            // Hands never open the 1-slot hand container UI — only a held item that is itself storage (bag).
            Item held = hand.ItemInHand;
            if (held != null && _view != null)
            {
                HandsGearStrip.HandSlot handSlot = leftHand
                    ? HandsGearStrip.HandSlot.Left
                    : HandsGearStrip.HandSlot.Right;
                Rect bound = _view.HandsGear.GetHandInventorySlot(handSlot).worldBound;
                TryOpenItemStorageNear(held, new Vector2(bound.xMin, bound.yMin));
            }
        }

        /// <summary>
        /// Gear strip (belt/ID/pocket/back): equip/unequip vs active hand for 1-slot mounts; pocket opens
        /// its storage panel(s). Worn bags (backpack, tool belt) still open their own grids on click.
        /// </summary>
        private void HandleGearSlotClicked(HandsGearStrip.GearSlot slot)
        {
            if (_inventory == null)
            {
                return;
            }

            if (slot == HandsGearStrip.GearSlot.Pocket)
            {
                OpenPocketPanelsNearGear();
                return;
            }

            ContainerType type = GearSlotToContainerType(slot);
            if (!_inventory.TryGetTypeContainer(type, 0, out AttachedContainer container))
            {
                return;
            }

            Item item = container.Items.FirstOrDefault();
            if (item != null && _view != null)
            {
                Rect bound = _view.GetGearSlotWorldBound(slot);
                if (TryOpenItemStorageNear(item, new Vector2(bound.xMin, bound.yMin)))
                {
                    return;
                }
            }

            _inventory.ClientInteractWithContainerSlot(container, Vector2Int.zero);
        }

        /// <summary>
        /// Equipment doll: equip/unequip vs active hand, unless the worn item is itself storage.
        /// </summary>
        private void HandleEquipmentSlotClicked(EquipmentGrid.Slot slot)
        {
            if (_inventory == null)
            {
                return;
            }

            if (!TryGetEquipmentContainer(slot, out AttachedContainer container))
            {
                return;
            }

            Item item = container.Items.FirstOrDefault();
            if (item != null && _view?.Equipment != null)
            {
                Rect bound = _view.Equipment.GetSlotWorldBound(slot);
                if (TryOpenItemStorageNear(item, new Vector2(bound.xMin, bound.yMin)))
                {
                    return;
                }
            }

            _inventory.ClientInteractWithContainerSlot(container, Vector2Int.zero);
        }

        /// <summary>
        /// Opens a panel for storage living on an item (bag pockets, backpack grid). Returns false when
        /// the item has no <see cref="AttachedContainer"/> of its own — callers then equip/unequip.
        /// </summary>
        private bool TryOpenItemStorageNear(Item item, Vector2 anchor)
        {
            if (item == null
                || _inventory?.containerViewer == null
                || !TryGetStorageContainerOnItem(item, out AttachedContainer storage)
                || !SubSystems.TryGet(out StoragePanelHost panelHost))
            {
                return false;
            }

            panelHost.RequestOpenNear(_inventory.containerViewer, storage, anchor);
            return true;
        }

        /// <summary>
        /// Storage capacity attached to an item prefab (backpack, belt pouch). Prefer
        /// <see cref="AttachedContainer.HasUi"/> content grids; never use <see cref="Item.Container"/> —
        /// that is where the item is stored, not storage on the item. Body equip mounts use
        /// <see cref="AttachedContainer.DisplayAsSlotInUI"/> and live on the human, not the worn item.
        /// </summary>
        private static bool TryGetStorageContainerOnItem(Item item, out AttachedContainer storage)
        {
            storage = null;
            if (item == null)
            {
                return false;
            }

            AttachedContainer fallback = null;
            foreach (AttachedContainer candidate in item.GetComponentsInChildren<AttachedContainer>())
            {
                // Nested items' containers live under this item too — only take ones owned by this item.
                if (candidate.GetComponentInParent<Item>() != item)
                {
                    continue;
                }

                if (candidate.HasUi)
                {
                    storage = candidate;
                    return true;
                }

                if (fallback == null && !candidate.DisplayAsSlotInUI)
                {
                    fallback = candidate;
                }
            }

            storage = fallback;
            return storage != null;
        }

        private void HandleEquipmentDragStarted(EquipmentGrid.Slot slot, Vector2 position)
        {
            if (!TryGetEquipmentContainer(slot, out AttachedContainer container))
            {
                return;
            }

            Item item = container.Items.FirstOrDefault();
            if (item == null || !SubSystems.TryGet(out StoragePanelHost panelHost))
            {
                return;
            }

            panelHost.BeginHudDrag(item, container, position);
        }

        private void HandleGearDragStarted(HandsGearStrip.GearSlot slot, Vector2 position)
        {
            ContainerType type = GearSlotToContainerType(slot);
            if (_inventory == null
                || !_inventory.TryGetTypeContainer(type, 0, out AttachedContainer container))
            {
                return;
            }

            Item item = container.Items.FirstOrDefault();
            if (item == null || !SubSystems.TryGet(out StoragePanelHost panelHost))
            {
                return;
            }

            panelHost.BeginHudDrag(item, container, position);
        }

        private void HandleHandDragStarted(HandsGearStrip.HandSlot slot, Vector2 position)
        {
            if (_hands == null || _hands.PlayerHands.Count == 0)
            {
                return;
            }

            int index = slot == HandsGearStrip.HandSlot.Left ? 0 : 1;
            if (index >= _hands.PlayerHands.Count)
            {
                return;
            }

            Hand hand = _hands.PlayerHands[index];
            Item item = hand?.ItemInHand;
            if (item == null || hand.Container == null || !SubSystems.TryGet(out StoragePanelHost panelHost))
            {
                return;
            }

            panelHost.BeginHudDrag(item, hand.Container, position);
        }

        private void HandleHudDragMoved(EquipmentGrid.Slot slot, Vector2 position)
        {
            HandleHudDragMoved(position);
        }

        private void HandleHudDragMoved(HandsGearStrip.GearSlot slot, Vector2 position)
        {
            HandleHudDragMoved(position);
        }

        private void HandleHudDragMoved(HandsGearStrip.HandSlot slot, Vector2 position)
        {
            HandleHudDragMoved(position);
        }

        private void HandleHudDragMoved(Vector2 position)
        {
            if (SubSystems.TryGet(out StoragePanelHost panelHost))
            {
                panelHost.MoveHudDrag(position);
            }
        }

        private void HandleHudDragEnded(EquipmentGrid.Slot slot, Vector2 position)
        {
            HandleHudDragEnded(position);
        }

        private void HandleHudDragEnded(HandsGearStrip.GearSlot slot, Vector2 position)
        {
            HandleHudDragEnded(position);
        }

        private void HandleHudDragEnded(HandsGearStrip.HandSlot slot, Vector2 position)
        {
            HandleHudDragEnded(position);
        }

        private void HandleHudDragEnded(Vector2 position)
        {
            if (SubSystems.TryGet(out StoragePanelHost panelHost))
            {
                panelHost.EndHudDrag(position);
            }
        }

        /// <summary>Opens all pocket containers as storage panels (replaces ToggleInternalClothing).</summary>
        public void OpenPocketPanels()
        {
            OpenPocketPanelsNearGear();
        }

        private void OpenPocketPanelsNearGear()
        {
            if (_inventory?.containerViewer == null || !SubSystems.TryGet(out StoragePanelHost panelHost))
            {
                return;
            }

            Vector2 baseAnchor = new(120f, 200f);
            if (_view != null)
            {
                Rect bound = _view.GetGearSlotWorldBound(HandsGearStrip.GearSlot.Pocket);
                baseAnchor = new Vector2(bound.xMin, bound.yMin);
            }

            for (int i = 0; _inventory.TryGetTypeContainer(ContainerType.Pocket, i, out AttachedContainer pocket); i++)
            {
                Vector2 anchor = baseAnchor + new Vector2(i * 40f, -i * 24f);
                panelHost.RequestOpenNear(_inventory.containerViewer, pocket, anchor);
            }
        }

        private static ContainerType GearSlotToContainerType(HandsGearStrip.GearSlot slot) => slot switch
        {
            HandsGearStrip.GearSlot.Belt => ContainerType.Belt,
            HandsGearStrip.GearSlot.Id => ContainerType.Identification,
            HandsGearStrip.GearSlot.Pocket => ContainerType.Pocket,
            HandsGearStrip.GearSlot.Back => ContainerType.Bag,
            _ => ContainerType.None,
        };

        private static ContainerType EquipmentSlotToContainerType(EquipmentGrid.Slot slot) => slot switch
        {
            EquipmentGrid.Slot.Head => ContainerType.Head,
            EquipmentGrid.Slot.Eyes => ContainerType.Glasses,
            EquipmentGrid.Slot.Face => ContainerType.Mask,
            EquipmentGrid.Slot.Ears => ContainerType.EarLeft,
            EquipmentGrid.Slot.GloveLeft => ContainerType.GloveLeft,
            EquipmentGrid.Slot.Shirt => ContainerType.Jumpsuit,
            EquipmentGrid.Slot.GloveRight => ContainerType.GloveRight,
            EquipmentGrid.Slot.Feet => ContainerType.ShoeLeft,
            _ => ContainerType.None,
        };

        private bool TryGetEquipmentContainer(EquipmentGrid.Slot slot, out AttachedContainer container)
        {
            container = null;
            if (_inventory == null)
            {
                return false;
            }

            ContainerType type = EquipmentSlotToContainerType(slot);
            if (_inventory.TryGetTypeContainer(type, 0, out container))
            {
                return true;
            }

            if (slot == EquipmentGrid.Slot.Ears)
            {
                return _inventory.TryGetTypeContainer(ContainerType.EarRight, 0, out container);
            }

            if (slot == EquipmentGrid.Slot.Feet)
            {
                return _inventory.TryGetTypeContainer(ContainerType.ShoeRight, 0, out container);
            }

            return false;
        }

        private void RefreshIntent()
        {
            IntentType intent = _intentProvider?.CurrentIntent ?? IntentType.Help;
            _view.SetIntent(intent);
        }

        private void HandleInventoryChanged(AttachedContainer container)
        {
            RefreshEquipmentAndGear();
        }

        private void HandleContainerContentChanged(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
            RefreshEquipmentAndGear();
        }

        private void RefreshEquipmentAndGear()
        {
            if (_inventory == null)
            {
                return;
            }

            SetEquipment(EquipmentGrid.Slot.Head, ContainerType.Head);
            SetEquipment(EquipmentGrid.Slot.Eyes, ContainerType.Glasses);
            SetEquipment(EquipmentGrid.Slot.Face, ContainerType.Mask);
            SetEquipmentAlternate(
                EquipmentGrid.Slot.Ears,
                ContainerType.EarLeft,
                ContainerType.EarRight);
            SetEquipment(EquipmentGrid.Slot.GloveLeft, ContainerType.GloveLeft);
            SetEquipment(EquipmentGrid.Slot.Shirt, ContainerType.Jumpsuit);
            SetEquipment(EquipmentGrid.Slot.GloveRight, ContainerType.GloveRight);
            SetEquipmentAlternate(
                EquipmentGrid.Slot.Feet,
                ContainerType.ShoeLeft,
                ContainerType.ShoeRight);

            _view.SetHandContents(
                HandsGearStrip.HandSlot.Left,
                HandIconAt(0),
                HandNameAt(0));
            _view.SetHandContents(
                HandsGearStrip.HandSlot.Right,
                HandIconAt(1),
                HandNameAt(1));

            SetGear(HandsGearStrip.GearSlot.Belt, ContainerType.Belt);
            SetGear(HandsGearStrip.GearSlot.Id, ContainerType.Identification);
            SetGear(HandsGearStrip.GearSlot.Pocket, ContainerType.Pocket);
            SetGear(HandsGearStrip.GearSlot.Back, ContainerType.Bag);

            RegisterHudDropTargets();

            void SetEquipment(EquipmentGrid.Slot slot, ContainerType type)
            {
                Item item = ItemIn(type);
                _view.SetEquipmentContents(slot, item?.ItemSprite, item?.Name);
            }

            void SetEquipmentAlternate(EquipmentGrid.Slot slot, ContainerType primary, ContainerType secondary)
            {
                Item item = ItemIn(primary) ?? ItemIn(secondary);
                _view.SetEquipmentContents(slot, item?.ItemSprite, item?.Name);
            }

            void SetGear(HandsGearStrip.GearSlot slot, ContainerType type)
            {
                Item item = ItemIn(type);
                _view.SetGearContents(slot, item?.ItemSprite, item?.Name);
            }
        }

        private Item ItemIn(ContainerType type)
        {
            return _inventory != null && _inventory.TryGetTypeContainer(type, 0, out AttachedContainer container)
                ? container.Items.FirstOrDefault()
                : null;
        }

        private Sprite HandIconAt(int position) => HandItemAt(position)?.ItemSprite;

        private string HandNameAt(int position) => HandItemAt(position)?.Name;

        private Item HandItemAt(int position)
        {
            if (_hands == null || position >= _hands.PlayerHands.Count)
            {
                return null;
            }

            return _hands.PlayerHands[position].ItemInHand;
        }

        private void RegisterHudDropTargets()
        {
            if (_inventory == null || _view == null || !SubSystems.TryGet(out StoragePanelHost panelHost))
            {
                return;
            }

            List<HudDropTarget> targets = new();

            void AddEquipment(EquipmentGrid.Slot slot)
            {
                if (!TryGetEquipmentContainer(slot, out AttachedContainer container))
                {
                    return;
                }

                InventorySlot element = _view.Equipment.GetInventorySlot(slot);
                targets.Add(new HudDropTarget(element, container, Vector2Int.zero, () => container.Items.FirstOrDefault()));
            }

            foreach (EquipmentGrid.Slot slot in Enum.GetValues(typeof(EquipmentGrid.Slot)))
            {
                AddEquipment(slot);
            }

            void AddGear(HandsGearStrip.GearSlot slot)
            {
                // Always register the visual well so hover reject works even if the body is briefly
                // missing that ContainerType during bind.
                InventorySlot element = _view.HandsGear.GetGearInventorySlot(slot);
                ContainerType type = GearSlotToContainerType(slot);
                _inventory.TryGetTypeContainer(type, 0, out AttachedContainer container);
                AttachedContainer captured = container;
                targets.Add(new HudDropTarget(
                    element,
                    captured,
                    Vector2Int.zero,
                    () => captured != null ? captured.Items.FirstOrDefault() : null));
            }

            AddGear(HandsGearStrip.GearSlot.Belt);
            AddGear(HandsGearStrip.GearSlot.Id);
            AddGear(HandsGearStrip.GearSlot.Pocket);
            AddGear(HandsGearStrip.GearSlot.Back);

            if (_hands != null)
            {
                for (int i = 0; i < _hands.PlayerHands.Count && i < 2; i++)
                {
                    Hand hand = _hands.PlayerHands[i];
                    if (hand?.Container == null)
                    {
                        continue;
                    }

                    HandsGearStrip.HandSlot handSlot = i == 0
                        ? HandsGearStrip.HandSlot.Left
                        : HandsGearStrip.HandSlot.Right;
                    InventorySlot element = _view.HandsGear.GetHandInventorySlot(handSlot);
                    AttachedContainer handContainer = hand.Container;
                    targets.Add(new HudDropTarget(
                        element,
                        handContainer,
                        Vector2Int.zero,
                        () => handContainer.Items.FirstOrDefault()));
                }
            }

            panelHost.SetHudDropTargets(targets);
        }

        private void RefreshActiveHand()
        {
            if (_hands == null || _hands.PlayerHands.Count == 0)
            {
                return;
            }

            Hand selected = _hands.SelectedHand;
            if (selected == _cachedSelectedHand)
            {
                return;
            }

            _cachedSelectedHand = selected;
            _view.SetActiveHand(_hands.PlayerHands.IndexOf(selected) == 0);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only fallback when the Resources catalog is missing (pre-rebuild iteration).
        /// Player builds never hit this path.
        /// </summary>
        private void EnsureEditorAssets()
        {
            if (_mainHudStyle == null)
            {
                _mainHudStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    MainHudAssetPaths.MainHudStyle);
            }

            if (_alertIconStackStyle == null)
            {
                _alertIconStackStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    MainHudAssetPaths.AlertIconStackStyle);
            }

            if (_intentModuleStyle == null)
            {
                _intentModuleStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    MainHudAssetPaths.IntentModuleStyle);
            }

            if (_handsGearStripStyle == null)
            {
                _handsGearStripStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    MainHudAssetPaths.HandsGearStripStyle);
            }

            if (_equipmentGridStyle == null)
            {
                _equipmentGridStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    MainHudAssetPaths.EquipmentGridStyle);
            }

            if (_inventorySlotStyle == null)
            {
                _inventorySlotStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    MainHudAssetPaths.InventorySlotStyle);
            }

            if (_icons.Head == null)
            {
                _icons = new MainHudIconSet
                {
                    Head = LoadSprite("BeepHead"),
                    Eyes = LoadSprite("Eyes"),
                    Face = LoadSprite("Face"),
                    Ears = LoadSprite("Ears"),
                    HandLeft = LoadSprite("HandLeft"),
                    HandRight = LoadSprite("HandRight"),
                    Shirt = LoadSprite("Shirt"),
                    Feet = LoadSprite("Feet"),
                    Belt = LoadSprite("Waist"),
                    Id = LoadSprite("Neck"),
                    Pocket = LoadSprite("Pocket"),
                    Back = LoadSprite("BeepBack"),
                };
            }

            if (_document != null && _document.panelSettings == null)
            {
                _document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    MainHudAssetPaths.PanelSettings);
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                $"{MainHudAssetPaths.IconRoot}{fileName}.png");
        }
#endif
    }
}
