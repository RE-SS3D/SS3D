using System.Collections.Generic;
using System.Linq;
using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.UI.MainHud.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud
{
    /// <summary>
    /// Always-on main HUD overlay (design doc: Documents/design/main-hud.md). Binds the visual/interaction
    /// layer built in <see cref="MainHudView"/> to the local player's existing gameplay systems: hands/equipment
    /// (<see cref="Hands"/>/<see cref="HumanInventory"/>), per-body-part damage (<see cref="HealthController"/>),
    /// and help/harm intent (<see cref="IIntentProvider"/>).
    /// <para>
    /// The alert icon stack has no hunger/thirst/restrained/pressure/radiation trackers to bind to yet - it
    /// always reports the all-clear <see cref="AlertStackState"/> until those systems exist, mirroring how
    /// <see cref="SS3D.Systems.ScreenEffects.ScreenEffectsSubSystem"/> itself was built ahead of its own hookup.
    /// </para>
    /// <para>
    /// Self-bootstraps the same way <c>ScreenEffectsSubSystem</c> does, instead of living on a scene/prefab
    /// GameObject - hand-editing scene/prefab YAML outside the Unity Editor isn't safe.
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

        // Reuses the same physical key ExamineUI.cs defaults to for its unrelated hover-detail feature - there is
        // no dedicated self-examine input action yet (see Documents/design/main-hud.md §5/§15 and ExamineUI.cs).
        private const KeyCode SelfExamineKey = KeyCode.LeftShift;
        private const KeyCode SelfExamineKeyAlt = KeyCode.RightShift;

        private static readonly (string Label, string[] NameHints)[] LimbZones =
        {
            ("Head", new[] { "head" }),
            ("Chest", new[] { "torso", "chest" }),
            ("L. Arm", new[] { "leftarm", "left_arm", "l_arm", "armleft" }),
            ("R. Arm", new[] { "rightarm", "right_arm", "r_arm", "armright" }),
            ("L. Leg", new[] { "leftleg", "left_leg", "l_leg", "legleft" }),
            ("R. Leg", new[] { "rightleg", "right_leg", "r_leg", "legright" }),
            ("Groin", new[] { "groin", "pelvis", "hip" }),
        };

        [SerializeField] private UIDocument _document;
        [SerializeField] private StyleSheet _mainHudStyle;
        [SerializeField] private StyleSheet _alertIconStackStyle;
        [SerializeField] private StyleSheet _intentModuleStyle;
        [SerializeField] private StyleSheet _handsGearStripStyle;
        [SerializeField] private StyleSheet _equipmentGridStyle;
        [SerializeField] private StyleSheet _selfExamineStyle;
        [SerializeField] private StyleSheet _inventorySlotStyle;
        [SerializeField] private StyleSheet _machineWindowStyle;
        [SerializeField] private MainHudIconSet _icons;

        private MainHudView _view;
        private GameObject _localPlayer;
        private HumanInventory _inventory;
        private Hands _hands;
        private HealthController _health;
        private IIntentProvider _intentProvider;
        private Hand _cachedSelectedHand;
        private bool _examineOpen;

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
            if (_mainHudStyle == null || _machineWindowStyle == null)
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
        }

        protected override void OnDestroyed()
        {
            UnbindLocalPlayer();
            _view?.Detach();
            base.OnDestroyed();
        }

        private void Update()
        {
            if (_view == null)
            {
                return;
            }

            RefreshActiveHand();
            RefreshExamineVisibility();
        }

        private void BuildView()
        {
            StyleSheet[] styleSheets =
            {
                _mainHudStyle, _alertIconStackStyle, _intentModuleStyle, _handsGearStripStyle,
                _equipmentGridStyle, _selfExamineStyle, _inventorySlotStyle, _machineWindowStyle,
            };

            _view = new MainHudView(styleSheets, _icons);
            _view.IntentToggleRequested += HandleIntentToggleRequested;
            _view.Attach(_document.rootVisualElement);
            _view.SetAlertState(default);
        }

        private void HandleLocalPlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            UnbindLocalPlayer();

            if (!e.PlayerHasObject)
            {
                return;
            }

            _localPlayer = e.PlayerObject;
            _inventory = _localPlayer.GetComponentInChildren<HumanInventory>();
            _hands = _localPlayer.GetComponentInChildren<Hands>();
            _health = _localPlayer.GetComponentInChildren<HealthController>();
            _intentProvider = _localPlayer.GetComponent<IIntentProvider>() ?? _localPlayer.GetComponentInChildren<IIntentProvider>();

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
            _health = null;
            _intentProvider = null;
            _cachedSelectedHand = null;
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
            _view.SetEquipmentIcon(EquipmentGrid.Slot.Shirt, IconFor(ContainerType.Jumpsuit));
            _view.SetEquipmentIcon(EquipmentGrid.Slot.Feet, IconFor(ContainerType.ShoeLeft) ?? IconFor(ContainerType.ShoeRight));

            Sprite handLeftIcon = HandIconAt(0);
            Sprite handRightIcon = HandIconAt(1);
            _view.SetEquipmentIcon(EquipmentGrid.Slot.HandLeft, handLeftIcon);
            _view.SetEquipmentIcon(EquipmentGrid.Slot.HandRight, handRightIcon);
            _view.SetHandIcons(handLeftIcon, handRightIcon);

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

        private void RefreshExamineVisibility()
        {
            bool held = Input.GetKey(SelfExamineKey) || Input.GetKey(SelfExamineKeyAlt);
            if (held != _examineOpen)
            {
                _examineOpen = held;
                _view.SetExamineOpen(held);
            }

            if (held)
            {
                RefreshExamineData();
            }
        }

        private void RefreshExamineData()
        {
            if (_health == null)
            {
                _view.SetExamineData(BuildEmptyLimbs(), System.Array.Empty<OrganReadout>());
                return;
            }

            List<BodyPart> bodyParts = _health.BodyPartsOnEntity.ToList();
            List<LimbReadout> limbs = new(LimbZones.Length);

            foreach ((string label, string[] nameHints) in LimbZones)
            {
                BodyPart match = bodyParts.FirstOrDefault(part => nameHints.Any(hint =>
                    part.Name.Replace(" ", string.Empty).ToLowerInvariant().Contains(hint)));

                limbs.Add(match == null
                    ? new LimbReadout(label, "0 / 0")
                    : new LimbReadout(label, BuildBruteBurnText(match)));
            }

            // ContainsLayer/FirstBodyLayerOfType are FishNet [Server]-gated; on a non-host client they currently
            // read back defaults until the health system exposes client-synced damage values. Pre-existing
            // networking limitation, out of scope for this HUD pass.
            List<OrganReadout> organs = new();
            foreach (BodyPart part in bodyParts)
            {
                if (!part.ContainsLayer(BodyLayerType.Organ))
                {
                    continue;
                }

                BodyLayer organLayer = part.FirstBodyLayerOfType(BodyLayerType.Organ);
                int percent = Mathf.RoundToInt(Mathf.Clamp01(1f - organLayer.RelativeDamage) * 100f);
                OrganSeverity severity = organLayer.RelativeDamage >= 0.6f
                    ? OrganSeverity.Critical
                    : organLayer.RelativeDamage >= 0.15f
                        ? OrganSeverity.Warning
                        : OrganSeverity.Normal;

                organs.Add(new OrganReadout(part.Name, percent, severity));
            }

            _view.SetExamineData(limbs, organs);
        }

        // Aggregates the closest brute/burn equivalent from the existing DamageType set (Crush/Slash/Puncture ~
        // brute, Heat/Cold ~ burn) since a literal brute/burn/toxin/oxy split doesn't exist per-layer.
        private static string BuildBruteBurnText(BodyPart part)
        {
            float brute = 0f;
            float burn = 0f;

            foreach (BodyLayer layer in part.BodyLayers)
            {
                brute += layer.GetDamageTypeQuantity(DamageType.Crush)
                    + layer.GetDamageTypeQuantity(DamageType.Slash)
                    + layer.GetDamageTypeQuantity(DamageType.Puncture);
                burn += layer.GetDamageTypeQuantity(DamageType.Heat) + layer.GetDamageTypeQuantity(DamageType.Cold);
            }

            return $"{Mathf.RoundToInt(brute)} / {Mathf.RoundToInt(burn)}";
        }

        private static List<LimbReadout> BuildEmptyLimbs()
        {
            List<LimbReadout> limbs = new(LimbZones.Length);
            foreach ((string label, _) in LimbZones)
            {
                limbs.Add(new LimbReadout(label, "0 / 0"));
            }

            return limbs;
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

            if (_selfExamineStyle == null)
            {
                _selfExamineStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MainHud/Components/SelfExamineWindowContent.uss");
            }

            if (_inventorySlotStyle == null)
            {
                _inventorySlotStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Components/InventorySlot.uss");
            }

            if (_machineWindowStyle == null)
            {
                _machineWindowStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Components/MachineWindow.uss");
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
