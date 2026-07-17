using System.Linq;
using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.UI.MainHud.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud
{
    /// <summary>
    /// Main HUD overlay (design doc: Documents/design/main-hud.md). Binds the visual/interaction
    /// layer built in <see cref="MainHudView"/> to the local player's existing gameplay systems: hands/equipment
    /// (<see cref="Hands"/>/<see cref="HumanInventory"/>) and help/harm intent (<see cref="IIntentProvider"/>).
    /// <para>
    /// Hidden until <see cref="LocalPlayerObjectChanged"/> reports a local spawned body; hidden again when the
    /// round leaves in-game states (same spawn/round gate pattern as <c>GameScreensController</c>).
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

        protected override void OnAwake()
        {
            base.OnAwake();

            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

#if UNITY_EDITOR
            EnsureEditorAssets();
#endif
            EnsureRuntimeAssets();
            BuildView();
        }

        // Assets are resolved through the Editor-only AssetDatabase lookups below because this subsystem
        // self-bootstraps a bare GameObject at runtime (see Bootstrap()) rather than living on a serialized
        // scene/prefab - the same reasoning ScreenEffectsSubSystem documents for self-bootstrapping in the
        // first place (hand-editing scene/prefab YAML outside the Editor isn't safe). Unlike the other
        // MonoBehaviour hosts in this codebase (MachineInterfaceHost, ArmedInteractionSubSystem), which get
        // their serialized asset references "for free" from a prefab/scene, a bare bootstrapped GameObject has
        // no such carrier in a standalone (non-Editor) build. Wiring this for a real build needs either a
        // Resources.Load path for these assets or moving MainHudSubSystem onto a persistent prefab.
        private void EnsureRuntimeAssets()
        {
#if !UNITY_EDITOR
            if (_mainHudStyle == null)
            {
                Debug.LogWarning(
                    "MainHudSubSystem is missing its UI Toolkit assets in this build - it self-bootstraps and " +
                    "currently only resolves them via Editor AssetDatabase lookups. Wire them through " +
                    "Resources.Load (or host this on a persistent prefab) for standalone builds.",
                    this);
            }
#endif
        }

        protected override void OnStart()
        {
            base.OnStart();
            AddHandle(LocalPlayerObjectChanged.AddListener(HandleLocalPlayerObjectChanged));
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
        }

        protected override void OnDestroyed()
        {
            UnbindLocalPlayer();
            _view?.Detach();
            base.OnDestroyed();
        }

        private void Update()
        {
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
            _view.Attach(_document.rootVisualElement);
            _view.SetAlertState(default);
        }

        private void HandleLocalPlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            UnbindLocalPlayer();

            if (!e.PlayerHasObject || e.PlayerObject == null)
            {
                HideHud();
                return;
            }

            BindLocalPlayer(e.PlayerObject);
            ShowHud();
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            switch (e.RoundState)
            {
                case RoundState.Ongoing:
                case RoundState.Ending:
                    break;
                default:
                    UnbindLocalPlayer();
                    HideHud();
                    break;
            }
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

            _localPlayer = null;
            _inventory = null;
            _hands = null;
            _intentProvider = null;
            _cachedSelectedHand = null;
        }

        private void ShowHud()
        {
            _view?.SetVisible(true);
        }

        private void HideHud()
        {
            _view?.SetVisible(false);
        }

        private void HandleIntentToggleRequested()
        {
            _intentProvider?.RequestToggleIntent();
            RefreshIntent();
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

            _view.SetEquipmentIcon(EquipmentGrid.Slot.Head, IconFor(ContainerType.Head));
            _view.SetEquipmentIcon(EquipmentGrid.Slot.Eyes, IconFor(ContainerType.Glasses));
            _view.SetEquipmentIcon(EquipmentGrid.Slot.Face, IconFor(ContainerType.Mask));
            _view.SetEquipmentIcon(EquipmentGrid.Slot.Ears, IconFor(ContainerType.EarLeft) ?? IconFor(ContainerType.EarRight));
            _view.SetEquipmentIcon(EquipmentGrid.Slot.GloveLeft, IconFor(ContainerType.GloveLeft));
            _view.SetEquipmentIcon(EquipmentGrid.Slot.Shirt, IconFor(ContainerType.Jumpsuit));
            _view.SetEquipmentIcon(EquipmentGrid.Slot.GloveRight, IconFor(ContainerType.GloveRight));
            _view.SetEquipmentIcon(EquipmentGrid.Slot.Feet, IconFor(ContainerType.ShoeLeft) ?? IconFor(ContainerType.ShoeRight));

            _view.SetHandIcons(HandIconAt(0), HandIconAt(1));

            _view.SetGearIcon(HandsGearStrip.GearSlot.Belt, IconFor(ContainerType.Belt));
            _view.SetGearIcon(HandsGearStrip.GearSlot.Id, IconFor(ContainerType.Identification));
            _view.SetGearIcon(HandsGearStrip.GearSlot.Pda, IconFor(ContainerType.Pda));
            _view.SetGearIcon(HandsGearStrip.GearSlot.Back, IconFor(ContainerType.Bag));
        }

        private Sprite IconFor(ContainerType type)
        {
            return _inventory != null && _inventory.TryGetTypeContainer(type, 0, out AttachedContainer container)
                ? container.Items.FirstOrDefault()?.ItemSprite
                : null;
        }

        private Sprite HandIconAt(int position)
        {
            if (_hands == null || position >= _hands.PlayerHands.Count)
            {
                return null;
            }

            return _hands.PlayerHands[position].ItemInHand?.ItemSprite;
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
        private void EnsureEditorAssets()
        {
            if (_mainHudStyle == null)
            {
                _mainHudStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MainHud/MainHud.uss");
            }

            if (_alertIconStackStyle == null)
            {
                _alertIconStackStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MainHud/Components/AlertIconStack.uss");
            }

            if (_intentModuleStyle == null)
            {
                _intentModuleStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MainHud/Components/IntentModule.uss");
            }

            if (_handsGearStripStyle == null)
            {
                _handsGearStripStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MainHud/Components/HandsGearStrip.uss");
            }

            if (_equipmentGridStyle == null)
            {
                _equipmentGridStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MainHud/Components/EquipmentGrid.uss");
            }

            if (_inventorySlotStyle == null)
            {
                _inventorySlotStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Components/InventorySlot.uss");
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
                    Shirt = null,
                    Feet = LoadSprite("Feet"),
                    Belt = LoadSprite("Waist"),
                    Id = LoadSprite("Neck"),
                    Pda = LoadSprite("Pocket"),
                    Back = LoadSprite("BeepBack"),
                };
            }

            if (_document != null && _document.panelSettings == null)
            {
                _document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    "Assets/Content/Systems/UI/Interactions/RadialInteractionMenu/HudOverlayPanelSettings.asset");
            }

            if (_document != null)
            {
                // Stays below the radial menu / armed-interaction reticle (both on the same shared panel
                // settings), so those transient overlays still draw on top of the persistent HUD.
                _document.sortingOrder = -10;
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                $"Assets/Art/Graphics/UI/Containers/InventoryIcons/{fileName}.png");
        }
#endif
    }
}
