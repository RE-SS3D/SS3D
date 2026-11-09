using System;
using System.Collections.Generic;
using System.Text;
using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Localization;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Examine;
using SS3D.Systems.Health;
using SS3D.Systems.Inputs;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Screens;
using SS3D.Systems.Selection;
using SS3D.Systems.Vision;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SS3D.UI.Examine
{
    /// <summary>
    /// Owns every examine hover/detail UI surface: the generic hover-name/detailed-text/detailed-image
    /// view (<see cref="GenericExamineHoverView"/>, replacing the condemned uGUI <c>ExamineUI</c>) for
    /// ordinary examinables, and the character-examine hover preview / persistent window
    /// (<see cref="CharacterQuickLookView"/> / <see cref="CharacterExamineWindowView"/>) for characters
    /// — discriminated via <see cref="ExamineData.Type"/> == <see cref="ExamineType.CHARACTER"/>, not a
    /// dedicated marker component (<c>Human.prefab</c>'s existing <c>Selectable</c>/<c>SimpleExaminable</c>
    /// already make characters examinable). Self-bootstraps like <see cref="UiShellSubSystem"/> so
    /// adopting it needs no Boot/Game scene edit.
    /// </summary>
    public sealed class ExamineOverlaySubSystem : SubSystem
    {
        private const string UnidentifiedKey = "character_examine.unidentified_crew_member";
        private const string UnidentifiedFallback = "Unidentified Crew Member";

        private readonly ExamineContentResolver _contentResolver = new();

        private ExamineSubSystem _examineSystem;
        private InputSubSystem _inputSystem;
        private ExamineOverlayAssetCatalog _catalog;
        private GenericExamineHoverView _genericView;
        private CharacterQuickLookView _quickLookView;
        private CharacterExamineWindowView _windowView;

        private IExaminable _currentExaminable;
        private HumanInventory _hoveredCharacterInventory;
        private HumanInventory _windowVictimInventory;
        private InteractionController _interactionController;
        private GameObject _localPlayer;
        private bool _pinnedDetailedExamine;
        private bool _wasDetailedExamineHeld;
        private bool _takeInProgress;

        private IExaminable _cachedExaminable;
        private ExamineContent _cachedContent;
        private bool _hasCachedContent;
        private bool _targetHidden;
        private float _nextVisibilityCheck;
        private bool _examineEventsBound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SubSystems.TryGet(out ExamineOverlaySubSystem _))
            {
                return;
            }

            GameObject host = new(nameof(ExamineOverlaySubSystem));
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<ExamineOverlaySubSystem>();
        }

        protected override void OnStart()
        {
            base.OnStart();
            AddHandle(LocalPlayerObjectChanged.AddListener(HandleLocalPlayerObjectChanged));
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();

            LocalizedTextService.EnsureInitialized();
            LocalizedTextService.LocaleChanged += HandleLocaleChanged;
            SubSystems.TryGet(out _inputSystem);
            TryBindExamineSystemEvents();
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            LocalizedTextService.LocaleChanged -= HandleLocaleChanged;
            UnsubscribeInteractionController();
            UnsubscribeExamineSystemEvents();
        }

        protected override void OnDestroyed()
        {
            UnsubscribeWindowView();
            UnsubscribeInteractionController();
            UnsubscribeExamineSystemEvents();
            _genericView?.Detach();
            _quickLookView?.Detach();
            _windowView?.Detach();
            _genericView = null;
            _quickLookView = null;
            _windowView = null;
            base.OnDestroyed();
        }

        private void Update()
        {
            // ExamineSubSystem lives on NetworkSystemsHub (Online only). This overlay is DDOL and
            // often enables before the hub exists — keep retrying until events are bound.
            TryBindExamineSystemEvents();

            if (_inputSystem == null)
            {
                SubSystems.TryGet(out _inputSystem);
            }

            // The hover target can be destroyed with no selection change (a disconnect despawns the
            // round's objects). The interface null check misses a destroyed Unity object.
            if (_currentExaminable is UnityEngine.Object target && target == null)
            {
                _currentExaminable = null;
                _hoveredCharacterInventory = null;
                InvalidateContentCache();
                RefreshGenericDisplay();
            }

            // The hover target can leave the FOV with no selection change: a door shuts, or the
            // player walks behind a wall.
            if (Time.unscaledTime >= _nextVisibilityCheck && UpdateTargetVisibility())
            {
                RefreshGenericDisplay();
            }

            bool detailedHeld = IsDetailedExamineHeld();
            if (detailedHeld != _wasDetailedExamineHeld)
            {
                _wasDetailedExamineHeld = detailedHeld;
                RefreshGenericDisplay();
            }
            else if (detailedHeld)
            {
                RefreshGenericDisplay();
            }

            if (_windowView != null && _windowView.IsOpen)
            {
                _windowView.Tick(Time.deltaTime);
            }

            if (_takeInProgress
                && (_interactionController == null || !_interactionController.HasActiveDelayedInteraction))
            {
                HandleTakeFromCharacterEnded();
            }

            if (Mouse.current == null)
            {
                return;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            _genericView?.UpdateAnchor(mousePosition);
            if (_windowView == null || !_windowView.IsOpen)
            {
                _quickLookView?.UpdateAnchor(mousePosition);
            }
        }

        private void HandleLocalPlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            UnsubscribeInteractionController();
            _localPlayer = e.PlayerHasObject ? e.PlayerObject : null;
            TryBindInteractionController();
        }

        private void HandleLocaleChanged()
        {
            InvalidateContentCache();
            RefreshGenericDisplay();
        }

        /// <summary>
        /// Binds hover/window events once <see cref="ExamineSubSystem"/> is registered (after Online).
        /// Hub teardown clears the bind so the next Online session can re-subscribe.
        /// </summary>
        private void TryBindExamineSystemEvents()
        {
            if (_examineEventsBound && _examineSystem == null)
            {
                _examineEventsBound = false;
            }

            if (_examineEventsBound)
            {
                return;
            }

            if (!SubSystems.TryGet(out _examineSystem) || _examineSystem == null)
            {
                return;
            }

            _examineSystem.OnExaminableChanged += HandleExaminableChanged;
            _examineSystem.OnDetailedExamineRequested += HandleDetailedExamineRequested;
            _examineSystem.OnCharacterWindowRequested += HandleWindowRequested;
            _examineSystem.OnCursorLabelOverrideChanged += RefreshGenericDisplay;
            _examineEventsBound = true;

            // Catch up whatever is under the cursor so hover is not blank until selection moves.
            if (SubSystems.TryGet(out SelectionSubSystem selection))
            {
                HandleExaminableChanged(selection.GetCurrentSelectable<IExaminable>());
            }
        }

        private void UnsubscribeExamineSystemEvents()
        {
            if (_examineSystem != null)
            {
                _examineSystem.OnExaminableChanged -= HandleExaminableChanged;
                _examineSystem.OnDetailedExamineRequested -= HandleDetailedExamineRequested;
                _examineSystem.OnCharacterWindowRequested -= HandleWindowRequested;
                _examineSystem.OnCursorLabelOverrideChanged -= RefreshGenericDisplay;
            }

            _examineSystem = null;
            _examineEventsBound = false;
        }

        private void HandleExaminableChanged(IExaminable examinable)
        {
            if (_pinnedDetailedExamine && examinable != _currentExaminable && examinable != null)
            {
                _pinnedDetailedExamine = false;
            }

            _currentExaminable = examinable;
            _wasDetailedExamineHeld = IsDetailedExamineHeld();
            _hoveredCharacterInventory = ResolveCharacterInventory(examinable);

            // Clothing pick under a human: still treat as character hover for name / Shift+Click.
            if (_hoveredCharacterInventory == null
                && examinable is Component component
                && CharacterExamineTargetUtility.TryResolveFromTransform(
                    component.transform,
                    out IExaminable characterExaminable,
                    out HumanInventory inventory))
            {
                _currentExaminable = characterExaminable;
                _hoveredCharacterInventory = inventory;
            }

            UpdateTargetVisibility();

            if (!EnsureViews())
            {
                return;
            }

            if (_windowView.IsOpen)
            {
                // The full window pins its own target; hover changes elsewhere don't affect either
                // preview while it's open (mirrors the design mock's `if (!fullOpen) showPreview`).
                return;
            }

            // Characters: name on hover, ExamineData details on Shift — paperdoll is Shift+Click
            // on others only (CharacterExamineWindowView). No pickable quick-look under the cursor.
            _quickLookView.Hide();
            RefreshGenericDisplay();
        }

        private void HandleDetailedExamineRequested(IExaminable examinable)
        {
            _pinnedDetailedExamine = true;
            HandleExaminableChanged(examinable);
        }

        private void HandleWindowRequested(IExaminable examinable)
        {
            HumanInventory inventory = ResolveCharacterInventory(examinable);
            if (inventory == null || !EnsureViews())
            {
                return;
            }

            // Main HUD already shows the local player's worn inventory.
            if (IsLocalPlayerInventory(inventory))
            {
                return;
            }

            CancelActiveTake();
            _windowVictimInventory = inventory;
            TryBindInteractionController();
            _quickLookView.Hide();
            _genericView.Hide();
            IReadOnlyList<CharacterExamineSlotContent> slots = CharacterExamineContentBuilder.BuildSlots(inventory);
            bool takeAllowed = CharacterLootUtility.IsLootable(inventory);
            Vector2 screenPosition = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : Vector2.zero;
            _windowView.Show(ResolveDisplayName(inventory), slots, takeAllowed, screenPosition);
        }

        private void HandleCloseRequested()
        {
            CancelActiveTake();
            _windowVictimInventory = null;
            _windowView.Hide();
            HandleExaminableChanged(_currentExaminable);
        }

        private void HandleTakeHoldStarted(CharacterExamineSlot slot)
        {
            if (_windowVictimInventory == null || !CharacterLootUtility.IsLootable(_windowVictimInventory))
            {
                return;
            }

            if (!TryBindInteractionController())
            {
                return;
            }

            // Click starts a new windup; replace any in-flight take first.
            if (_interactionController.HasActiveDelayedInteraction)
            {
                _interactionController.CancelActiveDelayedInteraction();
            }

            _interactionController.RequestTakeFromCharacter(_windowVictimInventory, slot);
        }

        private void HandleTakeHoldCancelled()
        {
            CancelActiveTake();
        }

        private void HandleTakeFromCharacterStarted(CharacterExamineSlot slot, float delaySeconds)
        {
            if (_windowView == null || !_windowView.IsOpen)
            {
                return;
            }

            _takeInProgress = true;
            _windowView.BeginTakeProgress(slot, delaySeconds);
        }

        private void HandleTakeFromCharacterEnded()
        {
            bool wasTaking = _takeInProgress;
            _takeInProgress = false;
            _windowView?.ClearTakeProgress();

            if (wasTaking || (_windowView != null && _windowView.IsOpen))
            {
                RefreshWindowSlots();
            }
        }

        private void CancelActiveTake()
        {
            if (_interactionController != null && _interactionController.HasActiveDelayedInteraction)
            {
                _interactionController.CancelActiveDelayedInteraction();
            }

            _takeInProgress = false;
            _windowView?.ClearTakeProgress();
        }

        private void RefreshWindowSlots()
        {
            if (_windowView == null || !_windowView.IsOpen || _windowVictimInventory == null)
            {
                return;
            }

            IReadOnlyList<CharacterExamineSlotContent> slots =
                CharacterExamineContentBuilder.BuildSlots(_windowVictimInventory);
            bool takeAllowed = CharacterLootUtility.IsLootable(_windowVictimInventory);
            _windowView.TakeAllowed = takeAllowed;
            _windowView.RefreshSlots(ResolveDisplayName(_windowVictimInventory), slots);
        }

        private bool TryBindInteractionController()
        {
            if (_interactionController != null)
            {
                return true;
            }

            if (_localPlayer == null)
            {
                return false;
            }

            if (!_localPlayer.TryGetComponent(out _interactionController))
            {
                _interactionController = _localPlayer.GetComponentInChildren<InteractionController>();
            }

            if (_interactionController == null)
            {
                return false;
            }

            _interactionController.TakeFromCharacterStarted += HandleTakeFromCharacterStarted;
            _interactionController.TakeFromCharacterEnded += HandleTakeFromCharacterEnded;
            return true;
        }

        private void UnsubscribeInteractionController()
        {
            if (_interactionController == null)
            {
                return;
            }

            _interactionController.TakeFromCharacterStarted -= HandleTakeFromCharacterStarted;
            _interactionController.TakeFromCharacterEnded -= HandleTakeFromCharacterEnded;
            _interactionController = null;
        }

        private void UnsubscribeWindowView()
        {
            if (_windowView == null)
            {
                return;
            }

            _windowView.CloseRequested -= HandleCloseRequested;
            _windowView.TakeHoldStarted -= HandleTakeHoldStarted;
            _windowView.TakeHoldCancelled -= HandleTakeHoldCancelled;
        }

        private static HumanInventory ResolveCharacterInventory(IExaminable examinable)
        {
            if (examinable is not Component component)
            {
                return null;
            }

            ExamineData data = examinable.GetData();
            if (data == null || data.Type != ExamineType.CHARACTER)
            {
                return null;
            }

            return component.TryGetComponent(out HumanInventory inventory) ? inventory : null;
        }

        private bool IsLocalPlayerInventory(HumanInventory inventory)
        {
            if (inventory == null || _localPlayer == null)
            {
                return false;
            }

            if (!_localPlayer.TryGetComponent(out HumanInventory localInventory))
            {
                localInventory = _localPlayer.GetComponentInChildren<HumanInventory>();
            }

            return localInventory != null
                && !CharacterLootUtility.IsOtherCharacter(localInventory, inventory);
        }

        private static string ResolveDisplayName(HumanInventory inventory)
        {
            if (CharacterExamineContentBuilder.TryGetVisibleIdentity(inventory, out string name, out _))
            {
                return name;
            }

            return LocalizedTextService.GetFormattedString(
                ExamineCanonicalKeyGenerator.ExamineTableName,
                UnidentifiedKey,
                System.Array.Empty<object>(),
                UnidentifiedFallback);
        }

        private bool TryResolveLocalCharacter(out IExaminable examinable, out HumanInventory inventory)
        {
            examinable = null;
            inventory = null;
            return _localPlayer != null
                && CharacterExamineTargetUtility.TryResolveFromTransform(
                    _localPlayer.transform,
                    out examinable,
                    out inventory);
        }

        private void ShowCharacterExamineDisplay(IExaminable examinable, HumanInventory inventory, bool isSelf)
        {
            string displayName = ResolveDisplayName(inventory);
            ExamineContent content = GetCachedContent(examinable);

            if (IsDetailedExamineHeld())
            {
                // Health sections rebuild every frame while Shift is held so bleed/crit updates live.
                string detailed = BuildCharacterDetailedText(content, inventory, isSelf);
                if (!string.IsNullOrWhiteSpace(detailed))
                {
                    _genericView.ShowDetailedText(displayName, detailed);
                    if (Mouse.current != null)
                    {
                        _genericView.UpdateAnchor(Mouse.current.position.ReadValue());
                    }

                    return;
                }
            }

            _genericView.ShowHoverName(displayName);
            if (Mouse.current != null)
            {
                _genericView.UpdateAnchor(Mouse.current.position.ReadValue());
            }
        }

        private static string BuildCharacterDetailedText(
            ExamineContent content,
            HumanInventory inventory,
            bool isSelf)
        {
            StringBuilder builder = new();
            string baseText = BuildDetailedText(content);
            if (!string.IsNullOrEmpty(baseText))
            {
                builder.Append(baseText);
            }

            List<ExamineSection> healthSections = new();
            CharacterExamineHealthBuilder.AppendSections(
                ResolveHealthController(inventory),
                isSelf,
                healthSections);

            foreach (ExamineSection section in healthSections)
            {
                if (string.IsNullOrWhiteSpace(section.Text))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("\n\n");
                }

                builder.Append(section.Text);
            }

            return builder.ToString();
        }

        private static HumanHealthController ResolveHealthController(HumanInventory inventory)
        {
            if (inventory == null)
            {
                return null;
            }

            if (inventory.TryGetComponent(out HumanHealthController health))
            {
                return health;
            }

            health = inventory.GetComponentInParent<HumanHealthController>();
            return health != null ? health : inventory.GetComponentInChildren<HumanHealthController>();
        }

        private void RefreshGenericDisplay()
        {
            if (_genericView == null)
            {
                return;
            }

            // Shift is held for Shift+Click open; Update() re-calls this every frame while detailed.
            // Re-showing the hover name after HandleWindowRequested.Hide() is what stuck
            // "Unidentified Crew Member" on the cursor with the paperdoll already open.
            if (_windowView != null && _windowView.IsOpen)
            {
                _genericView.Hide();
                return;
            }

            // Map Editor: the badge names what is about to be placed; nothing under the pointer is examined.
            string labelOverride = _examineSystem != null ? _examineSystem.CursorLabelOverride : null;
            if (labelOverride != null)
            {
                _quickLookView?.Hide();
                if (labelOverride.Length == 0)
                {
                    _genericView.Hide();
                    return;
                }

                _genericView.ShowHoverName(labelOverride);
                if (Mouse.current != null)
                {
                    _genericView.UpdateAnchor(Mouse.current.position.ReadValue());
                }

                return;
            }

            // No-target Shift → self (examine.md §2 / health Tier 0+1).
            if (_targetHidden || _currentExaminable?.GetData() == null)
            {
                if (IsDetailedExamineHeld()
                    && TryResolveLocalCharacter(out IExaminable selfExaminable, out HumanInventory selfInventory))
                {
                    ShowCharacterExamineDisplay(selfExaminable, selfInventory, isSelf: true);
                    return;
                }

                _genericView.Hide();
                InvalidateContentCache();
                return;
            }

            // Prefer visible identity for characters (ID card) over the static ExamineData name.
            // Hover = name; Shift = details + health lines; Shift+Click paperdoll is others-only.
            if (_hoveredCharacterInventory != null)
            {
                bool isSelf = IsLocalPlayerInventory(_hoveredCharacterInventory);
                ShowCharacterExamineDisplay(_currentExaminable, _hoveredCharacterInventory, isSelf);
                return;
            }

            ExamineContent nonCharacterContent = GetCachedContent(_currentExaminable);

            if (IsDetailedExamineHeld())
            {
                ExamineData data = _currentExaminable.GetData();
                if (data.Type == ExamineType.SIMPLE_IMAGE
                    && IsWithinDetailedImageRange(_currentExaminable, data)
                    && TryGetImageDetailedContent(
                        _currentExaminable,
                        nonCharacterContent,
                        out Sprite image,
                        out string caption,
                        out Vector2 imageSize))
                {
                    _genericView.ShowDetailedImage(image, caption, imageSize);
                    if (Mouse.current != null)
                    {
                        _genericView.UpdateAnchor(Mouse.current.position.ReadValue());
                    }

                    return;
                }

                if (nonCharacterContent.HasDescription || nonCharacterContent.Sections.Count > 0)
                {
                    _genericView.ShowDetailedText(nonCharacterContent.Name, BuildDetailedText(nonCharacterContent));
                    if (Mouse.current != null)
                    {
                        _genericView.UpdateAnchor(Mouse.current.position.ReadValue());
                    }

                    return;
                }
            }

            string hoverName = string.IsNullOrEmpty(nonCharacterContent.Name) ? "???" : nonCharacterContent.Name;
            _genericView.ShowHoverName(hoverName);
            if (Mouse.current != null)
            {
                _genericView.UpdateAnchor(Mouse.current.position.ReadValue());
            }
        }

        private ExamineContent GetCachedContent(IExaminable examinable)
        {
            if (_hasCachedContent && _cachedExaminable == examinable)
            {
                return _cachedContent;
            }

            _cachedContent = _contentResolver.Resolve(examinable);
            _cachedExaminable = examinable;
            _hasCachedContent = true;
            return _cachedContent;
        }

        private void InvalidateContentCache()
        {
            _hasCachedContent = false;
            _cachedExaminable = null;
            _cachedContent = ExamineContent.Empty;
        }

        private bool TryGetImageDetailedContent(
            IExaminable examinable,
            ExamineContent content,
            out Sprite image,
            out string caption,
            out Vector2 imageSize)
        {
            image = null;
            caption = string.Empty;
            imageSize = Vector2.zero;

            ExamineData data = examinable?.GetData();
            if (data == null || data.Type != ExamineType.SIMPLE_IMAGE || examinable is not IImageExaminable imageExaminable)
            {
                return false;
            }

            image = imageExaminable.GetDetailedImage();
            if (image == null)
            {
                return false;
            }

            imageSize = data.DetailedImageSize;
            caption = content.Description;
            return true;
        }

        /// <summary>Re-reads whether the FOV hides the hover target. Returns true if that changed.</summary>
        private bool UpdateTargetVisibility()
        {
            const float checkInterval = 0.1f;
            _nextVisibilityCheck = Time.unscaledTime + checkInterval;

            bool hidden = IsHoverPointHiddenByVision();
            bool changed = hidden != _targetHidden;
            _targetHidden = hidden;
            return changed;
        }

        /// <summary>
        /// Examine sees what the player sees (examine.md §2). Tests the point under the cursor, not
        /// the object's center, so a long wall stays examinable where it shows. The pick pass does
        /// not know about the FOV mask, so it still reports targets under the black.
        /// </summary>
        private bool IsHoverPointHiddenByVision()
        {
            if (_currentExaminable == null
                || !SubSystems.TryGet(out VisionSubSystem vision)
                || !SubSystems.TryGet(out SelectionSubSystem selection)
                || !selection.TryGetCurrentSelectable(out Selectable selectable))
            {
                return false;
            }

            Camera camera = null;
            if (SubSystems.TryGet(out CameraSubSystem cameras) && cameras.PlayerCamera != null)
            {
                camera = cameras.PlayerCamera.GetComponent<Camera>();
            }

            if (!SelectionTargetUtility.TryResolveInteractionPoint(camera, selectable, out Vector3 point, out _))
            {
                point = selectable.PickWorldCenter;
            }

            return !vision.IsVisible(point);
        }

        private bool IsWithinDetailedImageRange(IExaminable examinable, ExamineData data)
        {
            if (!TryGetPlayerExamineOrigin(out Vector3 origin))
            {
                return false;
            }

            return ExamineRangeUtility.IsWithinRange(examinable, origin, data.DetailedImageRange);
        }

        private bool TryGetPlayerExamineOrigin(out Vector3 origin)
        {
            origin = default;

            if (_localPlayer == null)
            {
                return false;
            }

            Hands hands = _localPlayer.GetComponentInChildren<Hands>();
            if (hands == null)
            {
                return false;
            }

            Hand hand = hands.SelectedHand;
            if (hand == null)
            {
                return false;
            }

            origin = hand.InteractionOrigin;
            return true;
        }

        private bool IsDetailedExamineHeld()
        {
            if (_pinnedDetailedExamine)
            {
                return true;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null
                && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
            {
                return true;
            }

            return _inputSystem != null && _inputSystem.DetailedExamine.IsPressed();
        }

        private static string BuildDetailedText(ExamineContent content)
        {
            if (content.Sections == null || content.Sections.Count == 0)
            {
                return content.Description;
            }

            StringBuilder builder = new(content.Description);
            foreach (ExamineSection section in content.Sections)
            {
                if (string.IsNullOrWhiteSpace(section.Text))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("\n\n");
                }

                builder.Append(section.Text);
            }

            return builder.ToString();
        }

        private bool EnsureViews()
        {
            if (_genericView != null && _quickLookView != null && _windowView != null)
            {
                return true;
            }

            if (!TryEnsureCatalog())
            {
                return false;
            }

            if (!SubSystems.TryGet(out UiShellSubSystem uiShell)
                || !uiShell.TryGetLayer(UiLayer.Overlay, out VisualElement overlayLayer))
            {
                Debug.LogError("ExamineOverlaySubSystem could not find the UiShellSubSystem overlay layer.", this);
                return false;
            }

            // See RadialInteractionSubSystem for why this register is never paired with an unregister here.
            InputInterface.RegisterDocument(uiShell.Document);

            _genericView = new GenericExamineHoverView(_catalog.ExamineStyle);
            _genericView.Attach(overlayLayer);

            CharacterExamineIconSet icons = _catalog.Icons;
            _quickLookView = new CharacterQuickLookView(
                _catalog.ExamineStyle, _catalog.InventorySlotStyle, _catalog.DiegeticTokensStyle, icons);
            _quickLookView.Attach(overlayLayer);

            _windowView = new CharacterExamineWindowView(
                _catalog.ExamineStyle,
                _catalog.InventorySlotStyle,
                _catalog.DiegeticTokensStyle,
                _catalog.MachineWindowStyle,
                icons);
            _windowView.Attach(overlayLayer);
            _windowView.CloseRequested += HandleCloseRequested;
            _windowView.TakeHoldStarted += HandleTakeHoldStarted;
            _windowView.TakeHoldCancelled += HandleTakeHoldCancelled;

            return true;
        }

        private bool TryEnsureCatalog()
        {
            if (_catalog != null)
            {
                return true;
            }

            _catalog = Resources.Load<ExamineOverlayAssetCatalog>(ExamineOverlayAssetPaths.ResourcesCatalogName);
            if (_catalog == null)
            {
                Debug.LogError(
                    $"ExamineOverlaySubSystem could not load Resources/{ExamineOverlayAssetPaths.ResourcesCatalogName}. "
                    + "Run SS3D → Examine → Rebuild Examine Asset Catalog and commit the asset.",
                    this);
                return false;
            }

            if (!_catalog.HasRequiredAssets(out string missingField))
            {
                Debug.LogError(
                    $"ExamineOverlayAssetCatalog is missing required assets ({missingField}). "
                    + "Run SS3D → Examine → Rebuild Examine Asset Catalog.",
                    this);
                return false;
            }

            return true;
        }
    }
}
